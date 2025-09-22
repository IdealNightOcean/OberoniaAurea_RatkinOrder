using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchStatDefOf
{
    public static BranchStatDef OARO_AffectRadius;
    public static BranchStatDef OARO_NaturalPopulationCeiling; // 自然人口上限
    public static BranchStatDef OARO_BuildingCeiling;
    public static BranchStatDef OARO_DeployeeDailyXp;

    public static BranchStatDef OARO_BuildingCost;
    public static BranchStatDef OARO_FacilityCost;

    public static BranchStatDef OARO_BombardSupportCount;

    public static BranchStatDef OARO_SquadMemberCeiling;
    public static BranchStatDef OARO_SquadCommanderCeiling;
    public static BranchStatDef OARO_SquadSupplyCeiling;

    public static BranchStatDef OARO_SquadMemberRecoveryRate;
    public static BranchStatDef OARO_SquadSupplyRecoveryRate;

    static BranchStatDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchStatDefOf));
    }

}
