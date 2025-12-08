using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderReformationDefOf
{
    /// <summary>
    /// 全面联巡战备
    /// </summary>
    public static OrderReformationDef OARO_ComprehensiveJointPatrolPreparation;
    /// <summary>
    /// 勘探训练
    /// </summary>
    public static OrderReformationDef OARO_ExplorationTraining;

    /// <summary>
    /// 未实现Def自新占位
    /// </summary>
    public static OrderReformationDef OARO_ReformationPlaceholder;

    static OrderReformationDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderReformationDefOf));
    }
}
