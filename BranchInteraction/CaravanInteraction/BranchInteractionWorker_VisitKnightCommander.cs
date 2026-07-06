using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public class BranchInteractionWorker_VisitKnightCommander(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{

    protected override AcceptanceReport BranchValidate(BranchInteractionParms parms, bool resultOnly)
    {
        if (!parms.Branch.CommanderVisitable)
        {
            return resultOnly ? false : "OARO_CommanderNotVisitable".Translate();
        }
        return base.BranchValidate(parms, resultOnly);
    }


    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        parms.RatkinOrder.EsteemHandler.AdjustEsteem(2, byPlayer: true, reason: Def.LabelCap);
        ResidentPawnsManager.Instance.AllKnightsGainMeditation(100f, parms.RatkinOrder, directly: false);
        ThingDef privateBrewDef = DefDatabase<ThingDef>.GetNamedSilentFail("OARO_CommanderPrivateBrew");
        Thing privateBrew = ThingMaker.MakeThing(privateBrewDef);
        privateBrew.stackCount = 5;
        CaravanInventoryUtility.GiveThing(parms.TargetCaravan, privateBrew);

        Find.WindowStack.Add(OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_VisitKnightCommander_Reply".Translate(
                    parms.RatkinOrder.NameColored.Named(OARO_KeyLibrary_FormatArgName.OrderName),
                    parms.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                    2.Named("Esteem"),
                    100.Named("Meditation"),
                    GenLabel.ThingsLabel([privateBrew]).Named(KeyLibrary_FormatArgName.ThingsInfo)),
            ratkinOrder: parms.RatkinOrder));

        return (true, true);
    }
}