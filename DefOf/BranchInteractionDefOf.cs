using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchInteractionDefOf
{
    /// <summary>
    /// 要求战备
    /// </summary>
    public static BranchInteractionDef OARO_RequestCombatReadiness;
    /// <summary>
    /// 人员补充
    /// </summary>
    public static BranchInteractionDef OARO_MapRecommendationToKnight;
    /// <summary>
    /// 补充物资
    /// </summary>
    public static BranchInteractionDef OARO_MapSilverToSupply;
    /// <summary>
    /// 解锁支援权限
    /// </summary>
    public static BranchInteractionDef OARO_UnlockSupportAuthority;

    static BranchInteractionDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchInteractionDefOf));
    }
}
