using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_RequestCombatReadiness(BranchInteractionDef def) : BranchInteractionWorker_Targetless(def)
{
    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        AcceptanceReport acceptance = parms.Branch.TaskHandler.CanStartTask(BranchTaskDefOf.OARO_CombatReadiness, resultOnly: false);
        if (!acceptance)
        {
            return acceptance;
        }
        if (!parms.Branch.HasSupportAuthority)
        {
            return resultOnly ? false : "OARO_NoSupportAuthority".Translate();
        }

        return base.BranchValidate(parms, resultOnly);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        bool succeeded = parms.Branch.TaskHandler.TryStartTask(BranchTaskDefOf.OARO_CombatReadiness);
        if (succeeded && parms.Branch.TaskHandler.IsRestNow)
        {
            parms.Branch.TaskHandler.ResetRestTick();
        }
        return (succeeded, true);
    }
}