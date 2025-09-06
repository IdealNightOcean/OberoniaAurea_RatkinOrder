using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker_Memorial : BranchBuildingConstructChecker
{
    public override bool DoubleComfirm => true;
    public override AcceptanceReport CanConstruct(BranchBuildingConstructParameter constructParam, bool resultOnly = false)
    {
        BranchBuilding_MemorialExtension memorialExtension = constructParam.BuildingDef.GetModExtension<BranchBuilding_MemorialExtension>();
        if (memorialExtension is null)
        {
            return false;
        }
        else
        {
            return memorialExtension.IsSatisfyRequirements(constructParam.Branch) ? true : (resultOnly ? false : "OARO_Insufficient_SquadMedal".Translate());
        }
    }

    public override void DoubleComfirmAction(BranchBuildingConstructParameter constructParam)
    {
        constructParam.ByPlayer = true;
        OberoniaAurea_Frame.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_ConstructionConfirm_Memorial".Translate(),
                                                                         acceptAction: delegate
                                                                         {
                                                                             constructParam.Branch.BuildingHandler.StartBuildingConstructionDirectly(constructParam);
                                                                         });
    }
}

public class BranchBuilding_Memorial : BranchBuilding
{
    public override void PostAddBuilding(Branch branch)
    {
        branch.RecacheIsHonor();
    }

    public override void PostRemoveBuilding(Branch branch)
    {
        branch.RecacheIsHonor();
    }
}