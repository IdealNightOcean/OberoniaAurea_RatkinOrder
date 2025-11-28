using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderFundEventDef : Def
{
    /// <summary>
    /// 资金变化量，(-1f~1f)范围外的数值会被归正
    /// </summary>
    public FloatRange changeRange = FloatRange.Zero;

    /// <summary>
    /// 是否为一次性事件
    /// </summary>
    public bool immediately;

    /// <summary>
    /// 持续时间（Day）
    /// </summary>
    /// <remarks>- 若 <see cref="immediately"/> 为 <see langword="true"/>，该字段无效</remarks>
    public int durationDays;
    public bool OnceEvent => immediately || durationDays <= 1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (immediately && durationDays > 0)
        {
            durationDays = 0;
            yield return "has both an immediately true flag and a positive durationDays value at the same time. Set durationDays to 0.";
        }
    }
}
