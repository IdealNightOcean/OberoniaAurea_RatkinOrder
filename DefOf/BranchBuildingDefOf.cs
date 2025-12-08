using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchBuildingDefOf
{
    /// <summary>
    /// 分部教堂（修女小屋）
    /// </summary>
    public static BranchBuildingDef OARO_Church;
    /// <summary>
    /// 骑士长办公室
    /// </summary>
    public static BranchBuildingDef OARO_CommanderOffice;
    /// <summary>
    /// 建筑师办公室
    /// </summary>
    public static BranchBuildingDef OARO_ArchitectOffice;

    static BranchBuildingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchBuildingDefOf));
    }
}
