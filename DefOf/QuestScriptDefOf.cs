using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class OARO_QuestScriptDefOf
{
    /// <summary>
    /// 善行任务前置 - 寻求帮助
    /// </summary>
    public static QuestScriptDef OARO_MercyPre_HelpSeeker;

    public static QuestScriptDef OARO_Mercy_TaxCollectorTreat;

    public static QuestScriptDef OARO_Quest_TemporaryEncampment;

    public static QuestScriptDef OARO_Quest_KnightsVisit;
    public static QuestScriptDef OARO_Quest_ResidentKnight;

    public static QuestScriptDef OARO_Quest_OrderRelationshipUpgrade;

    /// <summary>
    /// 常驻骑士回归玩家
    /// </summary>
    public static QuestScriptDef OARO_Quest_ResidentKnightBackPlayer;

    /// <summary>
    /// 联合巡逻 - 分部求助
    /// </summary>
    public static QuestScriptDef OARO_Quest_JointPatrolCaravanHelp;

    static OARO_QuestScriptDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_QuestScriptDefOf));
    }
}