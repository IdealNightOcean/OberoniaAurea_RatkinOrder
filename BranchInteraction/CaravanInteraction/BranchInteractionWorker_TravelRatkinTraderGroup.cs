using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_TravelRatkinTraderGroup(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    protected override AcceptanceReport ParmsValidate(BranchInteractionParms parms, bool resultOnly)
    {
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: false);
        if (map is null)
        {
            return resultOnly ? false : "OARO_NoAvailablePlayerHomeMap".Translate();
        }
        if (OARO_ModDefOf.Rakinia_TravelRatkin is null || OAFrame_FactionUtility.FirstAvailableFactionOfDef(OARO_ModDefOf.Rakinia_TravelRatkin, FactionValidationParams.NonHostileNormalFaction) is null)
        {
            return resultOnly ? false : "OARO_NoNonHostileTravelRatkin".Translate();
        }

        return base.ParmsValidate(parms, resultOnly);
    }

    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: false);
        if (map is null)
        {
            return (false, false);
        }

        IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("OAGene_TravelRatkinTraderGroup");
        if (incidentDef is null)
        {
            return (false, false);
        }

        Faction faction = OAFrame_FactionUtility.FirstAvailableFactionOfDef(OARO_ModDefOf.Rakinia_TravelRatkin, FactionValidationParams.NonHostileNormalFaction);
        if (faction is null)
        {
            return (false, false);
        }

        IncidentParms incidentParms = new()
        {
            target = map,
            faction = faction,
            forced = true
        };

        OAFrame_MiscUtility.AddNewQueuedIncident(incidentDef, delayTicks: 3 * 60000, incidentParms);
        Find.WindowStack.Add(OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo
            (
                text: "OARO_BranchInteraction_TravelRatkinTraderGroup".Translate(
                    parms.Branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                    faction.Named(KeyLibrary_FormatArgName.FACTION),
                    map.Parent.Named("map")),
                ratkinOrder: parms.RatkinOrder
            ));

        return (true, true);
    }
}
