using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士团Def
/// </summary>
public class RatkinOrderDef : Def
{
    /// <summary>
    /// 骑士团固定名称
    /// </summary>
    [MustTranslate]
    public string fixedName;

    /// <summary>骑士团名称生成器</summary>
    /// <remarks>- 只在<see cref="fixedName"/>为<see langword="null"/>或<see cref="string.Empty"/>时起效</remarks>
    public RulePackDef nameMaker;

    /// <summary>
    /// 骑士团分部名称生成器
    /// </summary>
    public RulePackDef branchNameCoreSelecter;

    /// <summary>骑士团颜色</summary>
    /// <remarks>- 若为<see langword="null"/> 则使用 <see cref="Faction"/> 的颜色</remarks>
    public Color? color;

    /// <summary>
    /// 相关人员生成组
    /// </summary>
    public List<PawnGroupOption> pawnGroupOptions;

    public bool TryGetRandomPawnGroupMaker(PawnGroupKindDef pawnGroupKindDef, out PawnGroupOption groupOption)
    {
        if (pawnGroupOptions.NullOrEmpty())
        {
            groupOption = null;
            return false;
        }
        return pawnGroupOptions.Where(g => g.kindDef == pawnGroupKindDef)
                               .TryRandomElementByWeight(g => g.commonality, out groupOption);
    }

}