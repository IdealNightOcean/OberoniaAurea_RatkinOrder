using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class BranchTaskDefOf
{
    public static BranchTaskDef OARO_Squad_JurisdictionDutyPerp;
    public static BranchTaskDef OARO_Squad_GroupPatrolPerp;
    public static BranchTaskDef OARO_Squad_GroupPatrol;

    static BranchTaskDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchTaskDefOf));
    }
}
