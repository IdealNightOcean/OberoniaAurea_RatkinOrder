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
            BranchMedalRecord.BranchMedalType[] branchMedalsArr = EnumArraryLibrary.BranchMedalsArr;
            if (memorialExtension.requireAllTypesOfMedals)
            {
                for (int i = 1; i < branchMedalsArr.Length; i++) // 从1开始，因为0是None
                {
                    short count = (short)(memorialExtension.medalCount - medalHandler.GetMedalCount(branchMedalsArr[i]));
                    if (count > 0)
                    {
                        medalHandler.AddMedal(branchMedalsArr[i], count);
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