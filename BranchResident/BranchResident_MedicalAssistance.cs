using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResident_MedicalAssistance : BranchResident
{
    public override int Priority => 550;
    protected BranchResident_MedicalAssistance() : base() { }
    public BranchResident_MedicalAssistance(Pawn resident, int totalDeployDays) : base(resident, totalDeployDays) { }
    public override void StartResidency(Branch branch)
    {
        base.StartResidency(branch);
        List<Hediff> badHediffs = resident.health.hediffSet.hediffs.Where(h => h.def.isBad).ToList();
        if (badHediffs.Count > 0)
        {
            foreach (Hediff hediff in badHediffs)
            {
                resident.health.RemoveHediff(hediff);
            }
        }
    }

    public override void EndResidency(Branch branch) { }
}