using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchInteractionDefOf
{
    public static BranchInteractionDef OARO_RequestCombatReadiness; //要求战备
    public static BranchInteractionDef OARO_MapRecommendationToKnight; //人员补充
    public static BranchInteractionDef OARO_MapSilverToSupply; //补充物资
    public static BranchInteractionDef OARO_UnlockSupportAuthority; //解锁支援权限

    static BranchInteractionDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchInteractionDefOf));
    }
}
