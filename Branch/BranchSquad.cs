using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchSquad : IExposable, ITickHourOfDay
{
    protected const int StatUpdateHour = 5;

    [Unsaved] private readonly Branch branch;

    protected string name;
    public string Name => name;

    public float MemberCeiling => memberCeilingCache.GetCachedResult();
    public float CommanderCeiling => commanderCeilingCache.GetCachedResult();

    public float MemberCount
    {
        get => memberCount;
        set => memberCount = Mathf.Clamp(value, 0f, MemberCeiling);
    }
    public int MemberCountInt => Mathf.FloorToInt(memberCount); //分队成员数量（整数）
    public float MemberPercentage => memberCount / MemberCeiling;

    public float CommanderCount
    {
        get => commanderCount;
        set => commanderCount = Mathf.Clamp(value, 0f, CommanderCeiling);
    }
    public int CommanderCountInt => Mathf.FloorToInt(commanderCount); //分队骑士长数量（整数）
    public float CommanderPercentage => commanderCount / CommanderCeiling;

    public float AllCrewCount => memberCount + commanderCount;
    public int AllCrewCountInt => Mathf.FloorToInt(memberCount + commanderCount);

    [Unsaved] private readonly SimpleValueCache<float> memberCeilingCache;
    [Unsaved] private readonly SimpleValueCache<float> commanderCeilingCache;

    private float memberCount; //分队成员数量
    private float commanderCount; //分队骑士长数量

    private BranchSquad(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        memberCeilingCache = new(cacheInterval: 60000, defaultValue: BranchStatDefOf.OARO_SquadMemberCeiling.baseValue, () => branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberCeiling));
        commanderCeilingCache = new(cacheInterval: 60000, defaultValue: BranchStatDefOf.OARO_SquadCommanderCeiling.baseValue, () => branch.GetStatValue(BranchStatDefOf.OARO_SquadCommanderCeiling));
    }

    public void PostBranchGenerated()
    {
        memberCount = MemberCeiling;
        commanderCount = CommanderCeiling;
    }

    public static BranchSquad GenerateSquadForBranch(Branch branch)
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

        BranchSquad squad;

        try
        {
            squad = new(branch)
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

        Scribe_Values.Look(ref memberCount, "memberCount", 0f);
        Scribe_Values.Look(ref commanderCount, "commanderCount", 0f);
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_Squad(branch));

    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == StatUpdateHour)
        {
            DailyRecovery();
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBranchSquadOfType(Branch.BranchType type) => (branch.CurType & type) == type;

    private void AnnualRetirement()
    {
        MemberCount -= Mathf.CeilToInt(Rand.Range(0.05f, 0.1f) * MemberCeiling);
    }

    private void DailyRecovery()
    {
        if (branch.EffectTags.HasTag(KeyLibrary_EffectTag.BlockSquadRecover))
        {
            return;
        }

        if (Rand.Chance(0.1f))
        {
            if (commanderCount < CommanderCeiling)
            {
                CommanderCount += BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);
            }
        }
        else if (memberCount < MemberCeiling)
        {
            MemberCount += BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        }

        /*
        if (squadStat.Supply < squadStat.SupplyCeiling)
        {
            squadStat.Supply += BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_SupplyRecoveryRate);
        }
        */
    }
}