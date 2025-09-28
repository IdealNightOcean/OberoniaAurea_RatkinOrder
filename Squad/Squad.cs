using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
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
    public CooldownRecordManager CooldownManager => Branch.CooldownManager;

    protected string name;
    public string Name => name;

    public BranchType SquadType => Branch.BranchType;

    protected string stateStr = string.Empty;
    protected string TaskState => taskHandler.TaskState;

    public string SquadState => TaskState ?? stateStr; //如果有固定状态，则使用固定状态，否则使用当前状态

    public bool BlockSupport => taskHandler.BlockSupport; //是否阻止恢复
    public bool BlockRecover => taskHandler.BlockRecover; //是否阻止恢复

    protected SquadStat squadStat;
    protected SquadTaskHandler taskHandler;
    protected SquadSupportHandler supportHandler;

    public SquadStat SquadStat => squadStat;
    public SquadTaskHandler TaskHandler => taskHandler;
    public SquadSupportHandler SupportHandler => supportHandler;

    private Squad(Branch branch)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void PostBranchGenerated()
    {
        name = "OARO_BranchSquadName".Translate(Branch.Name.Named("branchName"));
        EnsureComponentsInit();
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
            squad = new(branch);
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
        Scribe_Deep.Look(ref supportHandler, "supportHandler", ctorArgs: this);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Squad(this));

    public void TickHour(int hourOfDay)
    {
        if (!CooldownManager.IsInCooldown(KeyLibrary_CDRecord.SquadStateDesc))
        {
            CooldownManager.RegisterRecord(KeyLibrary_CDRecord.SquadStateDesc, cdTicks: 9 * 2500);
            UpdateStateDesc(hourOfDay);
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
    public bool IsBranchSquadOfType(BranchType type) => (SquadType & type) == type;

    public void PostSquadCombatPawnGenerate(IEnumerable<Pawn> members, IEnumerable<Pawn> commanders, bool friendly)
    {
        if (members is null) { return; }
        IReadOnlyList<(HediffDef, float)> medalHediffs = Branch.MedalHandler.MedalHediffs;
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
        squadStat.MemberCount -= Mathf.CeilToInt(Rand.Range(0.05f, 0.1f) * squadStat.MemberCeiling);
    }

    private void TryRecovery()
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
        supportHandler ??= new SquadSupportHandler(this);
    }
}
