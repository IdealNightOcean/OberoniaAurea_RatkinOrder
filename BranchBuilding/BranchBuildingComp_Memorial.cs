namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingCompProperties_Memorial : BranchBuildingCompProperties
{
    public BranchMedalDef medalDef;
    public int medalCount = 1;

    public BranchBuildingCompProperties_Memorial()
    {
        compClass = typeof(BranchBuildingComp_Memorial);
    }
}

public class BranchBuildingComp_Memorial : BranchBuildingComp
{
    private BranchBuildingCompProperties_Memorial Props => (BranchBuildingCompProperties_Memorial)props;
    public override void PostInitActive()
    {
        base.PostInitActive();
        if (Props.medalDef is null)
        {
            return;
        }

        BranchMedalHandler medalHandler = parent.Branch.MedalHandler;

        int count = Props.medalCount - medalHandler.GetMedalCount(Props.medalDef);
        if (count > 0)
        {
            medalHandler.AddMedal(Props.medalDef, count);
        }
    }
}