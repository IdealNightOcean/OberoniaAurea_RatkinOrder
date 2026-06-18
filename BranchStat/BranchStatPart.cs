using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchStatPart
{
    protected const string ExplanatCap = "    ";

    /// <summary>
    /// 优先级，数值越大越先计算
    /// </summary>
    private int priority = 500;
    public int Priority => priority;
    public abstract void PostTransform(Branch branch, ref float curValue);

    public abstract void ModifyExplanation(Branch branch, BranchStatDef statDef, StringBuilder explanation);

}