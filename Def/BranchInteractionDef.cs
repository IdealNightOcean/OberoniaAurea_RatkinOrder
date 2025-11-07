using System;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionDef : InteractionDefBase
{
    public Type workerClass;
    private BranchInteractionWorker worker;
    public BranchInteractionWorker Worker => worker ??= (BranchInteractionWorker)Activator.CreateInstance(workerClass, args: this);

    public bool isCommonInteraction;
    public BranchBuildingDef relatedBranchBuilding;

    public float needSupply = -1f;
    public int floorPopulation = -1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (workerClass is null)
        {
            yield return "has a null workerClass.";
        }
        if (isCommonInteraction && relatedBranchBuilding is not null)
        {
            isCommonInteraction = false;
            yield return "is CommonInteraction but has \"relatedBranchBuilding\". Set \"isCommonInteraction\" to Fasle.";
        }
    }
}