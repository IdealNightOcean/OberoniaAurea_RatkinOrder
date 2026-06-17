using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchInteractionWorker_MapOnly(BranchInteractionDef def) : BranchInteractionWorker(def)
{
    protected override AcceptanceReport ParmsValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (Def.target != BranchInteractionDef.InteractionTarget.Map)
        {
            return resultOnly ? false : "OARO_BranchInteraction_InconsistentTargetType".Translate().Colorize(ColorLibrary.RedReadable);
        }
        if (parms.Target is not Map)
        {
            return resultOnly ? false : "OARO_NeedAMap".Translate();
        }
        return base.ParmsValidate(parms, resultOnly);
    }
}