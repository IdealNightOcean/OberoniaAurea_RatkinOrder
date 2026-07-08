using System;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatRequestData : StatRequestData<BranchStatDef, Branch>
{
    public BranchStatRequestData() { }
    public BranchStatRequestData(Branch branch, BranchStatDef statDef) : base(branch, statDef)
    { }
}


public class BranchStatRequestData_Building : BranchStatRequestData
{
    public BranchBuildingDef BuildingDef { get; set; }
    public BranchStatRequestData_Building() { }
    public BranchStatRequestData_Building(Branch branch, BranchStatDef statDef, BranchBuildingDef buildingDef) : base(branch, statDef)
    {
        BuildingDef = buildingDef ?? throw new ArgumentNullException(nameof(buildingDef));
    }
}


public class BranchStatRequestData_Facility : BranchStatRequestData
{
    public BranchFacilityDef FacilityDef { get; set; }
    public BranchStatRequestData_Facility() { }
    public BranchStatRequestData_Facility(Branch branch, BranchStatDef statDef, BranchFacilityDef facilityDef) : base(branch, statDef)
    {
        FacilityDef = facilityDef ?? throw new ArgumentNullException(nameof(facilityDef));
    }
}