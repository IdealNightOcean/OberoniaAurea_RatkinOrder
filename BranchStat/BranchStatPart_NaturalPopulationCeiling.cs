using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_NaturalPopulationCeiling : BranchStatPart
{
    public override float PostTransform(Branch branch, float curValue)
    {
        return curValue + branch.FacilityHandler.TotalFacilityLevel * 200;
    }

    public override void ModifyExplanation(Branch branch, StringBuilder explanation)
    {
        explanation.Append("    ");
        explanation.AppendLine("OARO_StatOffset_FacilityLevel".Translate((branch.FacilityHandler.TotalFacilityLevel * 200).ToStringWithSign())
                                                              .Colorize(Color.green));
    }
}