using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_PawnUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanBeKnight(this Pawn p) => p is not null && p.RaceProps.Humanlike;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRatkin(this Pawn pawn)
    {
        return pawn.def == OARO_ThingDefOf.Ratkin || pawn.def == OARO_ThingDefOf.Ratkin_Su;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOrderKnight(this Pawn pawn)
    {
        return pawn.CanBeKnight() && GameComponent_RatkinOrder.Instance.KnightPawns.Contains(pawn);
    }

    public static Hediff_Knight GetKnightHediff(this Pawn pawn)
    {
        if (!pawn.CanBeKnight())
        {
            return null;
        }

        return pawn.health.hediffSet.GetFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_OrderKnight) as Hediff_Knight;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSkillLevel(this Pawn pawn, SkillDef skill)
    {
        return pawn.skills?.GetSkill(skill).GetLevel() ?? 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pawn GenerateOrderKnight(PawnGenerationRequest generationRequest, RatkinOrder ratkinOrder, Branch branch = null, bool isCommander = false)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
        pawn.InitKnightHediff(ratkinOrder, branch, isCommander);
        return pawn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pawn GenerateOrderKnight(PawnKindDef pawnKind, RatkinOrder ratkinOrder, Branch branch = null, bool isCommander = false, PlanetTile? tile = null, bool forceNew = false)
    {
        return GenerateOrderKnight(generationRequest: DefaultKnightGenerationRequest(pawnKind, ratkinOrder.Faction, forceNew: forceNew),
                                   ratkinOrder: ratkinOrder,
                                   branch: branch,
                                   isCommander: isCommander);
    }

    public static PawnGenerationRequest DefaultKnightGenerationRequest(PawnKindDef pawnKind, Faction faction, PlanetTile? tile = null, bool forceNew = false)
    {
        PawnGenerationRequest generationRequest = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile, forceNew: forceNew);
        generationRequest.ForcedTraits = [OARO_ModDefOf.OARO_OrderKnight];
        generationRequest.AllowAddictions = false;
        return generationRequest;
    }

    public static void InitKnightHediff(this Pawn pawn, RatkinOrder ratkinOrder, Branch branch = null, bool isCommander = false)
    {
        Hediff_Knight knightHediff = (Hediff_Knight)pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_OrderKnight);
        knightHediff.InitKnightHediff(ratkinOrder, branch, isCommander);
        if (branch is not null)
        {
            Hediff_BranchMedal medalHediff = (Hediff_BranchMedal)pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_BranchMedal);
            medalHediff.InitOrderBranch(branch);
        }
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