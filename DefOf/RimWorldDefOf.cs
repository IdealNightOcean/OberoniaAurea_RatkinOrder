using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_RimWorldDefOf
{
    public static ThingDef SculptureSmall;

    static OARO_RimWorldDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_RimWorldDefOf));
    }
}
