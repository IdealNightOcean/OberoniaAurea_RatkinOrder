using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class BranchTaskDefOf
{
    public static BranchTaskDef OARO_JurisdictionDutyPerp;

    static BranchTaskDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchTaskDefOf));
    }
}