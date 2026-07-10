using System.Text;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker_FacilityCost(BranchStatDef statDef) : BranchStatWorker_ConstructionCost<BranchFacilityDef>(statDef)
{

    public override bool PrepareInitialBaseValue(BranchStatRequestData requestData, ref StatComputeState curValue, float? baseValueOverride = null, bool resultOnly = true, StringBuilder explanation = null)
    {
        if (!TryCastRequestData<BranchStatRequestData_BranchFacility>(requestData, out BranchStatRequestData_BranchFacility facilityData))
        {
            curValue.IsConverged = true;
            return false;
        }

        float baseValue = baseValueOverride ?? facilityData.ConstructionDef.GetLevelStage(facilityData.FacilityLevel)?.silverCost ?? 2000f; ;
        curValue.Value = baseValue;
        if (!resultOnly)
        {
            explanation.AppendLine(StatDef.GetBaseValueExplanation(baseValue));
        }
        return true;
    }
}
