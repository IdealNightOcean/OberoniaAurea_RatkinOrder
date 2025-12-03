using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_RequestCombatReadiness(BranchInteractionDef def) : BranchInteractionWorker_Targetless(def)
{
    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        AcceptanceReport acceptance = parms.Branch.TaskHandler.CanSwitchToTask(BranchTaskDefOf.OARO_CombatReadiness, resultOnly: false);
        if (!acceptance)
        {
            return acceptance;
        }
        if (!parms.Branch.HasSupportAuthority)
        {

        }

        return base.BranchValidate(parms, resultOnly);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        bool succeeded = parms.Branch.TaskHandler.TrySwitchToTask(BranchTaskDefOf.OARO_CombatReadiness, endCurIfCantSwitch: false);
        return (succeeded, true);
    }
}