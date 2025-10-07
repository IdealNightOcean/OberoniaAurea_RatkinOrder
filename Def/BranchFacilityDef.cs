using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityDef : Def
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

    /// <param name="minLevelExclude">最小等级（不包含）</param>
    /// <param name="maxLevelInclude">最大等级（包含）</param>
    public IEnumerable<BranchFacilityLevelStage> GetAllUpgradeStages(BranchFacilityLevel minLevelExclude, BranchFacilityLevel maxLevelInclude)
    {
        if (minLevelExclude >= maxLevelInclude || minLevelExclude >= BranchFacilityLevel.Excellent)
        {
            yield break;
        }
        for (BranchFacilityLevel level = minLevelExclude + 1; level <= maxLevelInclude; level++)
        {
            BranchFacilityLevelStage stage = GetLevelStage(level);
            if (stage is not null)
            {
                yield return stage;
            }
        }
    }

    /// <param name="minLevelExclude">最小等级（不包含）</param>
    /// <param name="maxLevelInclude">最大等级（包含）</param>
    public IEnumerable<BranchFacilityLevelStage> GetAllDowngradeStages(BranchFacilityLevel maxLevelInclude, BranchFacilityLevel minLevelExclude)
    {
        if (minLevelExclude >= maxLevelInclude || minLevelExclude >= BranchFacilityLevel.Excellent)
        {
            yield break;
        }
        for (BranchFacilityLevel level = maxLevelInclude; level > minLevelExclude; level--)
        {
            BranchFacilityLevelStage stage = GetLevelStage(level);
            if (stage is not null)
            {
                yield return stage;
            }
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (poorStage is null)
        {
            yield return $"BranchFacilityDef {defName} has null {nameof(poorStage)}.";
        }
        if (normalStage is null)
        {
            yield return $"BranchFacilityDef {defName} has null {nameof(normalStage)}.";
        }
        if (goodStage is null)
        {
            yield return $"BranchFacilityDef {defName} has null {nameof(goodStage)}.";
        }
        if (excellentStage is null)
        {
            yield return $"BranchFacilityDef {defName} has null {nameof(excellentStage)}.";
        }
    }
}