using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class SpecialLetterDefBase : Def
{
    /// <summary>
    /// 发信人
    /// </summary>
    [MustTranslate]
    public string sender;

    /// <summary>
    /// 信件内容
    /// </summary>
    /// <remarks>- 信件标题使用<see cref="Def.label"/>字段</remarks>
    [MustTranslate]
    public string text;

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
}


public class SpecialLetterDef : SpecialLetterDefBase;