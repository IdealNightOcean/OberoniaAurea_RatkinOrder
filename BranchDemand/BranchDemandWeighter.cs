using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandWeighter
{
    public float GetDemandWeightOnly(BranchDemandDef def, Branch branch)
    {
        return GetDemandWeight(def, branch, resultOnly: true, out _);
    }

    public virtual float GetDemandWeight(BranchDemandDef def, Branch branch, bool resultOnly, out string explain)
    {
        explain = resultOnly ? null : "OARK_DemandWeight_Default".Translate(def.baseSelectWeight);
        return def.baseSelectWeight;
    }
}