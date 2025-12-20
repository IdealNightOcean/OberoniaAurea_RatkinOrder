using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_RulePackDefOf
{
    /// <summary>
    /// 分部问候语
    /// </summary>
    public static RulePackDef OARO_Maker_BranchGreetingDesc;

    /// <summary>
    /// 分部名称拼装 
    /// </summary>
    public static RulePackDef OARO_NameBuilder_BranchName;
    /// <summary>
    /// 分队名称拼装
    /// </summary>
    public static RulePackDef OARO_NameBuilder_SquadName;
    public static RulePackDef OARO_Dialog_AroundKnightGroupVisitInvalid;
    /// <summary>
    /// 贵族名称
    /// </summary>
    public static RulePackDef OARO_Namer_Nobility;
    /// <summary>
    /// 联合巡逻完成
    /// </summary>
    public static RulePackDef OARO_JointPatrolCompletion;
    static OARO_RulePackDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_RulePackDefOf));
    }
}
