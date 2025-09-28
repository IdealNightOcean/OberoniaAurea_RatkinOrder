using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandWeighter_SupplyDepotCotr : BranchDemandWeighter
{
    public override float GetDemandWeight(BranchDemandDef def, Branch branch, bool resultOnly, out string explain)
    {
        explain = resultOnly ? null : "OARK_DemandWeight_Default".Translate(def.baseSelectWeight);
        float medalWeight = branch.MedalHandler.GetMedalCount(BranchMedalType.Tenacity) * 15f;
        if (!resultOnly)
        {
            explain = explain + "\n" + "OARK_DemandWeight_TenacityMedal".Translate(medalWeight.ToStringWithSign("F0").Colorize(Color.green));
        }
        return def.baseSelectWeight + medalWeight;
    }
}