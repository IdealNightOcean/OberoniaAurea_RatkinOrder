using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class BranchBuildingDefOf
{
    public static BranchBuildingDef OARO_Church; //分部教堂（修女小屋）
    public static BranchBuildingDef OARO_CommanderOffice; //骑士长办公室
    public static BranchBuildingDef OARO_ArchitectOffice; //建筑师办公室

    static BranchBuildingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BranchBuildingDefOf));
    }
}
