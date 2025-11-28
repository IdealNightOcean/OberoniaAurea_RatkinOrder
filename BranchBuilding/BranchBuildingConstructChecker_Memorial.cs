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
        OberoniaAurea_Frame.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_ConstructionConfirm_Memorial".Translate(),
                                                                         acceptAction: delegate
                                                                         {
                                                                             constructParam.Branch.BuildingHandler.StartBuildingConstructionDirectly(constructParam);
                                                                         });
    }
}