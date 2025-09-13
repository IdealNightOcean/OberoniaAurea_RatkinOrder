using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ThoughtDefOf
{
    public static ThoughtDef OARO_Thought_ChildrenCare;
    public static ThoughtDef OARO_Thought_CelebrationHost;
    public static ThoughtDef OARO_Thought_FamineVillagetFeast;
    public static ThoughtDef OARO_Thought_VisitingKnight;

    static OARO_ThoughtDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ThoughtDefOf));
    }
}
