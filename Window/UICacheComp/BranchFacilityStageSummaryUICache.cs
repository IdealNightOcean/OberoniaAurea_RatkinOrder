using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityStageSummaryUICache
{
    public BranchFacilityDef Def { get; }
    public BranchFacilityLevel Level { get; }
    public BranchFacilityLevelStage Stage { get; }
    public int SilverCost { get; }
    public int TimeCost { get; }

    private string silverCostExplanation;

    private List<string> stageEffectDesc;
    private string stageEffectDescJoint;
    public List<string> StageEffectDesc => stageEffectDesc ??= (Stage?.GetEffectDescriptions().ToList() ?? []);
    public string StageEffectDescJoint
    {
        get
        {
            if (stageEffectDescJoint is null)
            {
                if (StageEffectDesc.NullOrEmpty())
                {
                    stageEffectDescJoint = string.Empty;
                }
                StringBuilder sb = new();
                for (int i = 0; i < stageEffectDesc.Count; i++)
                {
                    sb.Append("- ");
                    sb.AppendLine(stageEffectDesc[i]);
                }
                stageEffectDescJoint = sb.ToString();
            }
            return stageEffectDescJoint;
        }
    }


    public BranchFacilityStageSummaryUICache() { }
    public BranchFacilityStageSummaryUICache(BranchFacilityDef def, BranchFacilityLevel level, Branch branch)
    {
        Def = def ?? throw new ArgumentNullException(nameof(def));
        Level = level;
        Stage = def.GetLevelStage(level);
        SilverCost = branch.GetFacilitySilverCost(def, level, resultOnly: true, out _);
        TimeCost = branch.GetFacilityTimeCost(def, level);
    }

    public string GetSilverCostExplanation(Branch branch)
    {
        if (silverCostExplanation is null)
        {
            if (branch is null)
            {
                silverCostExplanation = string.Empty;
            }
            else
            {
                branch.GetFacilitySilverCost(Def, Level, resultOnly: false, out silverCostExplanation);
            }

        }
        return silverCostExplanation;
    }
}