using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchMedalRecord;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandWeighter_FamineVillage : BranchDemandWeighter
{
    public override float GetDemandWeight(BranchDemandDef def, Branch branch, bool resultOnly, out string explain)
    {
        explain = resultOnly ? null : "OARK_DemandWeight_Default".Translate(def.baseSelectWeight);
        float medalWeight = branch.MedalHandler.GetMedalCount(BranchMedalType.Justice) * 20f;
        if (!resultOnly)
        {
            explain = explain + "\n" + "OARK_DemandWeight_JusticeMedal".Translate(medalWeight.ToStringWithSign("F0").Colorize(Color.green));
        }
        return def.baseSelectWeight + medalWeight;
    }
}