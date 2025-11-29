using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_WorldObjectDefOf
{
    public static WorldObjectDef OARO_WO_BranchUnderConstruction; //建设中的分部
    public static WorldObjectDef OARO_WO_BranchSite; //分部（单独设施）

    public static WorldObjectDef OARO_WO_ApprenticeHome; //小学徒的家乡
    public static WorldObjectDef OARO_Map_NobilityTerritory; //叛乱镇压 - 贵族领地 攻击时的地图

    static OARO_WorldObjectDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_WorldObjectDefOf));
    }
}