using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionWorker_RequestCaravanMedicalAssistance(BranchInteractionDef def) : BranchInteractionWorker_CaravanOnly(def)
{
    protected override (bool succeeded, bool doPostApply) InteractionEffect(BranchInteractionParms parms)
    {
        List<FloatMenuOption> options = [];
        Caravan caravan = parms.Caravan;
        foreach (Pawn p in caravan.PawnsListForReading)
        {
            options.Add(new FloatMenuOption(p.LabelShort, () => ApplyMedicalAssistance(parms, p)));
        }
        Find.WindowStack.Add(new FloatMenu(options));

        return (true, false);
    }

    private void ApplyMedicalAssistance(BranchInteractionParms parms, Pawn pawn)
    {
        BranchResident_CaravanMedicalAssistance resident = (BranchResident_CaravanMedicalAssistance)BranchResident.GenerateBranchResident(
            def: BranchResidentDefOf.OARO_CaravanMedicalAssistance,
            residentPawn: pawn,
            deployDaysOverride: BranchResidentDefOf.OARO_CaravanMedicalAssistance.defaultDeployDays);

        bool result = parms.Branch.ResidentHandler.AddResident(resident);

        PostApplyInteraction(parms, succeeded: result);
    }
}
