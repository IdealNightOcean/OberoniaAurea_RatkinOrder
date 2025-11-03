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
}