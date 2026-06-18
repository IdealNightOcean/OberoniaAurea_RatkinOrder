using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻人员管理器 - 负责管理常驻骑士和常驻殖民者的记录、注册、注销和每日检测等功能
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
            ceiling += OrderStationHandler.Instance.OrderStationLevel switch
            {
                < 3 => 0,
                < 5 => 3,
                < 6 => 5,
                _ => 7
            };
            return ceiling;
        }
    }

    private List<ResidentKnight> residentKnights = [];
    private Dictionary<Pawn, ResidentKnight> residentKnightsDict = [];
    public int KnightsCount => residentKnights.Count;
    public IReadOnlyList<ResidentKnight> ResidentKnights => residentKnights;

    private List<ResidentPawn> residentColonists = [];
    public IReadOnlyList<ResidentPawn> ResidentColonists => residentColonists;
    private Dictionary<Pawn, ResidentPawn> residentColonistsDict = [];

    private ResidentRoleManager roleManager;
    public static ResidentRoleManager RoleManager => Instance?.roleManager;

    private MentorshipManager mentorshipManager;
    public static MentorshipManager MentorshipManager => Instance?.mentorshipManager;

    private ResidentPawnsCacheManager cacheManager;
    public static ResidentPawnsCacheManager CacheManager => Instance?.cacheManager;

    internal ResidentPawnsManager(bool initCtor)
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(AcceptedBranchDemandHandler));
        Instance = this;
        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();

        if (initCtor)
        {
            EnsureComponentsInit();
        }

        cacheManager ??= new(this);
    }

    public static void ClearStaticCache() => Instance = null;

    private void EnsureComponentsInit()
    {
        roleManager ??= new ResidentRoleManager(this);
        mentorshipManager ??= new MentorshipManager(this);

        cacheManager ??= new(this);
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref residentColonists, nameof(residentColonists), LookMode.Deep);
        Scribe_Collections.Look(ref residentKnights, nameof(residentKnights), LookMode.Deep);

        Scribe_Deep.Look(ref roleManager, nameof(roleManager), ctorArgs: this);
        Scribe_Deep.Look(ref mentorshipManager, nameof(mentorshipManager), ctorArgs: this);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            EnsureComponentsInit();
            if (residentColonists.RemoveAll(r => r is null || r.CurState == ResidentPawnState.ForceRemove) > 0)
                Log.Error($"[OARO] {nameof(ResidentPawnsManager)} 的部分常驻殖民者记录在加载后为null或无效，已被移除。");

            if (residentKnights.RemoveAll(r => r is null || r.CurState == ResidentPawnState.ForceRemove) > 0)
                Log.Error($"[OARO] {nameof(ResidentPawnsManager)} 的部分常驻骑士记录在加载后为null或无效，已被移除。");

            residentColonistsDict = new(residentColonists.Count);
            foreach (ResidentPawn r in residentColonists)
                residentColonistsDict.Add(r.Pawn, r);

            residentKnightsDict = new(residentKnights.Count);
            foreach (ResidentKnight r in residentKnights)
                residentKnightsDict.Add(r.Pawn, r);
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
            foreach (ResidentKnight residentKnight in residentKnights)
            {
                listing_Rect.SubLabel(residentKnight.Pawn.Name + ": " + residentKnight.ToString(), widthPct: 0.8f);
            }
        }

        listing_Rect.Gap(12f);
        roleManager.DrawDevWindow(listing_Rect);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentKnight(Pawn pawn) => residentKnightsDict.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out ResidentKnight record) => residentKnightsDict.TryGetValue(pawn, out record);

    public bool TryRegisterKnight(Pawn pawn, KnightRecord knightRecord = null)
    {
        if (knightRecord is null && !KnightPawnsManager.Instance.TryGetKnightRecord(pawn, out knightRecord))
        {
            Log.Error($"[OARO] 尝试将非骑士单位添加到 {nameof(ResidentPawnsManager)}");
            return false;
        }
        if (knightRecord.Pawn != pawn)
        {
            Log.Error($"[OARO] 骑士记录的 Pawn ({knightRecord.Pawn}) 与尝试注册的 Pawn ({pawn}) 不匹配，无法添加到 {nameof(ResidentPawnsManager)}");
            return false;
        }

        if (IsResidentColonist(pawn))
        {
            Log.Error($"[OARO] 尝试将常驻殖民者同时注册为常驻骑士: {pawn}");
            return false;
        }

        if (IsResidentKnight(pawn))
        {
            Log.Error($"[OARO] 尝试重复注册常驻骑士: {pawn}");
            return false;
        }

        RegisterKnightDirectly(knightRecord);
        return true;
    }

    public bool DeregisterKnight(Pawn pawn, ResidentKnightRemovalReason reason)
    {
        if (pawn is null || !residentKnightsDict.TryGetValue(pawn, out ResidentKnight record))
            return false;

        return DeregisterKnightDirectly(record, reason);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsResidentColonist(Pawn pawn) => residentColonistsDict.ContainsKey(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetColonistRecord(Pawn pawn, out ResidentPawn record) => residentColonistsDict.TryGetValue(pawn, out record);

    public bool TryRegisterColonist(Pawn pawn)
    {
        if (pawn is null)
            return false;

        if (IsResidentColonist(pawn))
        {
            Log.Error($"[OARO] 尝试重复注册常驻殖民者: {pawn}");
            return false;
        }

        if (IsResidentKnight(pawn))
        {
            Log.Error($"[OARO] 尝试将常驻殖民者同时注册为常驻骑士: {pawn}");
            return false;
        }

        ResidentPawn residentPawn = new(pawn);
        residentColonistsDict.Add(pawn, residentPawn);
        residentColonists.Add(residentPawn);
        return true;
    }

    public bool DeregisterColonist(Pawn pawn)
    {
        if (pawn is null || !TryGetColonistRecord(pawn, out ResidentPawn residentPawn))
            return false;

        return DeregisterColonistDirectly(residentPawn);
    }

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, 2500))
        {
            bool dailyCheck = TickUtility.IsHashIntervalTick(tickHashOffset, 60000);
            if (dailyCheck)
            {
                DailyKnightsCheck();
                DailyColonistsCheck();
            }

            cacheManager.TickHour();

            if (dailyCheck)
            {
                DailyKnightsVirtueCheck();
                mentorshipManager.TickDay();
            }
        }
    }

    public void AllKnightsGainMeditation(float gain, RatkinOrder ratkinOrder = null, bool directly = false)
    {
        foreach (ResidentKnight record in residentKnights)
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
        foreach (ResidentKnight residentKnight in residentKnights)
        {
            if (residentKnight.RatkinOrder != ratkinOrder || residentKnight.Pawn.needs is null || residentKnight.Pawn.needs.mood is null)
            {
                continue;
            }
            int forceStage = residentKnight.Branch == branch ? 1 : 0;
            Thought_Memory memory = ThoughtMaker.MakeThought(OARO_ThoughtDefOf.OARO_Thought_ResidentKnight_SquadBeAttackedOnTask, forceStage);
            residentKnight.Pawn.needs.mood.thoughts.memories.TryGainMemory(memory);
        }
    }

    /// <summary>
    /// 每日检测 - 移除无效常驻殖民者
    /// </summary>
    private void DailyColonistsCheck()
    {
        List<ResidentPawn> toRemove = [];
        foreach (ResidentPawn residentPawn in residentColonists)
        {
            residentPawn.CheckPendingRemoval();
            if (residentPawn.CurState == ResidentPawnState.ForceRemove)
            {
                toRemove.Add(residentPawn);
            }
        }
        foreach (ResidentPawn residentPawn in toRemove)
        {
            DeregisterColonistDirectly(residentPawn);
        }
    }

    /// <summary>
    /// 每日检测 - 移除无效常驻骑士（离职、离开、转化为常驻殖民者）
    /// </summary>
    private void DailyKnightsCheck()
    {
        List<(ResidentKnight, ResidentPawnState)> toProcess = [];
        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnight record in residentKnights)
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
    }

    private void DailyKnightsVirtueCheck()
    {
        foreach (ResidentKnight residentKnight in residentKnights)
        {
            if (!Rand.Chance(0.005f))
                continue;

            KnightVirtueDef virtueDef = KnightVirtueUtility.GetRandomAvailableVirtue(residentKnight);
            if (virtueDef is null)
                continue;

            int newVirtueLevel = KnightVirtueUtility.GetRandomNewVirtueLevel_Daily(residentKnight);
            residentKnight.VirtueHandler.TryAddVirtue(virtueDef: virtueDef,
                                                            level: newVirtueLevel,
                                                            reason: "OARO_KnightVirtueGainReason_Daily".Translate());
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        List<ResidentKnight> toColonist = residentKnights.Where(r => r.RatkinOrder == ratkinOrder).ToList();
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
        ResidentKnight residentKnight = new(knightRecord);
        residentKnightsDict.Add(knightRecord.Pawn, residentKnight);
        residentKnights.Add(residentKnight);
        residentKnight.PostAdded();
        OnKnightsChanged();
    }

    private bool DeregisterColonistDirectly(ResidentPawn residentPawn)
    {
        if (!residentColonistsDict.Remove(residentPawn.Pawn))
        {
            return false;
        }
        residentColonists.Remove(residentPawn);
        mentorshipManager.RemoveStudent(residentPawn);
        return true;
    }

    private bool DeregisterKnightDirectly(ResidentKnight residentKnight, ResidentKnightRemovalReason reason)
    {
        if (!residentKnightsDict.Remove(residentKnight.Pawn))
            return false;

        residentKnights.Remove(residentKnight);
        roleManager.OnResidentKnightDeregistered(residentKnight, reason);
        mentorshipManager.RemoveTeacher(residentKnight);
        mentorshipManager.RemoveStudent(residentKnight);

        residentKnight.PostRemoved(reason);
        OnKnightsChanged();
        return true;
    }

    private bool CoverKnightToColonist(ResidentKnight residentKnight)
    {
        if (residentKnight is null)
            return false;

        if (IsResidentColonist(residentKnight.Pawn))
            return false;

        if (!DeregisterKnightDirectly(residentKnight, ResidentKnightRemovalReason.ConvertToColonist))
            return false;

        ResidentPawn residentPawn = new(residentKnight);
        residentColonistsDict.Add(residentPawn.Pawn, residentPawn);
        residentColonists.Add(residentPawn);
        return true;
    }

    private void RemoveAllInvalidRecord(Predicate<ResidentKnight> extraRemove = null, ResidentKnightRemovalReason extraRemoveReason = ResidentKnightRemovalReason.Unknown)
    {
        List<ResidentKnight> recordsToRemove = [];
        List<ResidentKnight> recordsToExtraRemove = extraRemove is null ? null : [];
        foreach (ResidentKnight record in residentKnights)
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

    private void OnKnightsChanged()
    {
        cacheManager.OnKnightsChanged();
    }
}