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
    private readonly int tickHashOffset;

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
    public int KnightsCount => residentKnights.Count;
    public IReadOnlyDictionary<Pawn, ResidentKnight> ResidentKnights => residentKnights;

    private Dictionary<Pawn, ResidentPawn> residentColonists = [];
    public IReadOnlyDictionary<Pawn, ResidentPawn> ResidentColonists => residentColonists;

    private ResidentRoleManager roleManager;
    public static ResidentRoleManager RoleManager => Instance?.roleManager;

    private MentorshipManager mentorshipManager;
    public static MentorshipManager MentorshipManager => Instance?.mentorshipManager;

    public LazyMutable<float> MinResignationDays { get; }
    public LazyMutable<KnightPersonality> AllHasPersonalityTypes { get; }

    public LazyMutable<int> InstructorKnightsCount { get; }

    internal ResidentPawnsManager(bool initCtor)
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AcceptedBranchDemandHandler));
        Instance = this;
        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();

        MinResignationDays = new(refreshFunc: RefreshMinResignationDays);
        AllHasPersonalityTypes = new(refreshFunc: () => residentKnights.Values.Aggregate(KnightPersonality.None, (acc, rk) => acc | (rk?.Personality ?? KnightPersonality.None)));

        InstructorKnightsCount = new(refreshFunc: () => residentKnights.Values.Where(rk => rk?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_Instructor).Count());

        if (initCtor)
        {
            EnsureComponentsInit();
        }
    }

    public static void ClearStaticCache() => Instance = null;

    private void EnsureComponentsInit()
    {
        roleManager ??= new ResidentRoleManager(this);
        mentorshipManager ??= new MentorshipManager(this);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref residentKnights, nameof(residentKnights), LookMode.Reference, LookMode.Deep, ref residentKnightKeys, ref residentKnightValues);
        Scribe_Collections.Look(ref residentColonists, nameof(residentColonists), LookMode.Reference, LookMode.Deep, ref residentColonistKeys, ref residentColonistValues);

        Scribe_Deep.Look(ref roleManager, nameof(roleManager), ctorArgs: this);
        Scribe_Deep.Look(ref mentorshipManager, nameof(mentorshipManager), ctorArgs: this);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnsureComponentsInit();
            if (residentKnights.RemoveAll(kv => kv.Value is null || kv.Value.CurState == ResidentPawnState.ForceRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentPawnsManager)} 的部分常驻骑士记录在加载后为null或无效，已被移除。");
            }
            if (residentColonists.RemoveAll(kv => kv.Value is null || kv.Value.CurState == ResidentPawnState.ForceRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentPawnsManager)} 的部分常驻殖民者记录在加载后为null或无效，已被移除。");
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
        roleManager.DrawDevWindow(listing_Rect);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentKnight(Pawn pawn) => residentKnights.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out ResidentKnight record) => residentKnights.TryGetValue(pawn, out record);

    public void RegisterKnight(Pawn pawn, KnightRecord knightRecord = null)
    {
        if (knightRecord is null && !KnightPawnsManager.Instance.TryGetKnightRecord(pawn, out knightRecord))
        {
            Log.Error($"[OARO] 尝试将非骑士单位添加到 {nameof(ResidentPawnsManager)}");
            return;
        }
        if (knightRecord.Pawn != pawn)
        {
            Log.Error($"[OARO] 骑士记录的 Pawn ({knightRecord.Pawn}) 与尝试注册的 Pawn ({pawn}) 不匹配，无法添加到 {nameof(ResidentPawnsManager)}");
            return;
        }

        if (!IsResidentKnight(pawn))
        {
            RegisterKnightDirectly(knightRecord);
        }
    }

    public void DeregisterKnight(Pawn pawn, ResidentKnightRemovalReason reason)
    {
        if (pawn is null || !residentKnights.TryGetValue(pawn, out ResidentKnight record))
        {
            return;
        }

        DeregisterKnightDirectly(record, reason);
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

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, 2500))
        {
            if (TickUtility.IsHashIntervalTick(tickHashOffset, 60000))
            {
                DailyColonistsCheck();
                DailyKnightsCheck();
                mentorshipManager.TickDay();
            }


        }

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
            kv.Value.CheckPendingRemoval();
            if (kv.Value.CurState == ResidentPawnState.ForceRemove)
            {
                toRemove.Add(kv.Key);
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
        List<(ResidentKnight, ResidentPawnState)> toProcess = [];
        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnight record in residentKnights.Values)
        {
            record.CheckPendingRemoval();
            ResidentPawnState knightState = record.CurState;
            switch (knightState)
            {
                case ResidentPawnState.Normal:
                    {
                        float gainPoints = record.Pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationDailyGain);
                        record.MeditationPoints += gainPoints;

                        if (record.ResignationTick <= ticksGame)
                        {
                            if (RatkinOrderSettings.AutoPostponeResignationResidentKnight)
                            {
                                record.PostponeResignation(120);
                            }
                            else
                            {
                                record.SetForceState(ResidentPawnState.ReadyResignation);
                                toProcess.Add((record, ResidentPawnState.ReadyResignation));
                            }
                        }
                        continue;
                    }
                case ResidentPawnState.ReadyResignation or ResidentPawnState.ForceRemove or ResidentPawnState.PendingConvertToColonist:
                    {
                        toProcess.Add((record, knightState));
                        continue;
                    }
                default: continue;
            }
        }

        foreach ((ResidentKnight, ResidentPawnState) pair in toProcess)
        {
            if (pair.Item2 == ResidentPawnState.PendingConvertToColonist)
            {
                CoverKnightToColonist(pair.Item1);
                continue;
            }

            ResidentKnightRemovalReason removalReason = pair.Item2 switch
            {
                ResidentPawnState.ReadyResignation => ResidentKnightRemovalReason.Overdue,
                ResidentPawnState.ForceRemove => ResidentKnightRemovalReason.Invalid,
                _ => ResidentKnightRemovalReason.Unknown
            };

            DeregisterKnightDirectly(pair.Item1, removalReason);
        }

        MinResignationDays.MarkDirty();
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        List<ResidentKnight> toColonist = residentKnights.Values.Where(rk => rk.RatkinOrder == ratkinOrder).ToList();
        foreach (ResidentKnight knight in toColonist)
        {
            CoverKnightToColonist(knight);
        }
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        throw new NotImplementedException();
    }

    private void RegisterKnightDirectly(KnightRecord knightRecord)
    {
        residentKnights.Add(knightRecord.Pawn, new ResidentKnight(knightRecord));
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
            return;

        roleManager.OnResidentKnightDeregistered(record, reason);
        mentorshipManager.RemoveTeacher(record);

        record.PostRemoved(reason);
        OnKnightsChanged();
    }

    private void CoverKnightToColonist(ResidentKnight record)
    {
        if (record is null)
            return;

        DeregisterKnightDirectly(record, ResidentKnightRemovalReason.ConvertToColonist);
        if (record.Pawn is null || residentColonists.ContainsKey(record.Pawn))
            return;

        residentColonists.Add(record.Pawn, new ResidentPawn(record));
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
    }

    private List<Pawn> residentKnightKeys;
    private List<ResidentKnight> residentKnightValues;
    private List<Pawn> residentColonistKeys;
    private List<ResidentPawn> residentColonistValues;
}