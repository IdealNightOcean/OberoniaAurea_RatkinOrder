using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker_Memorial : BranchBuildingConstructChecker
{
    public override bool DoubleComfirm => true;
    public override AcceptanceReport CanConstruct(BranchBuildingConstructParameter constructParam, bool resultOnly = false)
    {
        BranchBuilding_MemorialExtension memorialExtension = constructParam.BuildingDef.GetModExtension<BranchBuilding_MemorialExtension>();
        if (memorialExtension is not null)
        {
            if (memorialExtension.requireAllTypesOfMedals)
            {
                IReadOnlyList<BranchMedalRecord> medalRecords = constructParam.Branch.MedalHandler.MedalRecords;
                if (medalRecords.Count < BranchUtility.BranchMedalsArr.Length - 1) // -1是因为有None这个Type
                {
                    return "OARO_Insufficient_SquadMedal".Translate();
                }
                foreach (BranchMedalRecord record in medalRecords)
                {
                    if (record.count < memorialExtension.medalCount)
                    {
                        return "OARO_Insufficient_SquadMedal".Translate();
                    }
                }
                return true;
            }
            else if (constructParam.Branch.MedalHandler.GetMedalCount(memorialExtension.medalType) < memorialExtension.medalCount)
            {
                return "OARO_Insufficient_SquadMedal".Translate();
            }
        }

        return true;
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