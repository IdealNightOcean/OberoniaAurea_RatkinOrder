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
    public static bool IsRatkin(this Pawn pawn) => pawn.def == OARO_ThingDefOf.Ratkin || pawn.def == OARO_ThingDefOf.Ratkin_Su;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanBeKnight(this Pawn pawn) => pawn is not null && pawn.RaceProps.Humanlike;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSkillLevel(this Pawn pawn, SkillDef skill) => pawn.skills?.GetSkill(skill).GetLevel() ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pawn GenerateOrderKnight(PawnKindDef pawnKind, KnightRecord knightRecord, PlanetTile? tile = null, bool forceNew = true)
    {
        PawnGenerationRequest generationRequest = DefaultKnightGenerationRequest(pawnKind, knightRecord.RatkinOrder.Faction, tile, forceNew);
        return GenerateOrderKnight(generationRequest, knightRecord);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pawn GenerateOrderKnight(PawnGenerationRequest generationRequest, KnightRecord knightRecord)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
        KnightPawnsManager.Instance.RegisterKnight(pawn, knightRecord);
        if (knightRecord.Branch is not null)
        {
            Hediff_BranchMedal medalHediff = (Hediff_BranchMedal)pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_BranchMedal);
            medalHediff.SetOrderBranch(knightRecord.Branch);
        }
        return pawn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PawnGenerationRequest DefaultKnightGenerationRequest(PawnKindDef pawnKind, Faction faction, PlanetTile? tile = null, bool forceNew = true)
    {
        PawnGenerationRequest generationRequest = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile, forceNew: forceNew);
        generationRequest.ForcedTraits = [OARO_ModDefOf.OARO_OrderKnight];
        generationRequest.AllowAddictions = false;
        return generationRequest;
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