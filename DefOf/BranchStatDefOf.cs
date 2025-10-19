using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchStatDefOf
{
    public static BranchStatDef OARO_AffectRadius;

    public static BranchStatDef OARO_BuildingCeiling;
    public static BranchStatDef OARO_DeployeeDailyXp;

    public static BranchStatDef OARO_SupplyCeiling; //分部补给容量上限
    public static BranchStatDef OARO_SupplyRecoveryRate; //分部补给恢复速率

    public static BranchStatDef OARO_DailyPopulationGrowth; // 每日人口增长
    public static BranchStatDef OARO_NaturalPopulationCeiling; // 自然人口上限

    public static BranchStatDef OARO_ConstructionCostFactor; //建设白银花费系数
    public static BranchStatDef OARO_ConstructionSpeedFactor; //建设速度系数

    public static BranchStatDef OARO_BombardSupportCount;

    public static BranchStatDef OARO_SquadMemberCeiling;
    public static BranchStatDef OARO_SquadCommanderCeiling;

    public static BranchStatDef OARO_SquadMemberRecoveryRate;

    static BranchStatDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchStatDefOf));
    }

}
