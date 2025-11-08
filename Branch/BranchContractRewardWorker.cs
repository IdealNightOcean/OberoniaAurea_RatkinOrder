using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchContractRewardWorker
{
    public virtual void Reward(BranchContract contract, Caravan caravan, Branch branch)
    {
        float rewardMarkerValue = contract.RequestThingDef.GetStatValueAbstract(StatDefOf.MarketValue) * contract.RequestCount * 1.2f;
        int rewardMarkerValueInt = Mathf.Max(1, Mathf.RoundToInt(rewardMarkerValue));

        Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
        silver.stackCount = rewardMarkerValueInt;
        CaravanInventoryUtility.GiveThing(caravan, silver);

        branch.SetFriendly(friendly: true);
    }
}