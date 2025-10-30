using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OrderReformationDefOf
{
    public static OrderReformationDef OARO_ComprehensiveJointPatrolPreparation; //全面联巡战备
    public static OrderReformationDef OARO_ExplorationTraining; //勘探训练

    public static OrderReformationDef OARO_ReformationPlaceholder; //未实现Def自新占位

    static OrderReformationDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OrderReformationDefOf));
    }
}
