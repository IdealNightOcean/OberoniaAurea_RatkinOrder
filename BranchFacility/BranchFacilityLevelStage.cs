using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

// xml相关
public class BranchFacilityLevelStage
{
    /// <summary>
    /// 建设白银花费（基础）
    /// </summary>
    public int silverCost;

    /// <summary> 
    /// 建设所需天数（基础）
    /// </summary>
    public int constructionDays;

    /// <summary>
    /// 效果标志列表
    /// </summary>
    public List<string> effectFlags;

    /// <summary>
    /// 属性修正列表（Offset）
    /// </summary>
    public List<BranchStatModifier> branchStatOffsets;

    /// <summary>
    /// 属性修正列表（Factor）
    /// </summary>
    public List<BranchStatModifier> branchStatFactors;

    /// <summary>额外自定义的效果描述</summary>
    /// <remarks>- 显示在修正效果之后</remarks>
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