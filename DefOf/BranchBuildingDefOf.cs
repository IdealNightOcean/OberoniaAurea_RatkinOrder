using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchBuildingDefOf
{
    public static BranchBuildingDef OARO_GuardMemorial;
    public static BranchBuildingDef OARO_PioneerMemorial;
    public static BranchBuildingDef OARO_InterveneMemorial;
    public static BranchBuildingDef OARO_LoyalMemorial;
    public static BranchBuildingDef OARO_GloryMemorial;

    static BranchBuildingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchBuildingDefOf));
    }
}
