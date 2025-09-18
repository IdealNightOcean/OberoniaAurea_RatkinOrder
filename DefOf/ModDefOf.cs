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

    public static HistoryEventDef OARO_OrderMediateFactionRelation;

    public static RulePackDef OARO_NamerOrderSquad;

    public static RoomRoleDef OARO_RatkinOrderHall;

    public static WorldObjectDef OARO_WO_ApprenticeHome;

    public static ResidentKnightRoleDef OARO_Clerk; //驻地文书
    public static ResidentKnightRoleDef OARO_Orderly; //地区看护

    public static TraitDef OARO_OrderKnight;


    static OARO_ModDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ModDefOf));
    }
}