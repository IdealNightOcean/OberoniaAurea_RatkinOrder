using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightsManager : IExposable, IOnBranchDestroyed
{
    public static ResidentKnightsManager Instance { get; private set; }

    public static int ResidentKnightCeiling
    {
        get
        {
            int ceiling = 1;
            ceiling += OrderHallHandler.Instance.OrderHallLevel switch
            {
                < 3 => 0,
                < 5 => 3,
                < 6 => 5,
                _ => 7
            };
            return ceiling;
        }
    }

    private Dictionary<Pawn, ResidentKnightRecord> residentKnights = [];
    private Dictionary<ResidentKnightRoleDef, ResidentKnightRecord> rolesToKnights = [];

    public int KnightsCount => residentKnights.Count;
    public IReadOnlyDictionary<Pawn, ResidentKnightRecord> ResidentKnights => residentKnights;
    public IReadOnlyDictionary<ResidentKnightRoleDef, ResidentKnightRecord> RolesToKnights => RolesToKnights;

    public LazyMutable<float> MinResignationDays { get; }
    public LazyMutable<KnightPersonality> AllHasPersonalityTypes { get; }

    public LazyMutable<int> InstructorKnightsCount { get; }
    public LazyMutable<int> LawOrderKnightsCount { get; }

    [Unsaved] private readonly Dictionary<StatDef, float> statOffsets = [];
    [Unsaved] private readonly Dictionary<StatDef, float> statFactors = [];
    [Unsaved] private readonly HediffStage buffHediffStage;
    private int NextBuffStatRegainTick { get; set; } = -1;

    public HediffStage BuffHediffStage
    {
        get
        {
            if (Find.TickManager.TicksGame > NextBuffStatRegainTick)
            {
                RegainRoleBuffStat();
            }
            return buffHediffStage;
        }
    }

    private List<Pawn> residentKnightKeys;
    private List<ResidentKnightRecord> residentKnightValues;

    private List<ResidentKnightRoleDef> rolesToKnightKeys;
    private List<ResidentKnightRecord> rolesToKnightValues;

    internal ResidentKnightsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AcceptedBranchDemandHandler));
        Instance = this;

        buffHediffStage = new HediffStage();
        MinResignationDays = new(refreshFunc: RefreshMinResignationDays);
        AllHasPersonalityTypes = new(refreshFunc: () => residentKnights.Values.Aggregate(KnightPersonality.None, (acc, rk) => acc | (rk?.Personality ?? KnightPersonality.None)));

        InstructorKnightsCount = new(refreshFunc: () => residentKnights.Values.Where(rk => rk?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_Instructor).Count());
        LawOrderKnightsCount = new(refreshFunc: () => residentKnights.Values.Where(rk => rk?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_LawOrder).Count());
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref residentKnights, nameof(residentKnights), LookMode.Reference, LookMode.Deep, ref residentKnightKeys, ref residentKnightValues);
        Scribe_Collections.Look(ref rolesToKnights, nameof(rolesToKnights), LookMode.Def, LookMode.Reference, ref rolesToKnightKeys, ref rolesToKnightValues);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (residentKnights.RemoveAll(kv => kv.Value is null || kv.Value.ShouldRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentKnightsManager)} 的部分常驻骑士记录在加载后为null或无效，已被移除。");
            }
            if (rolesToKnights.RemoveAll(kv => kv.Value is null || kv.Value.ShouldRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentKnightsManager)} 的部分常驻骑士角色在加载后为null或无效，已被移除。");
            }

            NextBuffStatRegainTick = -1;
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"常驻骑士总数: {residentKnights.Count}");
        if (residentKnights.NullOrEmpty())
        {
            listing_Rect.SubLabel("None".Translate(), widthPct: 0.8f);
        }
        else
        {
            foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
            {
                listing_Rect.SubLabel(kv.Key.Name + ": " + kv.Value.ToString(), widthPct: 0.8f);
            }
        }

        listing_Rect.Gap(12f);
        listing_Rect.Label($"常驻骑士职位: {rolesToKnights.Count}");
        if (rolesToKnights.NullOrEmpty())
        {
            listing_Rect.SubLabel("None".Translate(), widthPct: 0.8f);
        }
        else
        {
            foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnightRecord> kv in rolesToKnights)
            {
                listing_Rect.SubLabel(kv.Key.label + ": " + kv.Value.Knight.Name, widthPct: 0.8f);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentKnight(Pawn pawn) => residentKnights.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out ResidentKnightRecord record) => residentKnights.TryGetValue(pawn, out record);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightOfRole(ResidentKnightRoleDef roleDef, out ResidentKnightRecord record) => rolesToKnights.TryGetValue(roleDef, out record);

    public void TickDay()
    {
        List<Pawn> toRemove = [];
        int ticksGame = Find.TickManager.TicksGame;
        foreach ((Pawn knight, ResidentKnightRecord record) in residentKnights)
        {
            if (record.IsValid)
            {
                float gainPoints = record.Knight.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationDailyGain);
                record.MeditationPoints += gainPoints;
            }
            else
            {
                toRemove.Add(knight);
            }

            if (record.ResignationTick <= ticksGame)
            {
                if (RatkinOrderSettings.AutoPostponeResignationResidentKnight)
                {
                    record.PostponeResignation(120);
                }
                else
                {
                    toRemove.Add(knight);
                }
            }
        }
        if (toRemove.Count > 0)
        {
            foreach (Pawn p in toRemove)
            {
                RemoveResidentKnight(p);
            }
        }

        MinResignationDays.MarkDirty();
    }

    private void RemoveAllInvalidRecord(Predicate<ResidentKnightRecord> extraRemove = null)
    {
        HashSet<Pawn> pawnsToRemove = [];
        HashSet<ResidentKnightRoleDef> rolesToRemove = [];

        if (extraRemove is null)
        {
            foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
            {
                (Pawn knight, ResidentKnightRecord record) = kv;
                if (record is null || !record.Branch.IsValid())
                {
                    pawnsToRemove.Add(knight);
                    if (record?.CurRole is not null)
                    {
                        rolesToRemove.Add(record.CurRole);
                    }
                }
            }
        }
        else
        {
            foreach (KeyValuePair<Pawn, ResidentKnightRecord> kv in residentKnights)
            {
                (Pawn knight, ResidentKnightRecord record) = kv;
                if (record is null || !record.Branch.IsValid() || extraRemove.Invoke(record))
                {
                    pawnsToRemove.Add(knight);
                    if (record?.CurRole is not null)
                    {
                        rolesToRemove.Add(record.CurRole);
                    }
                }
            }
        }

        foreach (Pawn p in pawnsToRemove)
        {
            RemoveResidentKnight(p);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder) => RemoveAllInvalidRecord((record) => record.Branch.RatkinOrder == ratkinOrder);
    public void Notify_BranchDestroyed(Branch branch) => RemoveAllInvalidRecord((record) => record.Branch == branch);

    public void AddResidentKnight(Pawn pawn)
    {
        if (!KnightPawnsManager.Instance.TryGetKnightRecord(pawn, out KnightRecord kRecord))
        {
            Log.Error($"[OARO] 尝试将非骑士单位添加到 {nameof(ResidentKnightsManager)}");
            return;
        }

        AddResidentKnight(pawn, kRecord);
    }

    public void AddResidentKnight(Pawn pawn, KnightRecord knightRecord)
    {
        if (!residentKnights.ContainsKey(pawn))
        {
            residentKnights.Add(pawn, new ResidentKnightRecord(pawn, knightRecord));
            OnKnightsChanged();
        }
    }

    public void RemoveResidentKnight(Pawn pawn)
    {
        if (pawn is null || !residentKnights.TryGetValue(pawn, out ResidentKnightRecord record))
        {
            return;
        }
        residentKnights.Remove(pawn);
        if (record.CurRole is not null)
        {
            rolesToKnights.Remove(record.CurRole);
            NextBuffStatRegainTick = -1;
        }

        record.PostRemoved();
        OnKnightsChanged();
    }

    public bool TrySetResidentKnightRole(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (SetResidentKnightRole(pawn, roleDef, replaceCurRole))
        {
            NextBuffStatRegainTick = -1;
            return true;
        }
        return false;
    }

    public void AllResidentKnightsGainMeditation(float gain, RatkinOrder ratkinOrder = null, bool directly = false)
    {
        foreach (ResidentKnightRecord record in residentKnights.Values)
        {
            if (ratkinOrder is null || record.RatkinOrder == ratkinOrder)
            {
                float finnalGain = gain * (directly ? 1f : record.Knight.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor));
                record.MeditationPoints += finnalGain;
            }
        }
    }

    public void Notify_SquadBeAttackedOnTask(RatkinOrder ratkinOrder, Branch branch)
    {
        foreach ((Pawn p, ResidentKnightRecord record) in ResidentKnights)
        {
            if (record.RatkinOrder != ratkinOrder || p.needs is null || p.needs.mood is null)
            {
                continue;
            }
            int forceStage = record.Branch == branch ? 1 : 0;
            Thought_Memory memory = ThoughtMaker.MakeThought(OARO_ThoughtDefOf.OARO_Thought_ResidentKnight_SquadBeAttackedOnTask, forceStage);
            p.needs.mood.thoughts.memories.TryGainMemory(memory);
        }
    }

    private bool SetResidentKnightRole(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (!residentKnights.TryGetValue(pawn, out ResidentKnightRecord pawnRecord))
        {
            return false;
        }

        if (rolesToKnights.TryGetValue(roleDef, out ResidentKnightRecord curRolePawnRecord))
        {
            if (curRolePawnRecord.Knight == pawn)
            {
                return true;
            }
            if (!replaceCurRole)
            {
                return false;
            }
        }

        ResidentKnightRoleDef pOldRole = pawnRecord.CurRole;

        switch (curRolePawnRecord, pOldRole)
        {
            //新增职位
            case (null, null):
                {
                    pawnRecord.ChangeRole(roleDef);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
            //两人交接职位
            case (not null, null):
                {
                    curRolePawnRecord.ChangeRole(null);
                    pawnRecord.ChangeRole(roleDef);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
            //本人职位改变
            case (null, not null):
                {
                    pawnRecord.ChangeRole(roleDef);
                    rolesToKnights.Remove(pOldRole);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
            //替代对方职位
            case (not null, not null):
                {
                    curRolePawnRecord.ChangeRole(null);
                    pawnRecord.ChangeRole(roleDef);

                    rolesToKnights.Remove(pOldRole);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
        }

        return true;
    }

    private float RefreshMinResignationDays()
    {
        if (residentKnights.Count <= 0)
        {
            return -1f;
        }
        float minResignationDays = float.MaxValue;
        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnightRecord record in residentKnights.Values)
        {
            if (record.ResignationTick > 0)
            {
                float resignationDays = Mathf.Max(0f, (record.ResignationTick - ticksGame) / 60000f);
                if (resignationDays < minResignationDays)
                {
                    minResignationDays = resignationDays;
                }
            }
        }

        return minResignationDays;
    }

    private void OnKnightsChanged()
    {
        MinResignationDays.MarkDirty();
        AllHasPersonalityTypes.MarkDirty();
        InstructorKnightsCount.MarkDirty();
        LawOrderKnightsCount.MarkDirty();
    }


    private void RegainRoleBuffStat()
    {
        statOffsets.Clear();
        statFactors.Clear();
        NextBuffStatRegainTick = Find.TickManager.TicksGame + 60000;

        if (LawOrderKnightsCount.Value > 0)
        {
            AddStatModifier(
                modifiers: [new StatModifier()
                {
                    stat = StatDefOf.GlobalLearningFactor,
                    value = Mathf.Min(LawOrderKnightsCount.Value * 0.12f,0.6f)
                }],
                isFactor: false);
        }

        foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnightRecord> kv in rolesToKnights)
        {
            (ResidentKnightRoleDef roldDef, Pawn pawn) = (kv.Key, kv.Value.Knight);

            AddStatModifier(roldDef.statOffsets, isFactor: false);
            AddStatModifier(roldDef.RoleWorker.RoleStatOffsets(pawn), isFactor: false);

            AddStatModifier(roldDef.statFactors, isFactor: true);
            AddStatModifier(roldDef.RoleWorker.RoleStatFactors(pawn), isFactor: true);
        }

        buffHediffStage.statOffsets = statOffsets.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();
        buffHediffStage.statFactors = statFactors.Select(pair => new StatModifier { stat = pair.Key, value = pair.Value }).ToList();

        statOffsets.Clear();
        statFactors.Clear();

        void AddStatModifier(IEnumerable<StatModifier> modifiers, bool isFactor)
        {
            if (modifiers is null)
            {
                return;
            }
            Dictionary<StatDef, float> target = isFactor ? statFactors : statOffsets;

            foreach (StatModifier modifier in modifiers)
            {
                if (target.TryGetValue(modifier.stat, out float curValue))
                {
                    if (isFactor)
                    {
                        target[modifier.stat] = curValue * modifier.value;
                    }
                    else
                    {
                        target[modifier.stat] = curValue + modifier.value;
                    }
                }
                else
                {
                    target[modifier.stat] = modifier.value;
                }
            }
        }
    }
}