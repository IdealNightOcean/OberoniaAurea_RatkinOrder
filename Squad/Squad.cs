using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Squad : IExposable, ILoadReferenceable, IPostLoadInit, ITickHourOfDay, ITickDay
{
    protected const int StateDescUpdateInterval = 9;
    protected const int StatUpdateHour = 5;

    [Unsaved] public readonly Branch Branch;
    [Unsaved] public readonly RatkinOrder RatkinOrder;
    public SquadManager SquadManager => RatkinOrder.SquadManager;

    protected int loadID = -1;
    protected string name;
    public string Name => name;

    public BranchType SquadType => Branch.BranchType;

    protected string stateStr = string.Empty;
    protected string TaskState => taskHandler.TaskState;
    protected int lastStateHours;

    public string SquadState => TaskState ?? stateStr; //如果有固定状态，则使用固定状态，否则使用当前状态

    public bool BlockSupport => taskHandler.BlockSupport; //是否阻止恢复
    public bool BlockRecover => taskHandler.BlockRecover; //是否阻止恢复

    protected SquadStat squadStat;
    protected SquadTaskHandler taskHandler;
    protected SquadSupportHandler supportHandler;

    public SquadStat SquadStat => squadStat;
    public SquadTaskHandler TaskHandler => taskHandler;
    public SquadSupportHandler SupportHandler => supportHandler;

    private Squad(Branch branch, bool initConstruct)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        RatkinOrder = branch.RatkinOrder ?? throw new NullReferenceException(nameof(RatkinOrder));

        if (initConstruct)
        {
            EnsureComponentsInit();
            loadID = UniqueIDManager.Instance.GetUniqueID("Squad");
        }
    }

    public static Squad GenerateSquadForBranch(Branch branch)
    {
        if (branch is null)
        {
            return null;
        }
        if (branch.Squad is not null)
        {
            Log.Error($"Branch {branch} already has a squad assigned. Cannot generate a new one. Returning existing squad instead.");
            return branch.Squad;
        }

        Squad squad;

        try
        {
            squad = new(branch, initConstruct: true);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create BranchSquad for branch {branch}. Returning null: " + ex);
            return null;
        }

        return squad;
    }

    public void TickHour(int hourOfDay)
    {
        if ((lastStateHours - hourOfDay + 24) % 24 == StateDescUpdateInterval)
        {
            UpdateStateDesc(hourOfDay);
            lastStateHours = hourOfDay;
        }

        if (hourOfDay == StatUpdateHour)
        {
            squadStat.UpdateCeiling(this, updateStatCache: true);
            TryRecovery();
        }
    }
    public void TickDay()
    {
        taskHandler.TickDay();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSquadOfType(BranchType type)
    {
        return (SquadType & type) == type;
    }

    public void PostSquadCombatPawnGenerate(IEnumerable<Pawn> members, IEnumerable<Pawn> commanders, bool friendly)
    {
        if (members is null) { return; }
        List<(HediffDef, float)> medalHediffs = SquadUtility.GetSquadMedalHediffsToApply(this);
        bool hasMedalHediffs = medalHediffs is not null;

        SimpleUniqueList<IPostSquadCombatPawnGenerate> postSquadCombat = Branch.PostSquadCombatPawnGenerate;
        bool hasPostSquadCombat = postSquadCombat is not null;

        foreach (Pawn member in members)
        {
            if (hasMedalHediffs)
            {
                SquadUtility.ApplySquadMedalHediffs(member, medalHediffs);
            }
            if (hasPostSquadCombat)
            {
                for (int i = 0; i < postSquadCombat.Count; i++)
                {
                    try
                    {
                        postSquadCombat[i].PostSquadCombatPawnGenerate(member, this, isCommander: false, friendly);
                    }
                    catch (Exception ex)
                    {
                        string processorTypeName = postSquadCombat[i]?.GetType()?.FullName ?? "UnknownProcessor";
                        Log.Error($"Exception occurred while executing post-squad assist processor: ProcessorType={processorTypeName}, ErrorMessage: {ex.Message}");
                        continue;
                    }
                }
            }
        }

        if (commanders is null) { return; }

        foreach (Pawn commander in commanders)
        {
            if (hasMedalHediffs)
            {
                SquadUtility.ApplySquadMedalHediffs(commander, medalHediffs);
            }
            if (hasPostSquadCombat)
            {
                for (int i = 0; i < postSquadCombat.Count; i++)
                {
                    try
                    {
                        postSquadCombat[i].PostSquadCombatPawnGenerate(commander, this, isCommander: true, friendly);
                    }
                    catch (Exception ex)
                    {
                        string processorTypeName = postSquadCombat[i]?.GetType()?.FullName ?? "UnknownProcessor";
                        Log.Error($"Exception occurred while executing post-squad assist processor: ProcessorType={processorTypeName}, ErrorMessage: {ex.Message}");
                        continue;
                    }
                }
            }
        }
    }

    private void AnnualRetirement()
    {
        squadStat.MemberCount -= Mathf.CeilToInt(Rand.Range(0.05f, 0.1f) * squadStat.memberCeiling);
    }

    private void TryRecovery()
    {
        if (taskHandler.BlockRecover)
        {
            return;
        }

        if (Rand.Chance(0.1f))
        {
            if (squadStat.CommanderCount < squadStat.commanderCeiling)
            {
                squadStat.CommanderCount += BranchStatUtility.GetStatValue(Branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);
            }
        }
        else if (squadStat.MemberCount < squadStat.memberCeiling)
        {
            squadStat.MemberCount += BranchStatUtility.GetStatValue(Branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        }

        if (squadStat.Supply < squadStat.supplyCeiling)
        {
            squadStat.Supply += BranchStatUtility.GetStatValue(Branch, BranchStatDefOf.OARO_SquadSupplyRecoveryRate);
        }
    }

    private void UpdateStateDesc(int hourOfDay)
    {
        if (TaskState is not null)
        {
            return;
        }
        if (hourOfDay <= 5 || hourOfDay >= 21)
        {
            stateStr = "OARO_SquadStateRest".Translate();
            return;
        }

        stateStr = "OARO_SquadStateIdle".Translate();
    }

    public void PostLoadInit()
    {
        EnsureComponentsInit();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureComponentsInit()
    {
        squadStat ??= new SquadStat(initConstruct: true);
        taskHandler ??= new SquadTaskHandler(this);
        supportHandler ??= new SquadSupportHandler(this);
    }

    public string GetUniqueLoadID()
    {
        return "Squad_" + loadID;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);

        Scribe_Values.Look(ref stateStr, "stateStr");
        Scribe_Values.Look(ref lastStateHours, "lastStateHours", 0);

        Scribe_Deep.Look(ref squadStat, "squadStat", ctorArgs: false);
        Scribe_Deep.Look(ref taskHandler, "taskHandler", ctorArgs: this);
        Scribe_Deep.Look(ref supportHandler, "supportHandler", ctorArgs: this);
    }
}
