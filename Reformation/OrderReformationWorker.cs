namespace OberoniaAurea.RatkinOrder;

public class OrderReformationWorker(OrderReformationDef def)
{
    public readonly OrderReformationDef Def = def;

    /// <summary>
    /// 仅在添加自新时触发
    /// </summary>
    public virtual void InitActive(RatkinOrder ratkinOrder) { }

    /// <summary>
    ///  添加自新和加载存档时触发
    /// </summary>
    public virtual void PostActive(RatkinOrder ratkinOrder) { }
}