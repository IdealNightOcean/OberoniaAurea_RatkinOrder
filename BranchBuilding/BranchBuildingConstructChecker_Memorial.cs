using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea_Frame.DataLibrary;
using System.Text;
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
        if (constructParam.Branch.MedalHandler.GetMedalCount(memorialComp.medalChivalry) < memorialComp.medalCount)
        {
            return resultOnly ? false : "OARO_Insufficient_SquadMedalOf".Translate(
                memorialComp.medalChivalry.Named(KeyLibrary_FormatArgName.DEF),
                memorialComp.medalCount.Named(KeyLibrary_FormatArgName.Count));
        }
        return true;
    }

    public override void DoubleComfirmAction(BranchBuildingConstructParms constructParam)
    {
        constructParam.ByPlayer = true;
        Branch branch = constructParam.Branch;
        BranchHonorDef honorDef = constructParam.BuildingDef.honorDef;
        if (honorDef is null)
        {
            branch.BuildingHandler.StartBuildingConstructionDirectly(constructParam);
            return;
        }
        StringBuilder textBuilder = new("OARO_ConstructionConfirm_Memorial".Translate(honorDef.Named(OARO_KeyLibrary_FormatArgName.HONORDEF)));
        textBuilder.AppendLine();
        textBuilder.AppendLine();
        textBuilder.AppendLine(honorDef.LabelCap);
        textBuilder.AppendLine(honorDef.description);

        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_UIUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
            text: textBuilder.ToTaggedString(),
            ratkinOrder: branch.RatkinOrder,
            acceptAction: () => branch.BuildingHandler.StartBuildingConstructionDirectly(constructParam));

        Find.WindowStack.Add(nodeTree);
    }
}