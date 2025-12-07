using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_UnlockSupportAuthority(BranchInteractionDef def) : BranchInteractionWorker_MapOnly(def)
{
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