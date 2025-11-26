using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingCompProperties_Interaction : BranchBuildingCompProperties
{
    public BranchInteractionDef def;
    [MustTranslate]
    public string labelOverride;

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

    public string InteractionLabel => Props.labelOverride ?? Def.label;

    public AcceptanceReport CanUseInteraction(Caravan caravan, bool resultOnly)
    {
        if (Def is null)
        {
            return false;
        }
        return Def.Worker.CanUseInteraction(new BranchInteractionParms(parent.Branch, caravan, parent), resultOnly: resultOnly);
    }

    public void TryApplyInteraction(Caravan caravan)
    {
        Def?.Worker.TryApplyInteraction(new BranchInteractionParms(parent.Branch, caravan, parent));
    }

    public override void PostPostActive()
    {
        parent.Branch.BuildingHandler.InteractionComps.Add(this);
    }

    public override void PostPostDeactive()
    {
        parent.Branch.BuildingHandler.InteractionComps.Remove(this);
    }
}