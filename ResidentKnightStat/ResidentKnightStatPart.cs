using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class ResidentKnightStatPart
{
    /// <summary>
    /// 优先级，数值越大越先计算
    /// </summary>
    private int priority = 500;
    public int Priority => priority;

    public abstract void PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null);
}
