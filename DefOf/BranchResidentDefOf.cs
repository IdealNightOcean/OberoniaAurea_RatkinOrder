using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchResidentDefOf
{
    public static BranchResidentDef OARO_Deployment;

    static BranchResidentDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchResidentDefOf));
    }
}
