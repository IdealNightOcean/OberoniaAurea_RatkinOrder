namespace OberoniaAurea.RatkinOrder;

public abstract class BranchStatPart
{
    public int priority = 100; //优先级，数值越大越优先
    public abstract float PostTransform(Branch branch, float curValue);
}