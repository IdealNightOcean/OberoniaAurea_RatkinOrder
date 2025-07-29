using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.SquadStat;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingConstructChecker_Memorial : BranchBuildingConstructChecker
{
    public override bool DoubleComfirm => true;
    public override AcceptanceReport CanConstruct(Branch branch, BranchBuildingDef def, bool inSpecialSlot, bool byPlayer, Caravan caravan = null, bool resultOnly = false)
    {
        BranchBuilding_MemorialExtension memorialExtension = def.GetModExtension<BranchBuilding_MemorialExtension>();
        if (memorialExtension is null)
        {
            return false;
        }
        else
        {
            return memorialExtension.IsSatisfyRequirements(branch) ? true : (resultOnly ? false : "OARO_LackOfSquadMedal".Translate());
        }
    }

    public override void DoubleComfirmAction(Branch branch, BranchBuildingDef def, bool inSpecialSlot, Caravan caravan)
    {
        OberoniaAurea_Frame.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_ConstructionConfirmMemorial".Translate(),
                                                                         acceptAction: delegate
                                                                         {
                                                                             branch.BuildingHandler.StartBuildingConstructionDirectly(def, inSpecialSlot, byPlayer: true, caravan);
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

public class BranchBuilding_MemorialExtension : DefModExtension
{
    public SquadMedal medalType = SquadMedal.None;
    public bool requireAllTypesOfMedals = false;
    public short medalCount = 1;

    public bool IsSatisfyRequirements(Branch branch)
    {
        if (branch?.Squad is null)
        {
            return false;
        }

        if (requireAllTypesOfMedals)
        {
            IReadOnlyList<MedalRecord> medalRecords = branch.Squad.SquadStat.MedalRecords;
            if (medalRecords.Count < SquadMedalArr.Length - 1) // -1是因为有None这个Type
            {
                return false;
            }
            foreach (MedalRecord record in medalRecords)
            {
                if (record.count < medalCount)
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            return branch.Squad.SquadStat.GetMedalCount(medalType) > medalCount;
        }
    }

    public void CompleteRequirements(Branch branch)
    {
        if (branch?.Squad is null)
        {
            return;
        }

        SquadStat squadStat = branch.Squad.SquadStat;
        if (requireAllTypesOfMedals)
        {
            for (int i = 1; i < SquadMedalArr.Length; i++) // 从1开始，因为0是None
            {
                short count = (short)(medalCount - squadStat.GetMedalCount(SquadMedalArr[i]));
                if (count > 0)
                {
                    squadStat.AddMedal(SquadMedalArr[i], count);
                }
            }
        }
        else
        {
            short count = (short)(medalCount - squadStat.GetMedalCount(medalType));
            if (count > 0)
            {
                squadStat.AddMedal(medalType, count);
            }
        }
    }
}