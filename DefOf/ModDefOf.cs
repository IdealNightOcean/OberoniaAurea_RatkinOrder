using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ModDefOf
{
    public static BackstoryDef Ratkin_Knight;
    public static BackstoryDef Ratkin_KnightCommander;

    public static FactionDef Rakinia;
    public static FactionDef OARO_Rakinia_Sub;

    [MayRequire("OARK.RatkinFaction.GeneExpand")]
    public static FactionDef Rakinia_TravelRatkin; //旅鼠派系

    public static HistoryEventDef OARO_OrderMediateFactionRelation;

    public static IsolatedPawnGroupMakerDef OARO_LostItemsOfTrader; //丢东西的旅行商人

    public static RulePackDef OARO_Dialog_AroundKnightGroupVisitInvalid;
    public static RulePackDef OARO_Namer_Nobility; //贵族名称

    public static RoomRoleDef OARO_RatkinOrderHall;

    public static WorldObjectDef OARO_WO_ApprenticeHome;
    public static WorldObjectDef OARO_Map_NobilityTerritory; //叛乱镇压 - 贵族领地 攻击时的地图

    public static ResidentKnightRoleDef OARO_Clerk; //驻地文书
    public static ResidentKnightRoleDef OARO_Orderly; //地区看护

    public static TraitDef OARO_OrderKnight;


    static OARO_ModDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ModDefOf));
    }
}