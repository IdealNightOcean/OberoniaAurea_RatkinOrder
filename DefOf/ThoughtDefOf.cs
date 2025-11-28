using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_ThoughtDefOf
{
    public static ThoughtDef OARO_Thought_ChildrenCare;
    public static ThoughtDef OARO_Thought_CelebrationHost;
    public static ThoughtDef OARO_Thought_FamineVillagetFeast;
    public static ThoughtDef OARO_Thought_VisitingKnight;

    public static ThoughtDef OARO_Thought_ResidentKnight_SquadBeAttackedOnTask; //常驻骑士 - 自己骑士团有分部被袭击

    static OARO_ThoughtDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_ThoughtDefOf));
    }
}
