using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class SquadTaskDefOf
{
    public static SquadTaskDef OARO_Squad_JurisdictionDutyPerp;
    public static SquadTaskDef OARO_Squad_GroupPatrolPerp;
    public static SquadTaskDef OARO_Squad_GroupPatrol;

    static SquadTaskDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(SquadTaskDefOf));
    }
}
