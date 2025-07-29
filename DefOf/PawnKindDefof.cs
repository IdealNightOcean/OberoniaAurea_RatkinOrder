using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public class OARO_PawnKindDefOf
{
    public static PawnKindDef OARO_RatkinVillageChild;

    public static PawnKindDef RatkinColonist;
    public static PawnKindDef RatkinNoble;
    public static PawnKindDef RatkinDefender;

    public static PawnKindDef RatkinKnight;
    public static PawnKindDef RatkinKnightCommander;

    static OARO_PawnKindDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_PawnKindDefOf));
    }
}
