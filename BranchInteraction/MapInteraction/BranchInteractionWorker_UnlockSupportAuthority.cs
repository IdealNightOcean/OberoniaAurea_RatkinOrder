using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_UnlockSupportAuthority(BranchInteractionDef def) : BranchInteractionWorker_MapOnly(def)
{
    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (parms.Branch.HasSupportAuthority)
        {
            return resultOnly ? false : "OARO_AlreadyHasSupportAuthority".Translate();
        }
        if (parms.RatkinOrder.Faction.HostileTo(Faction.OfPlayer))
        {
            return resultOnly ? false : "OARO_OrderFaction_Hostile".Translate();
        }
        return base.BranchValidate(parms, resultOnly);
    }

    protected override void ApplyInteraction(BranchInteractionParms parms)
    {
        if (BranchUtility.IsInAffectedRange(parms.Branch, parms.Map.Tile))
        {
            base.ApplyInteraction(parms);
        }
        else
        {
            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                "OARO_UnlockSupportAuthority_OutOfAffectedRange".Translate(parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName)),
                parms.RatkinOrder,
                acceptAction: () => base.ApplyInteraction(parms));

            Find.WindowStack.Add(nodeTree);
        }
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        parms.Branch.HasSupportAuthority = true;
        return base.InteractionEffect(parms);
    }
}