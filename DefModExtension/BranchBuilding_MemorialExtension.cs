using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchMedalRecord;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding_MemorialExtension : DefModExtension
{
    public BranchMedalType medalType = BranchMedalType.None;
    public bool requireAllTypesOfMedals = false;
    public short medalCount = 1;

    public bool IsSatisfyRequirements(Branch branch)
    {
        if (requireAllTypesOfMedals)
        {
            IReadOnlyList<BranchMedalRecord> medalRecords = branch.MedalHandler.MedalRecords;
            if (medalRecords.Count < BranchUtility.BranchMedalsArr.Length - 1) // -1是因为有None这个Type
            {
                return false;
            }
            foreach (BranchMedalRecord record in medalRecords)
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
            return branch.MedalHandler.GetMedalCount(medalType) > medalCount;
        }
    }

    public void CompleteRequirements(Branch branch)
    {
        BranchMedalHandler medalHandler = branch.MedalHandler;
        if (requireAllTypesOfMedals)
        {
            for (int i = 1; i < BranchUtility.BranchMedalsArr.Length; i++) // 从1开始，因为0是None
            {
                short count = (short)(medalCount - medalHandler.GetMedalCount(BranchUtility.BranchMedalsArr[i]));
                if (count > 0)
                {
                    medalHandler.AddMedal(BranchUtility.BranchMedalsArr[i], count);
                }
            }
        }
        else
        {
            short count = (short)(medalCount - medalHandler.GetMedalCount(medalType));
            if (count > 0)
            {
                medalHandler.AddMedal(medalType, count);
            }
        }
    }
}