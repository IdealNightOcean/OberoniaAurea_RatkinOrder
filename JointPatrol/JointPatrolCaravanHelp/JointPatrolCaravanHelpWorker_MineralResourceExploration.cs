using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpWorker_MineralResourceExploration : JointPatrolCaravanHelpWorker_SkillHelp_ThingReward
{
    protected override ThingDef GetRewardThingDef(FixedCaravan fixedCaravan, Branch branch)
    {
        return DefDatabase<ThingDef>.AllDefsListForReading.Where(t => t.IsMetal).RandomElement();
    }

    protected override int GetRewardThingCount(FixedCaravan fixedCaravan, Branch branch, ThingDef rewardThingDef)
    {
        int totalLevel = OARO_PawnUtility.GetTotalSkillLevelOf(fixedCaravan.PawnsListForReading, SkillDefOf.Mining);
        float count = totalLevel * 40 * ThingDefOf.Gold.GetStatValueAbstract(StatDefOf.MarketValue) / rewardThingDef.GetStatValueAbstract(StatDefOf.MarketValue);
        return Mathf.RoundToInt(count);
    }
}