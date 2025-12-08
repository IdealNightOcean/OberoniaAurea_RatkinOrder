using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchMedalDefOf
{
    /// <summary>
    /// 勇气印记
    /// </summary>
    public static BranchMedalDef OARO_Courage;
    /// <summary>
    /// 简易印记
    /// </summary>
    public static BranchMedalDef OARO_Tenacity;
    /// <summary>
    /// 援护印记
    /// </summary>
    public static BranchMedalDef OARO_Rescue;
    /// <summary>
    /// 公义印记
    /// </summary>
    public static BranchMedalDef OARO_Justice;

    static BranchMedalDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchMedalDefOf));
    }
}