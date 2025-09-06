using System;

namespace OberoniaAurea.RatkinOrder;

public static class BranchBuildingMaker
{
    public static BranchBuilding MakeBranchBuilding(BranchBuildingDef def)
    {
        BranchBuilding building = (BranchBuilding)Activator.CreateInstance(def.buildingClass);
        building.Def = def;

        return building;
    }
}
