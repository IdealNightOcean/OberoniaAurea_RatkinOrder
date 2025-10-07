using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Squad : IExposable, IPostLoadInit, ITickHourOfDay, ITickDay
{
    protected const int StatUpdateHour = 5;

    [Unsaved] public readonly Branch Branch;

    public RatkinOrder RatkinOrder => Branch.RatkinOrder;
    public SquadManager SquadManager => RatkinOrder.SquadManager;

    protected string name;
    public string Name => name;

    protected string stateStr = string.Empty;
    protected string TaskState => taskHandler.TaskState;

    public string SquadState => TaskState ?? stateStr; //如果有固定状态，则使用固定状态，否则使用当前状态

    public bool BlockSupport => taskHandler.BlockSupport; //是否阻止恢复
    public bool BlockRecover => taskHandler.BlockRecover; //是否阻止恢复

    protected SquadStat squadStat;
    protected SquadTaskHandler taskHandler;

    public SquadStat SquadStat => squadStat;
    public SquadTaskHandler TaskHandler => taskHandler;

    private Squad(Branch branch, bool initCtor)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        if (initCtor)
        {
            EnsureComponentsInit();
        }
    }

    public void PostBranchGenerated()
    {
        squadStat.UpdateCeiling(this, updateStatCache: true);
        squadStat.MemberCount = squadStat.MemberCeiling;
        squadStat.CommanderCount = squadStat.CommanderCeiling;
        squadStat.Supply = 0.5f;
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
            squad = new(branch, initCtor: true)
            {
                name = "OARO_BranchSquadName".Translate(branch.Name.Named("branchName"))
            };
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create BranchSquad for branch {branch}. Returning null: " + ex);
            return null;
        }
        return squad;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref name, "name");
        Scribe_Values.Look(ref stateStr, "stateStr");

        Scribe_Deep.Look(ref squadStat, "squadStat");
        Scribe_Deep.Look(ref taskHandler, "taskHandler", ctorArgs: this);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Squad(this));

    public void TickHour(int hourOfDay)
    {
        if (!Branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.SquadStateDesc))
        {
            Branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.SquadStateDesc, cdTicks: 9 * 2500);
            UpdateStateDesc(hourOfDay);
        }

        if (hourOfDay == StatUpdateHour)
        {
            squadStat.UpdateCeiling(this, updateStatCache: true);
            DailyRecovery();
        }
    }
    public void TickDay()
    {
        taskHandler.TickDay();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchSquadOfType(Branch.BranchType type) => (Branch.CurType & type) == type;

    private void AnnualRetirement()
    {
        squadStat.MemberCount -= Mathf.CeilToInt(Rand.Range(0.05f, 0.1f) * squadStat.MemberCeiling);
    }

    private void DailyRecovery()
    {
        if (taskHandler.BlockRecover)
        {
            return;
        }

        if (Rand.Chance(0.1f))
        {
            if (squadStat.CommanderCount < squadStat.CommanderCeiling)
            {
                squadStat.CommanderCount += BranchStatUtility.GetStatValue(Branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);
            }
        }
        else if (squadStat.MemberCount < squadStat.MemberCeiling)
        {
            squadStat.MemberCount += BranchStatUtility.GetStatValue(Branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        }

        if (squadStat.Supply < squadStat.SupplyCeiling)
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
            stateStr = "OARO_SquadState_Rest".Translate();
            return;
        }

        stateStr = "OARO_SquadState_Idle".Translate();
    }

    public void PostLoadInit()
    {
        EnsureComponentsInit();
    }

    private void EnsureComponentsInit()
    {
        squadStat ??= new SquadStat();
        taskHandler ??= new SquadTaskHandler(this);
    }
}