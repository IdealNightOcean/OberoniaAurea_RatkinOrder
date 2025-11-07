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
        if (def.relatedBranchBuilding != parentDef)
        {
            yield return "'s BranchInteractionDef.relatedBranchBuilding does not match parentDef.";
        }
    }
}

public class BranchBuildingComp_Interaction : BranchBuildingComp
{
    private BranchBuildingCompProperties_Interaction Props => (BranchBuildingCompProperties_Interaction)props;
    public BranchInteractionDef Def => Props.def;

    private bool InteractionValidate()
    {
        return Def is not null && Def.relatedBranchBuilding == parent.Def;
    }

    public override void PostPostActive()
    {
        if (InteractionValidate())
        {
            parent.Branch.BuildingHandler.InteractionComps.Add(this);
        }
    }

    public override void PostPostDeactive()
    {
        if (InteractionValidate())
        {
            parent.Branch.BuildingHandler.InteractionComps.Add(this);
        }
    }
}