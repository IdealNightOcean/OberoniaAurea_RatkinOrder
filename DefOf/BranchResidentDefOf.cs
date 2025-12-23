using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchResidentDefOf
{
    // public static BranchResidentDef OARO_Deployment;

    /// <summary>
    /// 医疗援助
    /// </summary>
    public static BranchResidentDef OARO_CaravanMedicalAssistance;

    static BranchResidentDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchResidentDefOf));
    }
}
