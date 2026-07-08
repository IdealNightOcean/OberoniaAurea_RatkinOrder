using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class StatPart<T, TDef, TTarget>
    where TDef : OAROStatDefBase
    where T : StatRequestData<TDef, TTarget>
{
    /// <summary>
    /// 优先级，数值越大越先计算
    /// </summary>
    private int priority = 500;
    public int Priority => priority;

    public virtual bool PostTransModify(T requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    { return false; }
}
