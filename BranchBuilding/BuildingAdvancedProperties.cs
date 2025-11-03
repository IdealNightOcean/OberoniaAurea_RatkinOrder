using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BuildingAdvancedProperties
{
    [MustTranslate]
    public string label;
    [MustTranslate]
    public string extraDescription;

    public int advancedPopulation;

    public List<string> effectFlags; //效果标志列表
    public List<BranchStatModifier> branchStatOffsets; //属性修正列表（Offset）
    public List<BranchStatModifier> branchStatFactors; //属性修正列表（Factor）

    [MustTranslate]
    public List<string> customEffectDescriptions;
}