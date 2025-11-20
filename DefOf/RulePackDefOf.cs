using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_RulePackDefOf
{
    public static RulePackDef OARO_NameBuilder_BranchName; //分部名称拼装 
    public static RulePackDef OARO_NameBuilder_SquadName; //分队名称拼装
    public static RulePackDef OARO_Dialog_AroundKnightGroupVisitInvalid;
    public static RulePackDef OARO_Namer_Nobility; //贵族名称
    public static RulePackDef OARO_JointPatrolCompletion; //联合巡逻完成
    static OARO_RulePackDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_RulePackDefOf));
    }
}
