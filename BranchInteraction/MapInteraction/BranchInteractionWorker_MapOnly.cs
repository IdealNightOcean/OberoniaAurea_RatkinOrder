using OberoniaAurea_Frame;
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
        if (parms.Map is null)
        {
            return resultOnly ? false : "OARO_NeedAMap".Translate();
        }
        return base.ParmsValidate(parms, resultOnly);
    }

    protected override AcceptanceReport TargetValidate(BranchInteractionParms parms, bool resultOnly)
    {
        RatkinOrder ratkinOrder = parms.RatkinOrder;
        if (Def.needRecommendation > 0 && !parms.Map.HasEnoughRecommendation(Def.needRecommendation))
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(Def.needRecommendation.Named(KeyLibrary_FormatArgName.Count));
        }
        if (Def.needSilver > 0 && !parms.Map.HasEnoughThingsOfDef(ThingDefOf.Silver, Def.needSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, Def.needSilver);
        }
        return true;
    }

    protected override void DoTargetCost(BranchInteractionParms parms)
    {
        if (Def.needRecommendation > 0)
        {
            RecommendationUtility.UseRecommendationOfMap(parms.Map, Def.needRecommendation);
        }
        if (Def.needSilver > 0)
        {
            parms.Map.DestoryThingsOfDef(ThingDefOf.Silver, Def.needSilver);
        }
    }

}
