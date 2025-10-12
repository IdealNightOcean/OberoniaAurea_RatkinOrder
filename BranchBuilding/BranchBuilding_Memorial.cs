namespace OberoniaAurea.RatkinOrder;

public class BranchBuilding_Memorial : BranchBuildingWithComps
{
    public override void InitActive()
    {
        base.InitActive();
        BranchBuilding_MemorialExtension memorialExtension = def.GetModExtension<BranchBuilding_MemorialExtension>();
        if (memorialExtension is not null)
        {
            BranchMedalHandler medalHandler = branch.MedalHandler;
            if (memorialExtension.requireAllTypesOfMedals)
            {
                for (int i = 1; i < BranchUtility.BranchMedalsArr.Length; i++) // 从1开始，因为0是None
                {
                    short count = (short)(memorialExtension.medalCount - medalHandler.GetMedalCount(BranchUtility.BranchMedalsArr[i]));
                    if (count > 0)
                    {
                        medalHandler.AddMedal(BranchUtility.BranchMedalsArr[i], count);
                    }
                }
            }
            else
            {
                short count = (short)(memorialExtension.medalCount - medalHandler.GetMedalCount(memorialExtension.medalType));
                if (count > 0)
                {
                    medalHandler.AddMedal(memorialExtension.medalType, count);
                }
            }
        }
    }
}