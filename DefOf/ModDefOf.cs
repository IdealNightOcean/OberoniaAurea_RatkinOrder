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

    public static RulePackDef OARO_NamerOrderSquad;

    public static RoomRoleDef OARO_RatkinOrderHall;

    public static WorldObjectDef OARO_WO_ApprenticeHome;

    public static ResidentKnightRoleDef OARO_Orderly;

    static OARO_ModDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ModDefOf));
    }
}