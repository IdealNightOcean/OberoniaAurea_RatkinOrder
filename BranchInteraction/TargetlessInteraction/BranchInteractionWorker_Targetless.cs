using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchInteractionWorker_Targetless(BranchInteractionDef def) : BranchInteractionWorker(def)
{
    protected override AcceptanceReport ParmsValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (Def.target != BranchInteractionDef.InteractionTarget.None)
        {
            return resultOnly ? false : "OARO_BranchInteraction_InconsistentTargetType".Translate().Colorize(ColorLibrary.RedReadable);
        }
        return base.ParmsValidate(parms, resultOnly);
    }
    protected override AcceptanceReport TargetValidate(BranchInteractionParms parms, bool resultOnly) => true;
    protected override void DoTargetCost(BranchInteractionParms parms) { }
}