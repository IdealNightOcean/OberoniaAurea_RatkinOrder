using OberoniaAurea_Frame;
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
        if (parms.Caravan is null)
        {
            return resultOnly ? false : "OARO_NeedACaravan".Translate();
        }
        return base.ParmsValidate(parms, resultOnly);
    }

    protected override AcceptanceReport TargetValidate(BranchInteractionParms parms, bool resultOnly)
    {
        RatkinOrder ratkinOrder = parms.RatkinOrder;
        if (Def.needRecommendation > 0 && CaravanInventoryUtility.HasThings(parms.Caravan, OARO_ThingDefOf.OARO_OrderRecommendation, Def.needRecommendation, (t) => ((OrderRecommendation)t).RatkinOrder == ratkinOrder))
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation, ratkinOrder.Name);
        }
        if (Def.needSilver > 0 && !CaravanInventoryUtility.HasThings(parms.Caravan, ThingDefOf.Silver, Def.needSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, Def.needSilver);
        }
        return true;
    }

    protected override void DoTargetCost(BranchInteractionParms parms)
    {
        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfCaravan(parms.RatkinOrder, parms.Caravan, Def.needRecommendation);
        }
        if (Def.needSilver > 0)
        {
            parms.Caravan.RemoveThingsOfDef(ThingDefOf.Silver, Def.needSilver);
        }
    }
}