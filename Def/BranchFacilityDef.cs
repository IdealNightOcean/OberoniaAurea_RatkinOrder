using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityDef : BranchConstructionDef
{
    public BranchFacilityLevelStage poorStage;
    public BranchFacilityLevelStage normalStage;
    public BranchFacilityLevelStage goodStage;
    public BranchFacilityLevelStage excellentStage;

    public BranchFacilityLevelStage GetLevelStage(BranchFacilityLevel level)
    {
        return level switch
        {
            BranchFacilityLevel.Poor => poorStage,
            BranchFacilityLevel.Normal => normalStage,
            BranchFacilityLevel.Good => goodStage,
            BranchFacilityLevel.Excellent => excellentStage,
            _ => poorStage
        };
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (poorStage is null)
        {
            yield return $"has null {nameof(poorStage)}.";
        }
        if (normalStage is null)
        {
            yield return $"has null {nameof(normalStage)}.";
        }
        if (goodStage is null)
        {
            yield return $"has null {nameof(goodStage)}.";
        }
        if (excellentStage is null)
        {
            yield return $"has null {nameof(excellentStage)}.";
        }
    }
}