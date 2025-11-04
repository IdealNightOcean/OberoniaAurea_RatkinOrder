using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

// xml相关
public class BranchFacilityLevelStage
{
    public int silverCost;
    public int constructionDays;

    public List<string> effectFlags;
    public List<BranchStatModifier> branchStatOffsets; //属性修正列表（Offset）
    public List<BranchStatModifier> branchStatFactors; //属性修正列表（Factor）

    [MustTranslate]
    public List<string> customEffectDescriptions;

    public IEnumerable<string> GetEffectDescriptions()
    {
        if (branchStatOffsets is not null)
        {
            foreach (BranchStatModifier modifier in branchStatOffsets)
            {
                yield return modifier.ToStringOffset();
            }
        }

        if (branchStatFactors is not null)
        {
            foreach (BranchStatModifier modifier in branchStatFactors)
            {
                yield return modifier.ToStringFactor();
            }
        }

        if (customEffectDescriptions is not null)
        {
            foreach (string desc in customEffectDescriptions)
            {
                yield return desc;
            }

        }
    }
}