using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchInteractionWorker_CaravanOnly(BranchInteractionDef def) : BranchInteractionWorker(def)
{
    protected override AcceptanceReport ParmsValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (Def.target != BranchInteractionDef.InteractionTarget.Caravan)
        {
            return resultOnly ? false : "OARO_BranchInteraction_InconsistentTargetType".Translate().Colorize(ColorLibrary.RedReadable);
        }
        if (parms.Target is not Caravan)
        {
            return resultOnly ? false : "OARO_NeedACaravan".Translate();
        }
        return base.ParmsValidate(parms, resultOnly);
    }
}