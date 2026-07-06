using OberoniaAurea_Frame;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchPotency : BranchStatPart
{
    public override void PostTransform(Branch branch, ref float curValue)
    {
        curValue *= branch.TraditionHandler.ExtraBranchPotencyFactor.Value;

        curValue *= (0.9f + branch.FacilityHandler.TotalFacilityLevel * 0.025f + branch.MedalHandler.TotalMedalCount * 0.005f);
    }

    public override void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation)
    {

        float factor = branch.TraditionHandler.ExtraBranchPotencyFactor.Value;
        if (factor != 1f)
        {
            explanation.Append(ExplanatCap);
            explanation.AppendLine("OARO_ChangeFactor_BranchTradition".Translate(factor.ToString("0.##").Named(KeyLibrary_FormatArgName.Factor))
                                                                .Colorize(factor > 1f ? Color.green : ColorLibrary.RedReadable));
        }
        factor = (0.9f + branch.FacilityHandler.TotalFacilityLevel * 0.025f + branch.MedalHandler.TotalMedalCount * 0.005f);
        float facilityAdd = branch.FacilityHandler.TotalFacilityLevel * 0.025f;
        float medalAdd = branch.MedalHandler.TotalMedalCount * 0.005f;

        explanation.Append(ExplanatCap);
        explanation.AppendLine("OARO_ChangeFactor_BranchPotency_FacilityAndMedal".Translate(factor.ToString("0.##").Named(KeyLibrary_FormatArgName.Factor),
                                                                                            facilityAdd.ToStringWithSign("0.##"),
                                                                                            medalAdd.ToStringWithSign("0.##"))
                                                                                 .Colorize(factor > 1f ? Color.green : ColorLibrary.RedReadable));

    }
}