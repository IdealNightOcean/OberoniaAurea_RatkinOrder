using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_PawnUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRatkin(this Pawn pawn) => pawn.def == OARO_ThingDefOf.Ratkin || pawn.def == OARO_ThingDefOf.Ratkin_Su;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanBeKnight(this Pawn pawn) => pawn is not null && pawn.RaceProps.Humanlike;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSkillLevel(this Pawn pawn, SkillDef skill) => pawn.skills?.GetSkill(skill).GetLevel() ?? 0;

    public static int GetTotalSkillLevelOf(IEnumerable<Pawn> pawns, SkillDef skill)
    {
        if (pawns is null)
        {
            return 0;
        }

        int totalLevel = 0;
        foreach (Pawn pawn in pawns)
        {
            totalLevel += pawn.GetSkillLevel(skill);
        }

        return totalLevel;
    }
}