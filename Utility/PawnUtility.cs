using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_PawnUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRatkin(this Pawn pawn) => pawn.def == OARO_ThingDefOf.Ratkin;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanBeKnight(this Pawn pawn) => pawn is not null && pawn.RaceProps.Humanlike;

    public static int GetTotalSkillLevelOf(IEnumerable<Pawn> pawns, SkillDef skill)
    {
        if (pawns is null)
            return 0;

        int totalLevel = 0;
        foreach (Pawn pawn in pawns)
            totalLevel += pawn.GetSkillLevel(skill);

        return totalLevel;
    }

    public static bool IsHealthyPawn(Pawn pawn)
    {
        if (pawn.DestroyedOrNull() || pawn.Downed)
            return false;

        HediffSet hediffSet = pawn.health.hediffSet;
        if (hediffSet.BleedRateTotal > 0.001f || hediffSet.AnyHediffMakesSickThought)
            return false;


        foreach (Hediff hediff in hediffSet.hediffs)
        {
            if (hediff is Hediff_Injury)
            {
                return false;
            }
        }

        return true;
    }

}