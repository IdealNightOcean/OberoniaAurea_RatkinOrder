using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BuildingAdvancedProperties
{
    /// <summary>
    /// 升级后的建筑名称
    /// </summary>
    [MustTranslate]
    public string label;

    /// <summary>
    /// 升级后的建筑描述
    /// </summary>
    [MustTranslate]
    public string description;

    /// <summary>升级人口</summary>
    /// <remarks>- <see cref="BranchPopulationHandler.Population"/>大于等于该值时，<see cref="BranchBuilding"/> 会自动升级</remarks>
    public int advancedPopulation;

    /// <summary>
    /// 效果标志列表
    /// </summary>
    public List<string> effectFlags;

    /// <summary>
    /// 属性修正列表（Offset）
    /// </summary>
    public List<StatModifier<BranchStatDef>> branchStatOffsets;

    /// <summary>
    /// 属性修正列表（Factor）
    /// </summary>
    public List<StatModifier<BranchStatDef>> branchStatFactors;

    /// <summary>
    /// 分部界面问候段落 - 建筑部分
    /// </summary>
    [MustTranslate]
    public string greetingParagraph;
    /// <summary>额外自定义的效果描述</summary>
    /// <remarks>- 显示在修正效果之后</remarks>
    [MustTranslate]
    public List<string> customEffectDescriptions;
}