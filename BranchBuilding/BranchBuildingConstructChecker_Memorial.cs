using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker_Memorial : BranchBuildingConstructChecker
{
    public override bool DoubleComfirm => true;
    public override AcceptanceReport CanConstruct(BranchBuildingConstructParms constructParam, bool resultOnly = false)
    {
        BranchBuildingCompProperties_Memorial memorialComp = constructParam.BuildingDef.GetCompProperties<BranchBuildingCompProperties_Memorial>();
        if (memorialComp is null)
        {
            return true;
        }
        if (constructParam.Branch.MedalHandler.GetMedalCount(memorialComp.medalDef) < memorialComp.medalCount)
        {
            return "OARO_Insufficient_SquadMedal".Translate();
        }
        return true;
    }

    public override void DoubleComfirmAction(BranchBuildingConstructParms constructParam)
    {
        constructParam.ByPlayer = true;
        Branch branch = constructParam.Branch;
        if (constructParam.BuildingDef.honorDef is null)
        {
            branch.BuildingHandler.StartBuildingConstructionDirectly(constructParam);
            return;
        }

        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: "OARO_ConstructionConfirm_Memorial".Translate(constructParam.BuildingDef.honorDef.Named(KeyLibrary_FormatArgName.HONORDEF)),
            ratkinOrder: branch.RatkinOrder,
            acceptAction: () => branch.BuildingHandler.StartBuildingConstructionDirectly(constructParam));

        Find.WindowStack.Add(nodeTree);
    }
}