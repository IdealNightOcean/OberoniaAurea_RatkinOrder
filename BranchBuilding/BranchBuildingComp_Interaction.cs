using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingCompProperties_Interaction : BranchBuildingCompProperties
{
    public BranchInteractionDef def;

    public BranchBuildingCompProperties_Interaction()
    {
        compClass = typeof(BranchBuildingComp_Interaction);
    }

    public override IEnumerable<string> ConfigErrors(BranchBuildingDef parentDef)
    {
        if (def is null)
        {
            yield return "has a null BranchInteractionDef.";
        }
    }
}

public class BranchBuildingComp_Interaction : BranchBuildingComp
{
    private BranchBuildingCompProperties_Interaction Props => (BranchBuildingCompProperties_Interaction)props;
    public BranchInteractionDef Def => Props.def;


    public override void PostPostActive()
    {
        parent.Branch.BuildingHandler.InteractionComps.Add(this);
    }

    public override void PostPostDeactive()
    {
        parent.Branch.BuildingHandler.InteractionComps.Add(this);
    }
}