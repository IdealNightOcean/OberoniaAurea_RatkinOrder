using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityDef : Def
{
    private readonly static BranchFacilityLevel[] AllStageLevels = (BranchFacilityLevel[])Enum.GetValues(typeof(BranchFacilityLevel));
    private readonly static int LevelCount = AllStageLevels.Length;

    public int silverCost;
    public int constructionDays;
    public List<BranchFacilityLevelStage> levelStages = [];

    public int GetLevelStageIndex(BranchFacilityLevel level)
    {
        for (int i = 0; i < levelStages.Count; i++)
        {
            if (levelStages[i].level == level)
            {
                return i;
            }
        }
        return -1;
    }

    public override void PostLoad()
    {
        base.PostLoad();
        if (levelStages.NullOrEmpty())
        {
            Log.Error($"BranchFacilityDef {defName} has no level stages defined.");
            return;
        }

        List<BranchFacilityLevelStage> uniqueLevelStages = [];
        HashSet<BranchFacilityLevel> definedLevels = [];

        bool hasNoneLevel = false;
        bool hasDuplicateLevel = false;
        string duplicateLevels = string.Empty;

        foreach (BranchFacilityLevelStage stage in levelStages)
        {
            if (stage.level == BranchFacilityLevel.None)
            {
                hasNoneLevel = true;
                continue;
            }
            if (!definedLevels.Add(stage.level))
            {
                duplicateLevels += ($"{stage.level}, ");
                hasDuplicateLevel = true;
                continue;
            }
            uniqueLevelStages.Add(stage);
        }

        levelStages = uniqueLevelStages;
        levelStages.SortBy(s => (int)s.level);

        if (hasNoneLevel)
        {
            Log.Warning($"BranchFacilityDef {defName} has a stage defined for level None, which is not allowed. Removed.");
        }
        if (hasDuplicateLevel)
        {
            Log.Error($"BranchFacilityDef {defName} has duplicate level stages defined for: {duplicateLevels.TrimEnd(',', ' ')}.\n Only first one will be used.");
        }

        if (levelStages.Count < LevelCount - 1)
        {
            string missingLevels = string.Join(", ", AllStageLevels
                                         .Where(level => level != BranchFacilityLevel.None && !definedLevels.Contains(level))
                                         .Select(level => level.ToString()));

            Log.Error($"BranchFacilityDef {defName} is missing level stages for: {missingLevels}.");
        }
    }
}