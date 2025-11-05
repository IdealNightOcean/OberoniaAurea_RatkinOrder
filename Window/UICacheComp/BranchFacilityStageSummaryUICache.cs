using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityStageSummaryUICache
{
    public readonly BranchFacilityDef Def;
    public readonly BranchFacilityLevel Level;

    public readonly BranchFacilityLevelStage Stage;
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
                    sb.AppendLine(stageEffectDesc[i]);
                }
                stageEffectDescJoint = sb.ToString();
            }
            return stageEffectDescJoint;
        }
    }

    public BranchFacilityStageSummaryUICache() { }
    public BranchFacilityStageSummaryUICache(BranchFacilityDef def, BranchFacilityLevel level)
    {
        Def = def ?? throw new ArgumentNullException(nameof(def));
        Level = level;
        Stage = def.GetLevelStage(level);
    }
}