using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class BranchTaskDefOf
{
    public static BranchTaskDef OARO_JurisdictionDutyPrep;
    public static BranchTaskDef OARO_CombatReadiness; //战备

    static BranchTaskDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchTaskDefOf));
    }
}