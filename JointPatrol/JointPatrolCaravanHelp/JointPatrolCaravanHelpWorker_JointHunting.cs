using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelpWorker_JointHunting : JointPatrolCaravanHelpWorker_SkillHelp_ThingReward
{
    protected override ThingDef GetRewardThingDef(FixedCaravan fixedCaravan, Branch branch)
    {
        return DefDatabase<ThingDef>.AllDefsListForReading.Where(t => t.IsMeat && FoodUtility.GetMeatSourceCategory(t) == MeatSourceCategory.Undefined).RandomElement();
    }

    protected override int GetRewardThingCount(FixedCaravan fixedCaravan, Branch branch, ThingDef rewardThingDef)
    {
        return OARO_PawnUtility.GetTotalSkillLevelOf(fixedCaravan.PawnsListForReading, SkillDefOf.Shooting) * 50;
    }
}