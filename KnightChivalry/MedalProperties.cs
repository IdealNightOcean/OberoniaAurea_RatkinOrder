using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部印记属性类
/// </summary>
public class MedalProperties
{
    [MustTranslate]
    public string medalLabel;

    public string MedalLabelCap => medalLabel.CapitalizeFirst();

    [MustTranslate]
    public string medalDescription;

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
    /// 荣誉装饰框图标
    /// </summary>
    public PathedTexture2DWithExpanded honorDecorationTexture;

}