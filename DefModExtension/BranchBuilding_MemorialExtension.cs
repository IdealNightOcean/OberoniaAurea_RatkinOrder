using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.SquadStat;

namespace OberoniaAurea.RatkinOrder;

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
