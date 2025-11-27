using RimWorld;

namespace OberoniaAurea.RatkinOrder;

[DefOf]
public static class OARO_PawnGroupKindDefOf
{
    public static PawnGroupKindDef OARO_KnightlyVisitor;
    public static PawnGroupKindDef OARO_KnightRefugee;

    static OARO_PawnGroupKindDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(OARO_PawnGroupKindDefOf));
    }
}