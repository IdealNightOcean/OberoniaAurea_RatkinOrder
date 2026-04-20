using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部印记Def
/// </summary>
public class BranchMedalDef : Def
{
    /// <summary>印记专注任务类型</summary>
    /// <remarks>- 会在 <see cref="Branch"/> 初始化时设置 <see cref="BranchTaskHandler.FocusedTaskType"/></remarks>
    public BranchTaskType focusedTaskType;

    /// <summary>
    /// 对应骑士精神大类
    /// </summary>
    public KnightChivalryDef chivalry;

    /// <summary>
    /// 印记颜色
    /// </summary>
    public Color color;

    [MustTranslate]
    public string effectDescription;

    public List<StatModifierBySeverity> statOffsetsByCount;
    public List<StatModifierBySeverity> statFactorsByCount;

    /// <summary>
    /// 印记背景颜色
    /// </summary>
    public Color backgroundColor;
    private Texture2D backgroundTexture;
    /// <summary>
    /// 印记背景图标，颜色使用 <see cref="backgroundColor"/>
    /// </summary>
    public Texture2D BackgroundTexture => backgroundTexture ??= SolidColorMaterials.NewSolidColorTexture(backgroundColor);


    /// <summary>
    /// 印记图标
    /// </summary>
    public PathedTexture2D iconTexture;

    /// <summary>
    /// 主要印记图标
    /// </summary>
    public PathedTexture2D primaryIconTexture;

    /// <summary>
    /// 荣誉装饰框图标
    /// </summary>
    public PathedTexture2DWithExpanded honorDecorationTexture;

    /// <summary>
    /// 荣誉绶带图标
    /// </summary>
    public PathedTexture2D honorRibbonTexture;

    /// <summary>
    /// 荣誉背景图标
    /// </summary>
    public PathedTexture2D honorBackgroundTexture;

    public PathedTexture2D jointPatrolEntryBackgroundTexture;

    public PathedTexture2D jointPatrolEntryShadeTexture;

}