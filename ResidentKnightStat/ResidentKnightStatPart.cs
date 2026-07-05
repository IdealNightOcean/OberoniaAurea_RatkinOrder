using System.Text;

namespace OberoniaAurea.RatkinOrder;

public abstract class ResidentKnightStatPart
{
    protected const string ExplanatCap = "    ";

    /// <summary>
    /// 优先级，数值越大越先计算
    /// </summary>
    private int priority = 500;
    public int Priority => priority;

    public abstract void PostTransModify(ResidentKnightStatRequestData requestData, ref float curValue);

    public abstract void PostTransModifyExplanation(ResidentKnightStatRequestData requestData, ResidentKnightStatDef statDef, StringBuilder explanation);

}
