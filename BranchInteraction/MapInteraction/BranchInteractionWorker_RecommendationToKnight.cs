using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_RecommendationToKnight(BranchInteractionDef def) : BranchInteractionWorker_MapOnly(def)
{
    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (parms.Branch.Squad.MemberCount > parms.Branch.Squad.MemberCeiling - 1f)
        {
            return resultOnly ? false : "OARO_ReachMax_SquadMemberCount".Translate();
        }

        return base.BranchValidate(parms, resultOnly);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        parms.Branch.Squad.AdjustCrew(member: 5f, commander: 0f);
        return (true, true);
    }
}