using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ThoughtDefOf
{
    public static ThoughtDef OARO_Thought_ChildrenCare;
    public static ThoughtDef OARO_Thought_VisitingKnight;

    /// <summary>
    /// 常驻骑士 - 自己骑士团有分部被袭击
    /// </summary>
    public static ThoughtDef OARO_Thought_ResidentKnight_SquadBeAttackedOnTask;

    /// <summary>
    /// 风景区巡逻
    /// </summary>
    public static ThoughtDef OARO_Thought_TouristAreaPatrol;

    /// <summary>
    /// 拖延征收官 - 强颜欢笑
    /// </summary>
    public static ThoughtDef OARO_Thought_TaxTreatment;

    /// <summary>
    /// 联巡正面事件心情
    /// </summary>
    public static ThoughtDef OARO_Thought_JointPatrolPositive;
    /// <summary>
    /// 联巡负面事件心情
    /// </summary>
    public static ThoughtDef OARO_Thought_JointPatrolNegative;
    /// <summary>
    ///联巡灾难事件心情
    /// </summary>
    public static ThoughtDef OARO_Thought_JointPatrolDisaster;

    /// <summary>
    /// 美德社交意见修改器
    /// </summary>
    public static ThoughtDef OARO_Thought_VirtueSocialOpinion;

    static OARO_ThoughtDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ThoughtDefOf));
    }
}
