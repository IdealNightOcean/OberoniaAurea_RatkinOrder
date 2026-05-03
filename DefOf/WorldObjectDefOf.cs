using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_WorldObjectDefOf
{
    /// <summary>
    /// 建设中的分部
    /// </summary>
    public static WorldObjectDef OARO_WO_BranchUnderConstruction;
    /// <summary>
    /// 分部（单独设施）
    /// </summary>
    public static WorldObjectDef OARO_WO_BranchSite;

    /// <summary>
    /// 小学徒的家乡
    /// </summary>
    public static WorldObjectDef OARO_WO_ApprenticeHome;
    /// <summary>
    /// 叛乱镇压 - 贵族领地 攻击时的地图
    /// </summary>
    public static WorldObjectDef OARO_Map_NobilityTerritory;

    /// <summary>
    /// 执勤协助交互点
    /// </summary>
    public static WorldObjectDef OARO_WO_JurisdictionDutySite;

    static OARO_WorldObjectDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_WorldObjectDefOf));
    }
}