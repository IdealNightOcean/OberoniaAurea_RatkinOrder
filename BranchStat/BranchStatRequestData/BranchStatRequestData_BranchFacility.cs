
namespace OberoniaAurea.RatkinOrder;

public class BranchStatRequestData_BranchFacility : BranchStatRequestData_BranchConstruction<BranchFacilityDef>
{
    public BranchFacilityLevel FacilityLevel { get; set; }

    public BranchStatRequestData_BranchFacility(Branch branch, BranchStatDef statDef, BranchFacilityDef facilityDef, BranchFacilityLevel facilityLevel) : base(branch, statDef, facilityDef)
    {
        this.FacilityLevel = facilityLevel;
    }
}