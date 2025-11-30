using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class OARO_QuestScriptDefOf
{
    public static QuestScriptDef OARO_MercyPre_HelpSeeker;

    public static QuestScriptDef OARO_Mercy_ApplianceRepair;
    public static QuestScriptDef OARO_Mercy_PastureFlu;
    public static QuestScriptDef OARO_Mercy_TaxCollectorTreat;

    public static QuestScriptDef OARO_Quest_TemporaryEncampment;

    public static QuestScriptDef OARO_Quest_KnightsVisit;
    public static QuestScriptDef OARO_Quest_ResidentKnight;

    public static QuestScriptDef OARO_Quest_OrderRelationshipUpgrade;

    public static QuestScriptDef OARO_Quest_ResidentKnightBackPlayer; //常驻骑士回归玩家

    public static QuestScriptDef OARO_Quest_JointPatrolCaravanHelp; //联合巡逻 - 分部求助

    static OARO_QuestScriptDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_QuestScriptDefOf));
    }
}