using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_BranchPotency(BranchStatDef statDef) : BranchStatWorker(statDef)
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        Branch branch = requestData.Target;
        bool hasModification = false;

        float factor = branch.TraditionHandler.ExtraBranchPotencyFactor.Value;
        if (factor != 1f)
        {
            hasModification = true;
            curValue.Value *= factor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_BranchTradition"
                    .Translate(OARO_StatExplanationUtility.FactorNamedArgument(factor, StatDef))
                    .ColorizeStrByFactor(factor, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }

        }

        float facilityAdd = branch.FacilityHandler.TotalFacilityLevel * 0.025f;
        float medalAdd = branch.MedalHandler.TotalMedalCount * 0.005f;
        factor = facilityAdd + medalAdd;
        if (factor != 1f)
        {
            hasModification = true;
            curValue.Value *= factor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_BranchPotency_FacilityAndMedal"
                          .Translate(
                              OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef),
                              facilityAdd.ToStringWithSign("0.##"),
                              medalAdd.ToStringWithSign("0.##"))
                          .ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return hasModification;
    }

}