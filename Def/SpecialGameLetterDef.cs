using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 特殊游戏信件Def基类
/// </summary>
/// <remarks>- <see cref="DailyOrderLetterDef"/>、<see cref="SpecialGameLetterDef"/> 和 <see cref="CertainDateLetterDef"/> 的基类</remarks>
public abstract class SpecialLetterDefBase : Def
{
    [MustTranslate]
    public string labelOverride;
    /// <summary>
    /// 信件内容
    /// </summary>
    [MustTranslate]
    public string text;
    /// <summary>
    /// 发信人
    /// </summary>
    [MustTranslate]
    public string sender;

    /// <summary>
    /// 相关骑士团信件Def
    /// </summary>
    public OrderLetterDef relatedOrderLetterDef;

    /// <summary>
    /// 相关骑士团信件类型
    /// </summary>
    public OrderLetter.RelatedLetterType relatedLetterType;

    /// <summary>
    /// 是否全局唯一
    /// </summary>
    public bool absolutelyUnique;

    public List<ThingDefCountClass> attachments;
}


public class DailyOrderLetterDef : SpecialLetterDefBase;

public class SpecialGameLetterDef : SpecialLetterDefBase;