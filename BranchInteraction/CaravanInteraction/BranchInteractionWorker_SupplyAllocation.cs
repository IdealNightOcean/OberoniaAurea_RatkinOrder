using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_SupplyAllocation(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (parms.Branch.FacilityHandler.GetFacilityLevel(OARO_ModDefOf.OARO_SupportFacility) < BranchFacilityLevel.Normal)
        {
            return resultOnly ? false : "OARO_Insufficient_FacilityLevel".Translate(
                OARO_ModDefOf.OARO_SupportFacility.Named("FACILITY"),
                BranchFacilityLevel.Normal.GetFacilityLevelLabel().Named(KeyLibrary_FormatArgName.Level));
        }

        return base.BranchValidate(parms, resultOnly);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        float gainCount = parms.TargetCaravan.PawnsListForReading.Count * 50f + parms.RatkinOrder.Esteem * 0.01f * 100f;
        if (parms.Branch.FacilityHandler.GetFacilityLevel(OARO_ModDefOf.OARO_SupportFacility) >= BranchFacilityLevel.Good)
        {
            gainCount *= 2f;
        }

        Thing pemmican = ThingMaker.MakeThing(ThingDefOf.Pemmican);
        int gainCountInt = Mathf.Max(1, (int)gainCount);
        pemmican.stackCount = gainCountInt;

        CaravanInventoryUtility.GiveThing(parms.TargetCaravan, pemmican);

        Find.WindowStack.Add(OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo
        (
            text: "OARO_BranchInteraction_SupplyAllocation".Translate(
                parms.Branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                GenLabel.ThingsLabel([pemmican]).Named(KeyLibrary_FormatArgName.ThingsInfo)),
            ratkinOrder: parms.RatkinOrder
        ));

        return (true, true);
    }
}
