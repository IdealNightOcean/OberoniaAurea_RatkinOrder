using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchStatDefOf
{
    /// <summary>
    /// 分部影响范围
    /// </summary>
    public static BranchStatDef OARO_AffectRadius;

    /// <summary>
    /// 分部普通建筑上限
    /// </summary>
    public static BranchStatDef OARO_BuildingCeiling;
    public static BranchStatDef OARO_DeployeeDailyXpFactor;

    /// <summary>
    /// 分部补给容量上限
    /// </summary>
    public static BranchStatDef OARO_SupplyCeiling;
    /// <summary>
    /// 分部补给恢复速率
    /// </summary>
    public static BranchStatDef OARO_SupplyRecoveryRate;

    /// <summary>
    /// 每日人口增长
    /// </summary>
    public static BranchStatDef OARO_DailyPopulationGrowth;
    /// <summary>
    /// 自然人口上限
    /// </summary>
    public static BranchStatDef OARO_NaturalPopulationCeiling;

    /// <summary>
    /// 建设白银花费系数
    /// </summary>
    public static BranchStatDef OARO_ConstructionCostFactor;
    /// <summary>
    /// 建设速度系数
    /// </summary>
    public static BranchStatDef OARO_ConstructionSpeedFactor;

    /// <summary>
    /// 每轮炮击支援数量
    /// </summary>
    public static BranchStatDef OARO_BombardSupportCeiling;

    /// <summary>
    /// 分队普通成员上限
    /// </summary>
    public static BranchStatDef OARO_SquadMemberCeiling;
    /// <summary>
    /// 分队骑士长上限
    /// </summary>
    public static BranchStatDef OARO_SquadCommanderCeiling;
    /// <summary>
    /// 分队成员恢复率
    /// </summary>
    public static BranchStatDef OARO_SquadMemberRecoveryRate;

    static BranchStatDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchStatDefOf));
    }

}
