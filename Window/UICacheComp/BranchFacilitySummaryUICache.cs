using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilitySummaryUICache
{
    public readonly BranchFacilityDef Def;
    public readonly BranchFacilityLevel Level;

    public readonly BranchFacilityLevelStage CurStage;
    private List<string> curStageEffectDesc;
    private string curStageEffectDescJoint;
    public List<string> CurStageEffectDesc => curStageEffectDesc ??= (CurStage?.GetEffectDescriptions().ToList() ?? []);
    public string CurStageEffectDescJoint => curStageEffectDescJoint ??= JointEffectDesc(CurStageEffectDesc);

    public readonly BranchFacilityLevelStage NextStage;
    private List<string> nextStageEffectDesc;
    private string nextStageEffectDescJoint;
    public List<string> NextStageEffectDesc => nextStageEffectDesc ??= (NextStage?.GetEffectDescriptions().ToList() ?? []);
    public string NextStageEffectDescJoint => nextStageEffectDescJoint ??= JointEffectDesc(NextStageEffectDesc);

    public BranchFacilitySummaryUICache() { }
    public BranchFacilitySummaryUICache(BranchFacilityDef def, BranchFacilityLevel level)
    {
        Def = def ?? throw new ArgumentNullException(nameof(def));
        Level = level;
        CurStage = def.GetLevelStage(level);
        if (level < BranchFacilityLevel.Excellent)
        {
            NextStage = def.GetLevelStage(level.FacilityLevelOffSetBy(1));
        }
    }

    private string JointEffectDesc(List<string> effectDesc)
    {
        if (effectDesc.NullOrEmpty())
        {
            return string.Empty;
        }
        StringBuilder sb = new();
        for (int i = 0; i < effectDesc.Count; i++)
        {
            sb.AppendLine(effectDesc[i]);
        }
        return sb.ToString();
    }
}