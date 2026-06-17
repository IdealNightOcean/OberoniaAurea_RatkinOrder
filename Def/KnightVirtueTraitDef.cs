using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士美德词条Def
/// </summary>
public class KnightVirtueTraitDef : Def
{
    public List<StatModifier> statOffsets;

    public List<StatModifier> statFactors;

    /// <summary>
    /// 根据美德属性值调整的属性Offset
    /// </summary>
    public List<StatModifierBySeverity> statOffsetsByVirtue;

    /// <summary>
    /// 根据美德属性值调整的属性Factor
    /// </summary>  
    public List<StatModifierBySeverity> statFactorsByVirtue;

}