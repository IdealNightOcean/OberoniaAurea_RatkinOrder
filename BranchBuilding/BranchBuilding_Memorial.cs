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

            short count = (short)(memorialExtension.medalCount - medalHandler.GetMedalCount(memorialExtension.medalDef));
            if (count > 0)
            {
                medalHandler.AddMedal(memorialExtension.medalDef, count);
            }
        }
    }
}