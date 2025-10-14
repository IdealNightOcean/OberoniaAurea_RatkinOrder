using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Squad : IExposable, ITickHourOfDay
{
    protected const int StatUpdateHour = 5;

    [Unsaved] public readonly Branch Branch;

    protected string name;
    public string Name => name;

    protected SquadStat squadStat;
    public SquadStat SquadStat => squadStat;

    private Squad(Branch branch, bool initCtor)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        if (initCtor)
        {
            squadStat = new();
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
        Scribe_Deep.Look(ref squadStat, "squadStat");
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Squad(this));

    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == StatUpdateHour)
        {
            squadStat.UpdateCeiling(this, updateStatCache: true);
            DailyRecovery();
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchSquadOfType(Branch.BranchType type) => (Branch.CurType & type) == type;

    private void AnnualRetirement()
    {
        squadStat.MemberCount -= Mathf.CeilToInt(Rand.Range(0.05f, 0.1f) * squadStat.MemberCeiling);
    }

    private void DailyRecovery()
    {
        if (Branch.EffectTags.HasTag(KeyLibrary_EffectTag.BlockSquadRecover))
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
}