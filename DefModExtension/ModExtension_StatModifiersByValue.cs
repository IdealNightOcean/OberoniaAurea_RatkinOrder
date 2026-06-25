using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ModExtension_StatModifiersByValue : DefModExtension
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
