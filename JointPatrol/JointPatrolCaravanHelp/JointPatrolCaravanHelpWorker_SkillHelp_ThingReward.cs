using OberoniaAurea_Frame;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolCaravanHelpWorker_SkillHelp_ThingReward : JointPatrolCaravanHelpWorker_FixedCaravan_SkillHelp
{
    protected abstract ThingDef GetRewardThingDef(FixedCaravan fixedCaravan, Branch branch);
    protected abstract int GetRewardThingCount(FixedCaravan fixedCaravan, Branch branch, ThingDef rewardThingDef);

    public override void FinishWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite)
    {
        try
        {
            ThingDef rewardThingDef = GetRewardThingDef(fixedCaravan, branch);
            if (rewardThingDef is not null)
            {
                Thing rewardThing = ThingMaker.MakeThing(rewardThingDef);
                int rewardThingCount = Mathf.Max(1, GetRewardThingCount(fixedCaravan, branch, rewardThingDef));
                rewardThing.stackCount = rewardThingCount;
                OAFrame_FixedCaravanUtility.GiveThing(fixedCaravan, rewardThing);
                extraRewardText.AppendLine();
                extraRewardText.AppendLine("OAFrame_CarvanGetThing".Translate(rewardThingDef.Named(KeyLibrary_FormatArgName.THING), rewardThingCount.Named(KeyLibrary_FormatArgName.Count)));
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"give thing reward to {nameof(fixedCaravan)}",
                typeName: nameof(JointPatrolCaravanHelpWorker_SkillHelp_ThingReward),
                methodName: nameof(FinishWork),
                needStackTrace: true);
        }

        base.FinishWork(fixedCaravan, branch, incidentSite);
    }
}