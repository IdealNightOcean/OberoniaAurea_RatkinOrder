using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadStat : IExposable, IDrawDevWindow
{
    private float memberCount; //分队成员数量
    private float commanderCount; //分队骑士长数量
    private float supply; //分队补给数量

    public float MemberCeiling = 1f;
    public float CommanderCeiling = 1f;
    public float SupplyCeiling = 1f;

    public void ExposeData()
    {
        Scribe_Values.Look(ref memberCount, "memberCount", 0f);
        Scribe_Values.Look(ref commanderCount, "commanderCount", 0f);
        Scribe_Values.Look(ref supply, "supply", 0f);

        Scribe_Values.Look(ref MemberCeiling, "MemberCeiling", 0f);
        Scribe_Values.Look(ref CommanderCeiling, "CommanderCeiling", 0f);
        Scribe_Values.Look(ref SupplyCeiling, "SupplyCeiling", 0f);
    }

    public float MemberCount
    {
        get => memberCount;
        set => memberCount = Mathf.Clamp(value, 0f, MemberCeiling);
    }
    public int MemberCountInt => Mathf.FloorToInt(memberCount); //分队成员数量（整数）

    public float CommanderCount
    {
        get => commanderCount;
        set => commanderCount = Mathf.Clamp(value, 0f, CommanderCeiling);
    }
    public int CommanderCountInt => Mathf.FloorToInt(commanderCount); //分队骑士长数量（整数）

    public float AllCrewCount => memberCount + commanderCount;
    public int AllCrewCountInt => Mathf.FloorToInt(memberCount + commanderCount);

    public float Supply
    {
        get => supply;
        set => supply = Mathf.Clamp(value, 0f, SupplyCeiling);
    }

    public float MemberPercentage
    {
        get
        {
            if (MemberCeiling <= 0f)
            {
                return 1f;
            }
            return memberCount / MemberCeiling;
        }
    }

    public float CommanderPercentage
    {
        get
        {
            if (CommanderCeiling <= 0f)
            {
                return 1f;
            }
            return commanderCount / CommanderCeiling;
        }
    }

    public SquadStat() { }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"MemberCount: {memberCount:F2}");
        listing_Rect.Label($"CommanderCount: {commanderCount:F2}");
        listing_Rect.Label($"Supply: {Supply:F2}");
        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("Member +1", widthPct: 0.6f))
        {
            MemberCount += 1f;
        }
        if (listing_Rect.ButtonText("Commander +1", widthPct: 0.6f))
        {
            CommanderCount += 1f;
        }
        if (listing_Rect.ButtonText("Supply +10%", widthPct: 0.6f))
        {
            Supply += 0.1f;
        }
        listing_Rect.Gap(6f);
        listing_Rect.Label($"MemberCeiling: {MemberCeiling:F2}");
        listing_Rect.Label($"CommanderCeiling: {CommanderCeiling:F2}");
        listing_Rect.Label($"SupplyCeiling: {SupplyCeiling:F2}");
    }

    public void UpdateCeiling(Squad squad, bool updateStatCache)
    {
        Branch branch = squad.Branch;
        MemberCeiling = branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberCeiling, immediateUpdate: updateStatCache);
        CommanderCeiling = branch.GetStatValue(BranchStatDefOf.OARO_SquadCommanderCeiling, immediateUpdate: updateStatCache);
        SupplyCeiling = branch.GetStatValue(BranchStatDefOf.OARO_SquadSupplyCeiling, immediateUpdate: updateStatCache);
    }
}
