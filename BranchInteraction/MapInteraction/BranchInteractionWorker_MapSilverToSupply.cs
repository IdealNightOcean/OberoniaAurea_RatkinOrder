namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_MapSilverToSupply(BranchInteractionDef def) : BranchInteractionWorker_MapOnly(def)
{
    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        parms.Branch.Supply += 0.25f;
        return (true, true);
    }
}
