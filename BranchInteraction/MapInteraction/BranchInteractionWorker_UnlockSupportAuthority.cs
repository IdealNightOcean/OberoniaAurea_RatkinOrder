namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_UnlockSupportAuthority(BranchInteractionDef def) : BranchInteractionWorker_MapOnly(def)
{
    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        parms.Branch.HasSupportAuthority = true;
        return base.InteractionEffect(parms);
    }
}