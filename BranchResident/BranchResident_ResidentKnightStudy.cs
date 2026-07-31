using OberoniaAurea.RatkinOrder.Utility;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchResident_ResidentKnightStudy : BranchResident
{
    protected Dictionary<KnightChivalryDef, int> medalsCost = [];
    public IReadOnlyDictionary<KnightChivalryDef, int> MedalsCost => medalsCost;

    public override void StartResidency(Branch branch)
    {
        base.StartResidency(branch);
        OberoniaAurea_Frame.Utility.OAFrame_PawnUtility.MakePawnJoinPlayer(pawn);
    }

    public void MedalsCostAdd(KnightChivalryDef chivalry, int cost)
    {
        if (!medalsCost.TryGetValue(chivalry, out int count))
            count = 0;

        medalsCost[chivalry] = count + cost;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref medalsCost, nameof(medalsCost), LookMode.Def, LookMode.Value);
    }

    public static int GetDeployDays(Map map, Branch branch)
    {
        if (map is null || branch is null)
            return 10;

        float distance = branch.DistanceTo(map.Tile);
        return 5 + 5 + Mathf.RoundToInt(distance / 30f);
    }

    public static int RecommendationLetterCost(ResidentKnight knight, Branch targetBranch)
    {
        if (knight.Branch == targetBranch)
            return 0;

        if (targetBranch.IsBranchOfType(Branch.BranchType.Friendly))
            return 0;

        return targetBranch.RatkinOrder.Relationship switch
        {
            EsteemHandler.RelationshipKind.Soulmate => 1,
            EsteemHandler.RelationshipKind.Trustworthy => 2,
            EsteemHandler.RelationshipKind.Friendly => 3,
            _ => 0
        };
    }

    public static int MedalsCostLimit(ResidentKnight knight)
    {
        return knight.CurRank switch
        {
            ResidentKnightRank.Regular => 10,
            ResidentKnightRank.Elite => 15,
            ResidentKnightRank.Honor => 20,
            ResidentKnightRank.Crown => 30,
            _ => 10
        };
    }
}