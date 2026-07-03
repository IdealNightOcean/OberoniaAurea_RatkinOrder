using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResident_CaravanMedicalAssistance : BranchResident
{
    public override void StartResidency(Branch branch)
    {
        base.StartResidency(branch);
        List<Hediff> badHediffs = pawn.health.hediffSet.hediffs.Where(h => h.def.isBad).ToList();
        if (badHediffs.Count > 0)
        {
            foreach (Hediff hediff in badHediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }

    public override void EndResidency() { }
}