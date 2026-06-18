using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_NaturalPopulationCeiling : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue += branch.FacilityHandler.TotalFacilityLevel * 200;
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {
        explanation.Append(ExplanatCap);
        explanation.AppendLine("OARO_ChangeOffset_FacilityLevel".Translate((branch.FacilityHandler.TotalFacilityLevel * 200).ToStringWithSign())
                                                                .Colorize(Color.green));
    }
}