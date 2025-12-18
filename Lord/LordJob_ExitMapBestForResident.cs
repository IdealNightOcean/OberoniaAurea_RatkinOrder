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
            p.DeSpawnOrDeselect();
            if (!targetBranch.IsValid() || targetSkill is null)
            {
                return;
            }

            BranchResident_Deployment resident = (BranchResident_Deployment)BranchResident.GenerateBranchResident(BranchResidentDefOf.OARO_Deployment, p, totalDeployDays);
            targetBranch.ResidentHandler.AddResident(resident);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref targetBranch, nameof(targetBranch));
        Scribe_Values.Look(ref totalDeployDays, nameof(totalDeployDays), 1);
        Scribe_Defs.Look(ref targetSkill, nameof(targetSkill));
    }
}