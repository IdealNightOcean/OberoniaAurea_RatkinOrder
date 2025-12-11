using Verse;

namespace OberoniaAurea.RatkinOrder;


public class BranchInteractionWorker_VisitKnightCommander(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{

    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (!parms.Branch.CommanderVisitable)
        {
            return resultOnly ? false : "OARO_CommanderNotVisitable".Translate();
        }
        return base.BranchValidate(parms, resultOnly);
    }


    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        parms.RatkinOrder.EsteemHandler.AdjustEsteem(1, byPlayer: true, reason: "OARO_Esteem_VisitKnightCommander".Translate());


        return (true, true);
    }
}