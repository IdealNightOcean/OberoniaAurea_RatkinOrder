using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团信件Def
/// </summary>
public class OrderLetterDef : Def
{
    private static readonly Type defaultLetterClass = typeof(OrderLetter);

    /// <summary>
    /// 信件功能类
    /// </summary>
    public Type letterClass = defaultLetterClass;

    /// <summary>
    /// 信件类型
    /// </summary>
    public OrderLetterType letterType;

    /// <summary>
    /// 能否被转化为原版信件
    /// </summary>
    public bool canShowAsRimLetter = true;

    /// <summary>强制转化为原版信件</summary>
    /// <remarks>- 只在 <see cref="canShowAsRimLetter"/> 为 <see langword="true"/> 时生效</remarks>
    public bool forceShowAsRimLetter;

    /// <summary>相关原版信件Def</summary>
    /// <remarks>- 只在 <see cref="canShowAsRimLetter"/> 为 <see langword="true"/> 时生效</remarks>
    public LetterDef relatedLetterDef;


    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (letterClass is null)
        {
            letterClass = defaultLetterClass;
            yield return $"'{nameof(letterClass)}' 为 null，已设置为默认值。";
        }
        if (!canShowAsRimLetter && forceShowAsRimLetter)
        {
            forceShowAsRimLetter = false;
            yield return $"不能设置 '{nameof(forceShowAsRimLetter)}' 为 true，因为 '{nameof(canShowAsRimLetter)}' 为 false。";
        }
    }
}
