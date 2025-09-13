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

    public static LetterDef OARO_Apprentice_QuizStayIntentionLetter;
    public static LetterDef OARO_KnightGroupProactiveVisitLetter;

    public static RulePackDef OARO_NamerOrderSquad;

    public static RoomRoleDef OARO_RatkinOrderHall;

    public static WorldObjectDef OARO_WO_ApprenticeHome;

    static OARO_ModDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ModDefOf));
    }
}