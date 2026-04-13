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

/// <summary>
/// 常驻人员管理器
/// </summary>
public class ResidentPawnsManager : IExposable, IOnBranchDestroyed
{
    public static ResidentPawnsManager Instance { get; private set; }

    public static int ResidentKnightCeiling
    {
        get
        {
            int ceiling = 1;
            ceiling += OrderStationHandler.Instance.OrderHallLevel switch
            {
                < 3 => 0,
                < 5 => 3,
                < 6 => 5,
                _ => 7
            };
            return ceiling;
        }
    }

    private Dictionary<Pawn, ResidentKnight> residentKnights = [];
    private Dictionary<ResidentKnightRoleDef, ResidentKnight> rolesToKnights = [];
    private Dictionary<Pawn, ResidentPawn> residentColonists = [];

    public int KnightsCount => residentKnights.Count;
    public IReadOnlyDictionary<Pawn, ResidentKnight> ResidentKnights => residentKnights;
    public IReadOnlyDictionary<ResidentKnightRoleDef, ResidentKnight> RolesToKnights => RolesToKnights;


    private MentorshipManager mentorshipManager;
    public MentorshipManager MentorshipManager => mentorshipManager;

    public LazyMutable<float> MinResignationDays { get; }
    public LazyMutable<KnightPersonality> AllHasPersonalityTypes { get; }

    public LazyMutable<int> InstructorKnightsCount { get; }
    public LazyMutable<int> LawOrderKnightsCount { get; }

    private HediffStageTemplate BuffStageTemplate { get; }
    private int nextBuffStageForceRefreshTick;

    private List<Pawn> residentKnightKeys;
    private List<ResidentKnight> residentKnightValues;

    private List<ResidentKnightRoleDef> rolesToKnightKeys;
    private List<ResidentKnight> rolesToKnightValues;

    internal ResidentPawnsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AcceptedBranchDemandHandler));
        Instance = this;

        BuffStageTemplate = new();
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
        Scribe_Collections.Look(ref residentColonists, nameof(residentColonists), LookMode.Reference, LookMode.Deep);

        Scribe_Deep.Look(ref mentorshipManager, nameof(mentorshipManager));

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (residentKnights.RemoveAll(kv => kv.Value is null || kv.Value.ShouldRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentPawnsManager)} 的部分常驻骑士记录在加载后为null或无效，已被移除。");
            }
            if (rolesToKnights.RemoveAll(kv => kv.Value is null || kv.Value.ShouldRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentPawnsManager)} 的部分常驻骑士角色在加载后为null或无效，已被移除。");
            }
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
            foreach (KeyValuePair<Pawn, ResidentKnight> kv in residentKnights)
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
            foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnight> kv in rolesToKnights)
            {
                listing_Rect.SubLabel(kv.Key.label + ": " + kv.Value.Pawn.Name, widthPct: 0.8f);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentKnight(Pawn pawn) => residentKnights.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out ResidentKnight record) => residentKnights.TryGetValue(pawn, out record);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightOfRole(ResidentKnightRoleDef roleDef, out ResidentKnight record) => rolesToKnights.TryGetValue(roleDef, out record);

    public void RegisterKnight(Pawn pawn, KnightRecord knightRecord = null)
    {
        if (knightRecord is null && !KnightPawnsManager.Instance.TryGetKnightRecord(pawn, out knightRecord))
        {
            Log.Error($"[OARO] 尝试将非骑士单位添加到 {nameof(ResidentPawnsManager)}");
            return;
        }

        if (!IsResidentKnight(pawn))
        {
            RegisterKnightDirectly(pawn, knightRecord);
        }
        RegisterKnight(pawn, knightRecord);
    }

    public void DeregisterKnight(Pawn pawn, ResidentKnightRemovalReason reason)
    {
        if (pawn is null || !residentKnights.TryGetValue(pawn, out ResidentKnight record))
        {
            return;
        }

        DeregisterKnightDirectly(record, reason);
    }

    public bool TrySetKnightRole(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (SetResidentKnightRole(pawn, roleDef, replaceCurRole))
        {
            BuffStageTemplate.MarkInvalid();
            return true;
        }
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentColonist(Pawn pawn) => residentColonists.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetColonistRecord(Pawn pawn, out ResidentPawn record) => residentColonists.TryGetValue(pawn, out record);

    public void RegisterColonist(Pawn pawn)
    {
        if (pawn is null)
            return;
        if (IsResidentKnight(pawn) || IsResidentColonist(pawn))
            return;

        residentColonists.Add(pawn, new ResidentPawn(pawn));
    }

    public void DeregisterColonist(Pawn pawn)
    {
        if (pawn is null)
            return;
        if (!IsResidentColonist(pawn))
            return;

        DeregisterColonistDirectly(pawn);
    }


    public void TickDay()
    {
        if (Find.TickManager.TicksGame > nextBuffStageForceRefreshTick)
        {
            nextBuffStageForceRefreshTick = Find.TickManager.TicksGame + 60000;
            BuffStageTemplate.MarkInvalid();
        }

        DailyColonistsCheck();
        DailyKnightsCheck();
        mentorshipManager.TickDay();
    }

    /// <summary>
    /// 获取新的Buff阶段。会根据当前常驻骑士的职位情况刷新Buff阶段模板。
    /// </summary>
    public HediffStage GetNewBuffStage()
    {
        if (!BuffStageTemplate.IsReady)
        {
            RefreshRoleBuffStageTemplate();
        }

        return BuffStageTemplate.GetNewHediffStage();
    }

    public void AllKnightsGainMeditation(float gain, RatkinOrder ratkinOrder = null, bool directly = false)
    {
        foreach (ResidentKnight record in residentKnights.Values)
        {
            if (ratkinOrder is null || record.RatkinOrder == ratkinOrder)
            {
                float finnalGain = gain * (directly ? 1f : record.Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor));
                record.MeditationPoints += finnalGain;
            }
        }
    }

    public void Notify_SquadBeAttackedOnTask(RatkinOrder ratkinOrder, Branch branch)
    {
        foreach ((Pawn p, ResidentKnight record) in ResidentKnights)
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


    private void DailyColonistsCheck()
    {
        List<Pawn> toRemove = [];
        foreach (KeyValuePair<Pawn, ResidentPawn> kv in residentColonists)
        {
            if (kv.Value.ShouldRemove)
            {
                toRemove.Add(kv.Key);
            }
            else
            {
                kv.Value.CheckPendingRemoval();
            }
        }
        foreach (Pawn pawn in toRemove)
        {
            mentorshipManager.RemoveStudent(pawn);
            residentColonists.Remove(pawn);
        }
    }
    private void DailyKnightsCheck()
    {
        List<ResidentKnight> toRemove = [];
        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnight record in residentKnights.Values)
        {
            record.CheckPendingRemoval();
            if (record.ShouldRemove)
            {
                toRemove.Add(record);
            }
            else
            {
                float gainPoints = record.Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationDailyGain);
                record.MeditationPoints += gainPoints;
            }

            if (record.ResignationTick <= ticksGame)
            {
                if (RatkinOrderSettings.AutoPostponeResignationResidentKnight)
                {
                    record.PostponeResignation(120);
                }
                else
                {
                    toRemove.Add(record);
                }
            }
        }

        foreach (ResidentKnight r in toRemove)
        {
            DeregisterKnightDirectly(r, ResidentKnightRemovalReason.Overdue);
        }

        MinResignationDays.MarkDirty();
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder) => RemoveAllInvalidRecord(extraRemove: (record) => record.RatkinOrder == ratkinOrder,
                                                                                             extraRemoveReason: ResidentKnightRemovalReason.OrderDestory);
    public void Notify_BranchDestroyed(Branch branch) => RemoveAllInvalidRecord(extraRemove: (record) => record.Branch == branch,
                                                                                extraRemoveReason: ResidentKnightRemovalReason.BranchDestory);

    private void RegisterKnightDirectly(Pawn pawn, KnightRecord knightRecord)
    {
        residentKnights.Add(pawn, new ResidentKnight(pawn, knightRecord));
        OnKnightsChanged();
    }
    private void DeregisterColonistDirectly(Pawn pawn)
    {
        if (!residentColonists.Remove(pawn))
        {
            return;
        }

        mentorshipManager.RemoveStudent(pawn);
    }

    private void DeregisterKnightDirectly(ResidentKnight record, ResidentKnightRemovalReason reason)
    {
        if (!residentKnights.Remove(record.Pawn))
        {
            return;
        }

        mentorshipManager.RemoveTeacher(record);
        if (record.CurRole is not null)
        {
            rolesToKnights.Remove(record.CurRole);
            BuffStageTemplate.MarkInvalid();
        }
        record.PostRemoved(reason);
        OnKnightsChanged();
    }

    private bool SetResidentKnightRole(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (!residentKnights.TryGetValue(pawn, out ResidentKnight pawnRecord))
        {
            return false;
        }

        if (rolesToKnights.TryGetValue(roleDef, out ResidentKnight curRolePawnRecord))
        {
            if (curRolePawnRecord.Pawn == pawn)
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

    private void RemoveAllInvalidRecord(Predicate<ResidentKnight> extraRemove = null, ResidentKnightRemovalReason extraRemoveReason = ResidentKnightRemovalReason.Unknown)
    {
        List<ResidentKnight> recordsToRemove = [];
        List<ResidentKnight> recordsToExtraRemove = extraRemove is null ? null : [];
        foreach (ResidentKnight record in residentKnights.Values)
        {
            if (record is null || record.Pawn is null)
            {
                recordsToRemove.Add(record);
            }
            else if ((extraRemove is not null && extraRemove(record)))
            {
                recordsToExtraRemove.Add(record);
            }
        }

        foreach (ResidentKnight r in recordsToRemove)
        {
            DeregisterKnightDirectly(r, ResidentKnightRemovalReason.Invalid);
        }
        if (!recordsToExtraRemove.NullOrEmpty())
        {
            foreach (ResidentKnight r in recordsToRemove)
            {
                DeregisterKnightDirectly(r, extraRemoveReason);
            }
        }
    }

    private float RefreshMinResignationDays()
    {
        if (residentKnights.Count <= 0)
        {
            return -1f;
        }
        float minResignationDays = float.MaxValue;
        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnight record in residentKnights.Values)
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

    private void RefreshRoleBuffStageTemplate()
    {
        BuffStageTemplate.ResetTemplate();

        if (LawOrderKnightsCount.Value > 0)
        {
            BuffStageTemplate.AddOffset(StatDefOf.GlobalLearningFactor, Mathf.Min(LawOrderKnightsCount.Value * 0.12f, 0.6f));
        }

        foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnight> kv in rolesToKnights)
        {
            (ResidentKnightRoleDef roldDef, Pawn pawn) = (kv.Key, kv.Value.Pawn);

            BuffStageTemplate.AddOffsets(roldDef.statOffsets);
            BuffStageTemplate.AddOffsets(roldDef.RoleWorker.RoleStatOffsets(pawn));

            BuffStageTemplate.AddFactors(roldDef.statFactors);
            BuffStageTemplate.AddFactors(roldDef.RoleWorker.RoleStatFactors(pawn));
        }

        nextBuffStageForceRefreshTick = Find.TickManager.TicksGame + 60000;
        BuffStageTemplate.FinalizeTemplate();
    }
}