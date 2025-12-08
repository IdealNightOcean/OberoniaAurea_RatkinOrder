using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class BranchTaskDefOf
{
    public static BranchTaskDef OARO_JurisdictionDutyPrep;

    /// <summary>
    /// 战备
    /// </summary>
    public static BranchTaskDef OARO_CombatReadiness;

    static BranchTaskDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchTaskDefOf));
    }
}