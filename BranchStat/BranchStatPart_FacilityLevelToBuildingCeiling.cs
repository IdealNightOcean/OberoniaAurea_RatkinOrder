using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_FacilityLevelToBuildingCeiling : BranchStatPart
{
    public override float PostTransform(Branch branch, float value)
    {
        return value += Mathf.FloorToInt(branch.FacilityHandler.TotalFacilityLevel / 8f);
    }
}
