namespace OberoniaAurea.RatkinOrder;

public class OrderReformationWorker(OrderReformationDef def)
{
    public readonly OrderReformationDef Def = def;

    public virtual void PostAdd() { }

    public virtual void PostInit() { }
}