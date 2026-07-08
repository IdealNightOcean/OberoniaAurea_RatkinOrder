using OberoniaAurea_Frame;
using System;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部小队
/// </summary>
public class BranchSquad : IExposable, ITickHourOfDay
{
    protected const int StatUpdateHour = 5;

    [Unsaved] private readonly Branch branch;

    protected string name;
    public string Name => name;

    public float MemberCeiling => memberCeilingCache.GetCachedResult();
    public float CommanderCeiling => commanderCeilingCache.GetCachedResult();

    public float MemberCount => memberCount;

    public int MemberCountInt => Mathf.FloorToInt(memberCount); //分队成员数量（整数）
    public float MemberPercentage => memberCount / MemberCeiling;

    public float CommanderCount => commanderCount;
    public int CommanderCountInt => Mathf.FloorToInt(commanderCount); //分队骑士长数量（整数）
    public float CommanderPercentage => commanderCount / CommanderCeiling;

    public float AllCrewCount => memberCount + commanderCount;
    public int AllCrewCountInt => Mathf.FloorToInt(memberCount + commanderCount);

    [Unsaved] private SimpleValueCache<float> memberCeilingCache;
    [Unsaved] private SimpleValueCache<float> commanderCeilingCache;

    private float memberCount; //分队成员数量
    private float commanderCount; //分队骑士长数量

    internal BranchSquad(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        memberCeilingCache = new(cacheInterval: 60000, defaultValue: BranchStatDefOf.OARO_SquadMemberCeiling.baseValue, () => branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberCeiling));
        commanderCeilingCache = new(cacheInterval: 60000, defaultValue: BranchStatDefOf.OARO_SquadCommanderCeiling.baseValue, () => branch.GetStatValue(BranchStatDefOf.OARO_SquadCommanderCeiling));
    }

    internal void Rename(int ordinal, string nameCore)
    {
        int unitsDigit = ordinal % 10;
        GrammarRequest grammarRequest = new()
        {
            Includes = { OARO_RulePackDefOf.OARO_NameBuilder_SquadName }
        };
        grammarRequest.Constants.Add("unitsDigit", unitsDigit.ToString());
        grammarRequest.Rules.Add(new Rule_String("ordinal", ordinal.ToString()));
        grammarRequest.Rules.Add(new Rule_String("nameCore", nameCore));
        name = GrammarResolver.Resolve("r_name", grammarRequest);
    }

    public void PostBranchGenerated()
    {
        memberCount = MemberCeiling;
        commanderCount = CommanderCeiling;
        branch.BranchManager.TotalKnightsCount.MarkDirty();
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref name, nameof(name));

        Scribe_Values.Look(ref memberCount, nameof(memberCount), 0f);
        Scribe_Values.Look(ref commanderCount, nameof(commanderCount), 0f);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Gap(6f);
        listing_Rect.Label($"名称: {name}");
        if (listing_Rect.ButtonTextLabeled($"普通骑士: {MemberCountInt}", "Member +1"))
        {
            AdjustCrew(member: 1f, commander: 0f);
        }
        if (listing_Rect.ButtonTextLabeled($"骑士长: {CommanderCountInt}", "Commander +1"))
        {
            AdjustCrew(member: 0f, commander: 1f);
        }
        listing_Rect.Gap(6f);
        listing_Rect.Label($"普通骑士上限: {MemberCeiling:F2}");
        listing_Rect.Label($"骑士长上限: {CommanderCeiling:F2}");
    }

    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == StatUpdateHour)
        {
            DailyRecovery();
        }
    }

    public void AdjustCrew(float member, float commander)
    {
        if (member != 0f)
        {
            memberCount = Mathf.Clamp(memberCount + member, 0f, MemberCeiling);
            branch.BranchManager.TotalKnightsCount.MarkDirty();
        }
        if (commander != 0f)
        {
            commanderCount = Mathf.Clamp(commanderCount + commander, 0f, CommanderCeiling);
            branch.BranchManager.TotalKnightsCount.MarkDirty();
        }
    }

    private void AnnualRetirement()
    {
        AdjustCrew(member: -Rand.Range(0.05f, 0.1f) * MemberCeiling, commander: 0f);
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
                AdjustCrew(member: 0f, commander: branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate));
            }
        }
        else if (memberCount < MemberCeiling)
        {
            AdjustCrew(member: branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate), commander: 0f);
        }
    }
}