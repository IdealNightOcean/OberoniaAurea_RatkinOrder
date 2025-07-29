using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_ExitMapBestForDeployment : LordJob_ExitMapBest
{
    private Branch targetBranch;
    private int totalDeployDays = 1;
    private SkillDef targetSkill;

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap)
        {
            if (targetBranch is null || targetSkill is null)
            {
                return;
            }

            targetBranch.ResidentHandler.AddResident(p, totalDeployDays, new ResidencyWorker_Deployment
            {
                skill = targetSkill
            });
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref targetBranch, "targetBranch");
        Scribe_Values.Look(ref totalDeployDays, "totalDeployDays", 1);
        Scribe_Defs.Look(ref targetSkill, "targetSkill");
    }
}
