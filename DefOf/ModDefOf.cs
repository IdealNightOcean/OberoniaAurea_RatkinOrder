using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ModDefOf
{
    public static BackstoryDef Ratkin_Knight;
    public static BackstoryDef Ratkin_KnightCommander;

    public static BranchContractDef OARO_Contract_Silver;

    /// <summary>
    /// 防卫设施
    /// </summary>
    public static BranchFacilityDef OARO_DefensiveFacility;
    /// <summary>
    /// 支援设施
    /// </summary>
    public static BranchFacilityDef OARO_SupportFacility;

    public static FactionDef Rakinia;
    public static FactionDef OARO_SubRakinia_Neutral;

    /// <summary>
    /// 旅鼠派系
    /// </summary>
    [MayRequire("OARK.RatkinFaction.GeneExpand")]
    public static FactionDef Rakinia_TravelRatkin;
    /// <summary>
    /// 岩鼠派系
    /// </summary>
    [MayRequire("OARK.RatkinFaction.GeneExpand")]
    public static FactionDef Rakinia_RockRatkin;

    /// <summary>
    /// 骑士团交互 - 斡旋关系
    /// </summary>
    public static HistoryEventDef OARO_OrderMediateFactionRelation;

    /// <summary>
    /// 分部建筑 - 金鸢尾兰洽谈所
    /// </summary>
    [MayRequire("OARK.RatkinFaction.OberoniaAurea")]
    public static HistoryEventDef OARK_OberoniaConferenceHall;

    /// <summary>
    /// 荣誉分部 - 教导骑士
    /// </summary>
    public static BranchHonorDef OARO_Honor_Instructor;
    /// <summary>
    /// 荣誉分部 - 律令骑士
    /// </summary>
    public static BranchHonorDef OARO_Honor_LawOrder;

    /// <summary>
    /// 叛乱镇压 - 贵族领地战斗
    /// </summary>
    public static IncidentDef OARO_RaidNobilityTerritory;

    /// <summary>
    /// 丢东西的旅行商人
    /// </summary>
    public static IsolatedPawnGroupMakerDef OARO_LostItemsOfTrader;

    /// <summary>
    /// 骑士团总览
    /// </summary>
    public static MainButtonDef OARO_KnightOrdersOverview;

    public static RoomRoleDef OARO_RatkinOrderStation;

    /// <summary>
    /// 建筑商商店
    /// </summary>
    public static TraderKindDef OARO_TownConstruction_Trader;

    /// <summary>
    /// 驻地文书
    /// </summary>
    public static ResidentKnightRoleDef OARO_Clerk;
    /// <summary>
    /// 地区看护
    /// </summary>
    public static ResidentKnightRoleDef OARO_Orderly;

    /// <summary>
    /// 每日修行点获得
    /// </summary>
    public static StatDef OARO_Stat_MeditationDailyGain;
    /// <summary>
    /// 每日修行点获得系数
    /// </summary>
    public static StatDef OARO_Stat_MeditationFactor;
    /// <summary>
    /// 每日修行点获得基础
    /// </summary>
    public static StatDef OARO_Stat_MeditationBase;
    /// <summary>
    /// 美德
    /// </summary>
    public static StatDef OARO_Stat_PawnVirtue;

    public static TraitDef OARO_OrderKnight;

    /// <summary>
    /// 骑士个性 - 誓言
    /// </summary>
    public static KnightChivalryDef OARO_Oath;

    static OARO_ModDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ModDefOf));
    }
}