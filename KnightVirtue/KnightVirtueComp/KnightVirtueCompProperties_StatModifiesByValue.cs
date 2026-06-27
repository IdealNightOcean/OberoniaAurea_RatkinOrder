using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueCompProperties_StatModifiesByValue : KnightVirtueCompProperties
{
    /// <summary>
    /// 根据美德属性值调整的属性Offset
    /// </summary>
    public List<StatModifierBySeverity> statOffsetsByValue;

    /// <summary>
    /// 根据美德属性值调整的属性Factor
    /// </summary>  
    public List<StatModifierBySeverity> statFactorsByValue;
}