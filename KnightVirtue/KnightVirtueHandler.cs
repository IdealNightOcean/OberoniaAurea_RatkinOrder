using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// （常驻）骑士美德处理器 - 负责管理骑士的美德，包括美德的获取、升级、放弃等，以及与美德相关的buff计算和骑士信条等级检测
/// </summary>
public class KnightVirtueHandler : IExposable
{
    private readonly ResidentKnight knight;
    public Pawn Pawn => knight.Pawn;

    private Hediff buffHediff;
    public Hediff BuffHediff => buffHediff ??= Pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_KnightVirtue);

    /// <summary>
    /// 骑士信条等级
    /// </summary>
    private int curKnightCreedLevel;
    private List<KnightVirtue> virtues = [];

    public IReadOnlyList<KnightVirtue> Virtues => virtues;

    public List<ITickInterval> tickIntervalVirtues = [];

    public int TotalVirtueCount => virtues.Count;

    public int TotalVirtueLevel
    {
        get
        {
            int totalLevel = 0;
            foreach (KnightVirtue virtue in virtues)
                totalLevel += virtue.Level;
            return totalLevel;
        }
    }

    public bool HasUpgradableVirtue
    {
        get
        {
            foreach (KnightVirtue virtue in virtues)
                if (virtue.Level < virtue.Def.maxLevel)
                    return true;
            return false;
        }
    }
    public bool HasUnusedTraitSlot
    {
        get
        {
            for (int i = 0; i < virtues.Count; i++)
                if (virtues[i].HasUnusedTraitSlot)
                    return true;

            return false;
        }
    }

    public SimpleValueCache<float> VirtueStatValueCache { get; }
    public int CurVirtueCountLimit
    {
        get
        {
            return VirtueStatValueCache.GetCachedResult() switch
            {
                < 1f => 0,
                < 5f => 2,
                < 12f => 3,
                _ => 4,
            };
        }
    }

    private HediffStageModifierBuilder BuffStageTemplate { get; } = new();
    private string BuffDetailExplanation { get; set; }

    public void TickInterval(int delta)
    {
        if (knight.CurState != ResidentPawnState.Normal)
            return;

        if (tickIntervalVirtues.Count > 0)
        {
            foreach (ITickInterval virtue in tickIntervalVirtues)
            {
                virtue.TickInterval(delta);
            }
        }

        if (knight.Pawn.IsHashIntervalTick(2500))
            CheckKnightCreed();
    }

    public KnightVirtueHandler(ResidentKnight knight)
    {
        this.knight = knight ?? throw new ArgumentNullException(nameof(knight));

        VirtueStatValueCache = new(cacheInterval: 30000,
                                   checker: () => Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref virtues, nameof(virtues), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (virtues.RemoveAll(v => v?.Def is null) > 0)
            {
                Log.Warning("[OARO] 在加载存档时发现并移除了无效的骑士美德。");
            }

            foreach (KnightVirtue virtue in virtues)
            {
                ActiveVirtue(virtue);
            }
        }
    }

    public KnightVirtue GetVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
            if (virtue == virtues[i].Def)
                return virtues[i];

        return null;
    }

    public bool HasVirtue(KnightVirtueDef virtueDef)
    {
        for (int i = 0; i < virtues.Count; i++)
            if (virtueDef == virtues[i].Def)
                return true;

        return false;
    }

    public bool TryAddVirtue(KnightVirtueDef virtueDef, int level, string reason)
    {
        if (virtues.Count >= CurVirtueCountLimit)
            return false;

        if (!AddVirtue(virtueDef, level))
            return false;

        reason ??= string.Empty;

        Find.LetterStack.ReceiveLetter(
            label: "OARO_LetterLabel_VirtueGained".Translate(Pawn.Named(KeyLibrary_FormatArgName.PAWN)),
            text: "OARO_LetterText_VirtueGained".Translate(Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                                           virtueDef.Named(KeyLibrary_FormatArgName.VIRTUEDEF),
                                                           level.Named(KeyLibrary_FormatArgName.Level),
                                                           reason.Named(KeyLibrary_FormatArgName.Reason)),
            textLetterDef: LetterDefOf.PositiveEvent,
            lookTargets: Pawn);

        return true;
    }

    /// <summary>
    /// 将指定美德升级到目标等级（如果当前等级已经高于或等于目标等级则升级失败），成功升级后会发送提示信息
    /// </summary>
    public bool UpgradeVirtueTo(KnightVirtueDef virtueDef, int targetLevel, string reason)
    {
        if (virtueDef is null || targetLevel <= 0)
            return false;

        KnightVirtue virtue = GetVirtue(virtueDef);
        if (virtue is null)
        {
            Log.Error($"[OARO] 尝试升级骑士美德失败：未找到指定的美德 - {virtueDef}");
            return false;
        }

        int upgradeAmount = targetLevel - virtue.Level;
        if (upgradeAmount <= 0)
        {
            Log.Error($"[OARO] 尝试升级骑士美德失败：目标等级必须高于当前等级 - {virtueDef} 当前等级: {virtue.Level} 目标等级: {targetLevel}");
            return false;
        }

        int newLevel = UpgradeVirtue(virtue, upgradeAmount);
        if (newLevel < 0)
            return false;

        Messages.Message(
            text: "OARO_Message_VirtueUpgraded".Translate(Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                                          virtueDef.Named(KeyLibrary_FormatArgName.VIRTUEDEF),
                                                          newLevel.Named(KeyLibrary_FormatArgName.Level),
                                                          reason.Named(KeyLibrary_FormatArgName.Reason)),
            lookTargets: Pawn,
            def: MessageTypeDefOf.PositiveEvent);

        return true;
    }

    /// <summary>
    /// 升级指定美德
    /// </summary>
    public bool UpgradeVirtue(KnightVirtueDef virtueDef, int upgrade, string reason)
    {
        if (virtueDef is null || upgrade <= 0)
            return false;

        KnightVirtue virtue = GetVirtue(virtueDef);
        if (virtue is null)
            return TryAddVirtue(virtueDef, 1, reason);

        int newLevel = UpgradeVirtue(virtue, upgrade);
        if (newLevel < 0)
            return false;

        Messages.Message(
            text: "OARO_Message_VirtueUpgraded".Translate(Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                                          virtueDef.Named(KeyLibrary_FormatArgName.VIRTUEDEF),
                                                          newLevel.Named(KeyLibrary_FormatArgName.Level),
                                                          reason.Named(KeyLibrary_FormatArgName.Reason)),
            lookTargets: Pawn,
            def: MessageTypeDefOf.PositiveEvent);

        return true;
    }

    public bool AbandonVirtue(KnightVirtueDef virtue)
    {
        if (!RemoveVirtue(virtue))
            return false;

        float meditationPointsToReduce = knight.AcademicHandler.TotalAcademicLevel.Value * 500f;
        if (virtue.chivalry.IsSameDefNonNullable(knight.Chivalry))
        {
            meditationPointsToReduce *= 2f;
        }

        knight.MeditationPoints -= meditationPointsToReduce;

        return true;
    }

    public void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo)
    {
        if (knight.CurState != ResidentPawnState.Normal)
            return;

        for (int i = 0; i < virtues.Count; i++)
        {
            virtues[i].Notify_KilledPawn(victim, dinfo);
        }
    }

    public void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        if (knight.CurState != ResidentPawnState.Normal)
            return;

        for (int i = 0; i < virtues.Count; i++)
        {
            virtues[i].Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
        }
    }

    public void Notify_Stimulate(Pawn recipient)
    {
        if (knight.CurState != ResidentPawnState.Normal)
            return;

        for (int i = 0; i < virtues.Count; i++)
        {
            virtues[i].Notify_Stimulate(recipient);
        }
    }

    public HediffStage GetNewBuffStage()
    {
        if (!BuffStageTemplate.IsReady)
        {
            RefreshBuffStage();
        }

        return BuffStageTemplate.BuildNewHediffStage();
    }

    private bool AddVirtue(KnightVirtueDef virtueDef, int level)
    {
        if (HasVirtue(virtueDef))
        {
            return false;
        }
        KnightVirtue virtue = KnightVirtue.GenerateKnightVirtue(knight, virtueDef, level);
        virtues.Add(virtue);
        virtue.PostAdd();
        ActiveVirtue(virtue);
        VirtuesChanged();
        return true;
    }

    private void ActiveVirtue(KnightVirtue virtue)
    {
        if (virtue is ITickInterval tickIntervalProcessor)
        {
            RegisterTickIntervalProcessor(tickIntervalProcessor);
        }

        virtue.PostActive();
    }

    private bool RemoveVirtue(KnightVirtueDef virtueDef)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtueDef == virtues[i].Def)
            {
                KnightVirtue virtue = virtues[i];
                virtues.RemoveAt(i);

                if (virtue is ITickInterval tickIntervalProcessor)
                {
                    DeregisterTickIntervalProcessor(tickIntervalProcessor);
                }

                VirtuesChanged();
                return true;
            }
        }

        return false;
    }

    public void RegisterTickIntervalProcessor(ITickInterval tickIntervalProcessor) => tickIntervalVirtues.AddDistinct(tickIntervalProcessor);

    public void DeregisterTickIntervalProcessor(ITickInterval tickIntervalProcessor) => tickIntervalVirtues.Remove(tickIntervalProcessor);

    /// <returns>升级后的新等级，返回 -1 表示升级失败</returns>
    private int UpgradeVirtue(KnightVirtue virtue, int upgrade)
    {
        if (virtue.Level >= virtue.Def.maxLevel)
        {
            return -1;
        }
        virtue.Level += upgrade;
        VirtuesChanged();
        return virtue.Level;
    }

    private void VirtuesChanged()
    {
        BuffStageTemplate.MarkInvalid();
    }

    private void CheckKnightCreed()
    {
        float virtueStatvalue = VirtueStatValueCache.GetCachedResult();
        int targetKnightCreedLevel = virtueStatvalue switch
        {
            < 15f => 0,
            < 30f => 1,
            _ => 2
        };

        if (curKnightCreedLevel != targetKnightCreedLevel)
        {
            curKnightCreedLevel = targetKnightCreedLevel;
            if (targetKnightCreedLevel <= 0)
            {
                Pawn.RemoveFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_KnightCreed);
            }
            else
            {
                Hediff hediff = Pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_KnightCreed);
                hediff.Severity = targetKnightCreedLevel;
            }
        }
    }

    private void RefreshBuffStage()
    {
        BuffStageTemplate.ResetTemplate();
        HediffStageModifierBuilder tempTemplate = new();

        VirtueStatValueCache.Reset();
        float virtueStatvalue = VirtueStatValueCache.GetCachedResult();
        StringBuilder buffDetailExplanationBuilder = new(128);

        foreach (KnightVirtue virtue in virtues)
        {
            tempTemplate.ResetTemplate();
            KnightVirtueDef virtueDef = virtue.Def;
            tempTemplate.AddOffsets(virtueDef.statOffsets);
            tempTemplate.AddFactors(virtueDef.statFactors);
            if (virtueDef.statOffsetsByVirtue is not null)
            {
                foreach (StatModifierBySeverity statOffsetByVirtue in virtueDef.statOffsetsByVirtue)
                    tempTemplate.AddOffset(statOffsetByVirtue.stat, statOffsetByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
            }

            if (virtueDef.statFactorsByVirtue is not null)
            {
                foreach (StatModifierBySeverity statFactorByVirtue in virtueDef.statFactorsByVirtue)
                    tempTemplate.AddFactor(statFactorByVirtue.stat, statFactorByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
            }


            foreach (KnightVirtue.KnightVirtueTrait virtueTrait in virtue.SelectedTraits)
                ApplyTraitStatModifiers(virtueTrait.def);

            virtue.OnRefreshBuffStage(tempTemplate);

            if (!tempTemplate.HasAnyModifier)
                continue;

            buffDetailExplanationBuilder.AppendLine(virtue.Def.LabelCap);
            foreach ((StatDef stat, float value) in tempTemplate.OffsetDictForReading)
            {
                BuffStageTemplate.AddOffset(stat, value);
                buffDetailExplanationBuilder.AppendLine($"    {stat.LabelCap}: {stat.Worker.ValueToString(value, finalized: false)}");
            }

            foreach ((StatDef stat, float value) in tempTemplate.OffsetDictForReading)
            {
                BuffStageTemplate.AddFactor(stat, value);
                buffDetailExplanationBuilder.AppendLine($"    {stat.LabelCap}: {stat.Worker.ValueToString(value, finalized: false)}");
            }
        }

        BuffDetailExplanation = buffDetailExplanationBuilder.ToString();
        BuffStageTemplate.FinalizeTemplate();

        void ApplyTraitStatModifiers(KnightVirtueTraitDef virtueTraitDef)
        {
            if (virtueTraitDef is null)
                return;

            tempTemplate.AddOffsets(virtueTraitDef.statOffsets);
            tempTemplate.AddFactors(virtueTraitDef.statFactors);

            if (virtueTraitDef.statOffsetsByVirtue is not null)
            {
                foreach (StatModifierBySeverity statOffsetByVirtue in virtueTraitDef.statOffsetsByVirtue)
                    tempTemplate.AddOffset(statOffsetByVirtue.stat, statOffsetByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
            }

            if (virtueTraitDef.statFactorsByVirtue is not null)
            {
                foreach (StatModifierBySeverity statFactorByVirtue in virtueTraitDef.statFactorsByVirtue)
                    tempTemplate.AddFactor(statFactorByVirtue.stat, statFactorByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
            }
        }
    }

}