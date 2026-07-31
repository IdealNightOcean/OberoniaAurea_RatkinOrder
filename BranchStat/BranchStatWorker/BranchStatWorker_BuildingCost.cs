using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_BuildingCost(BranchStatDef statDef) : BranchStatWorker_ConstructionCost<BranchBuildingDef>(statDef)
{
    public override bool PrepareInitialBaseValue(BranchStatRequestData requestData, ref StatComputeState curValue, float? baseValueOverride = null, bool resultOnly = true, StringBuilder explanation = null)
    {
        if (!TryCastRequestData<BranchStatRequestData_BranchConstruction<BranchBuildingDef>>(requestData, out BranchStatRequestData_BranchConstruction<BranchBuildingDef> buildingData))
        {
            curValue.IsConverged = true;
            return false;
        }

        float baseValue = baseValueOverride ?? buildingData.ConstructionDef.silverCost;
        curValue.Value = baseValue;
        if (!resultOnly)
        {
            explanation.AppendLine(StatDef.GetBaseValueExplanation(baseValue));
        }
        return true;
    }


    public override bool PostTransModify(BranchStatRequestData requestData,
                                     ref StatComputeState curValue,
                                     bool resultOnly = true,
                                     StringBuilder explanation = null)
    {
        bool baseReult = base.PostTransModify(requestData, ref curValue, resultOnly, explanation);
        if (curValue.IsConverged)
            return baseReult;

        BranchStatRequestData_BranchConstruction<BranchBuildingDef> buildingRequest = requestData as BranchStatRequestData_BranchConstruction<BranchBuildingDef>;
        if (buildingRequest.Target.PopulationHandler.Population < buildingRequest.ConstructionDef.suggestedMinPopulation)
        {
            float factor = 2f;
            curValue.Value *= factor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BuildSilverCost_InsufficientPopulation"
                    .Translate(OARO_StatExplanationUtility.FactorNamedArgument(factor, StatDef))
                    .ColorizeStrByFactor(factor, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return true;
    }
}
