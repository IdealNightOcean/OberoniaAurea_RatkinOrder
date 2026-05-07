using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// （常驻）骑士美德处理器 - 负责管理骑士的美德，包括美德的获取、升级、放弃等，以及与美德相关的buff计算和骑士信条等级检测
/// </summary>
public class KnightVirtueHandler : IExposable
{
    private readonly ResidentKnight knight;
    public Pawn Pawn => knight.Pawn;

    /// <summary>
    /// 骑士信条等级
    /// </summary>
    private int curKnightCreedLevel;
    private List<KnightVirtue> virtues = [];

    public IReadOnlyList<KnightVirtue> Virtues => virtues;
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
            foreach (KnightVirtue virtue in virtues)
                if (virtue.HasUnusedTraitSlot)
                    return true;
            return false;
        }
    }

    public bool HasVirtueOfChivalry(KnightChivalryDef chivalry)
    {
        foreach (KnightVirtue virtue in virtues)
        {
            if (virtue.Chivalry == chivalry)
                return true;
        }
        return false;
    }

    public int GetVirtueCountOfChivalry(KnightChivalryDef chivalry)
    {
        int count = 0;
        foreach (KnightVirtue virtue in virtues)
        {
            if (virtue.Chivalry == chivalry)
                count++;
        }
        return count;
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

    private HediffStageTemplate BuffStageTemplate { get; } = new();

    public KnightVirtueHandler(ResidentKnight knight)
    {
        this.knight = knight ?? throw new ArgumentNullException(nameof(knight));

        VirtueStatValueCache = new(cacheInterval: 30000,
                                   checker: () => Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref virtues, nameof(virtues), LookMode.Deep);
    }

    public void TickHour()
    {
        CheckKnightCreed();
    }

    public bool HasVirtue(KnightVirtueDef virtueDef)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtueDef == virtues[i].Def)
            {
                return true;
            }
        }
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
        {
            return TryAddVirtue(virtueDef, 1, reason);
        }

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

    public KnightVirtue GetVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                return virtues[i];
            }
        }
        return null;
    }

    public bool AbandonVirtue(ResidentKnight record, KnightVirtueDef virtue)
    {
        if (!RemoveVirtue(virtue))
        {
            return false;
        }
        float meditationPointsToReduce = record.AcademicHandler.TotalAcademicLevel.Value * 500f;
        if (virtue.chivalry.IsSameDefNonNullable(record.Chivalry))
        {
            meditationPointsToReduce *= 2f;
        }

        record.MeditationPoints -= meditationPointsToReduce;

        return true;
    }

    public HediffStage GetNewBuffStage()
    {
        if (!BuffStageTemplate.IsReady)
        {
            RefreshBuffStage();
        }

        return BuffStageTemplate.GetNewHediffStage();
    }

    private bool AddVirtue(KnightVirtueDef virtueDef, int level)
    {
        if (HasVirtue(virtueDef))
        {
            return false;
        }
        virtues.Add(new KnightVirtue(virtueDef, level));
        VirtuesChanged();
        return true;
    }

    private bool RemoveVirtue(KnightVirtueDef virtue)
    {
        for (int i = 0; i < virtues.Count; i++)
        {
            if (virtue == virtues[i].Def)
            {
                virtues.RemoveAt(i);
                VirtuesChanged();
                return true;
            }
        }

        return false;
    }

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

        VirtueStatValueCache.Reset();
        float virtueStatvalue = VirtueStatValueCache.GetCachedResult();
        foreach (KnightVirtue virtue in virtues)
        {
            foreach (KnightVirtue.KnightVirtueTrait virtueTrait in virtue.SelectedTraits)
            {
                KnightVirtueTraitDef virtueTraitDef = virtueTrait.def;
                BuffStageTemplate.AddOffsets(virtueTraitDef.statOffsets);
                BuffStageTemplate.AddOffsets(virtueTraitDef.statFactors);

                if (virtueTraitDef.statOffsetsByVirtue is not null)
                {
                    foreach (StatModifierBySeverity statOffsetByVirtue in virtueTraitDef.statOffsetsByVirtue)
                    {
                        BuffStageTemplate.AddOffset(statOffsetByVirtue.stat, statOffsetByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
                    }
                }

                if (virtueTraitDef.statFactorsByVirtue is not null)
                {
                    foreach (StatModifierBySeverity statFactorByVirtue in virtueTraitDef.statFactorsByVirtue)
                    {
                        BuffStageTemplate.AddFactor(statFactorByVirtue.stat, statFactorByVirtue.valueBySeverity.Evaluate(virtueStatvalue));
                    }
                }
            }
        }

        BuffStageTemplate.FinalizeTemplate();
    }

}