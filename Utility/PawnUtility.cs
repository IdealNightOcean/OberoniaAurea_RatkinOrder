using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class PawnUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRatkin(this Pawn pawn)
    {
        return pawn.def == OARO_ThingDefOf.Ratkin || pawn.def == OARO_ThingDefOf.Ratkin_Su;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOrderKnight(this Pawn pawn)
    {
        if (pawn is null || !pawn.RaceProps.Humanlike)
        {
            return false;
        }
        return GameComponent_RatkinOrder.Instance.KnightPawns.Contains(pawn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSkillLevel(this Pawn pawn, SkillDef skill)
    {
        return pawn.skills?.GetSkill(skill).GetLevel() ?? 0;
    }

    public static Pawn GenerateOrderKnight(PawnKindDef pawnKind, RatkinOrder ratkinOrder, bool forceNew)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(DefaultKnightGenerationRequest(pawnKind, ratkinOrder.Faction, forceNew: forceNew));
        pawn.SetRatkinOrder(ratkinOrder);
        return pawn;
    }

    public static PawnGenerationRequest DefaultKnightGenerationRequest(PawnKindDef pawnKind, Faction faction, bool forceNew)
    {
        PawnGenerationRequest generationRequest = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, forceNew: forceNew);
        generationRequest.ForcedTraits = [OARO_ModDefOf.OARO_OrderKnight];
        generationRequest.AllowAddictions = false;

        return generationRequest;
    }

    public static void SetRatkinOrder(this Pawn pawn, RatkinOrder ratkinOrder)
    {
        Hediff_Knight knightHediff = (Hediff_Knight)pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_OrderKnight);
        knightHediff.InitRatkinOrder(ratkinOrder);
    }

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