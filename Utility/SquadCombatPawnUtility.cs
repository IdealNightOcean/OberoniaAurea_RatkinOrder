using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class SquadCombatPawnUtility
{
    private static IReadOnlyList<IPostSquadCombatPawnGenerate> tmpBranchPostSquadCombat;

    public static List<Pawn> GenerateCombatPawns(RatkinOrderCombatParameter parms)
    {
        List<Pawn> pawns = [];

        Branch branch = parms.Branch;
        if (!TryGetRandomBranchPawnGroupMakerOfKind(branch, PawnGroupKindDefOf.Combat, out PawnGroupOption groupOption))
        {
            Log.Error($"No usable {nameof(PawnGroupOption)} for {PawnGroupKindDefOf.Combat} found in {parms.RatkinOrder}");
            return pawns;
        }

        try
        {
            tmpBranchPostSquadCombat = branch.PostSquadCombatPawnGenerate;
            bool isFriendly = parms.IsFriendly;

            Faction faction = branch.RatkinOrder.Faction;
            int mapTile = parms.Map.Tile;

            IReadOnlyList<PawnGenOption> genOptions;
            if (parms.MemberCount > 0)
            {
                genOptions = groupOption.GetOptionsWithTag("KnightMember");
                if (genOptions is not null && genOptions.Count > 0)
                {
                    for (int i = 0; i < parms.MemberCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = genOptions.RandomElementByWeight(g => g.selectionWeight).kind;
                            KnightRecord knightRecord = new(branch, isCommander: false);
                            Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, knightRecord, tile: mapTile);
                            PostSquadCombatPawnGenerate(pawn, branch, isCommander: false, friendly: isFriendly);
                            pawns.Add(pawn);
                        }
                        catch (Exception ex)
                        {
                            ModUtility.LogExceptionError(ex,
                                errorDesc: "generating the knight member",
                                typeName: nameof(SquadCombatPawnUtility),
                                methodName: nameof(GenerateCombatPawns),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"No usable {nameof(PawnGenOption)} with tag \"KnightMember\" for select {nameof(PawnGroupOption)}");
                }
            }

            if (parms.CommanderCount > 0)
            {
                genOptions = groupOption.GetOptionsWithTag("KnightCommander");
                if (genOptions is not null && genOptions.Count > 0)
                {
                    for (int i = 0; i < parms.CommanderCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = genOptions.RandomElementByWeight(g => g.selectionWeight).kind;
                            KnightRecord knightRecord = new(branch, isCommander: true);
                            Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, knightRecord, tile: mapTile);
                            PostSquadCombatPawnGenerate(pawn, branch, isCommander: true, friendly: isFriendly);
                            pawns.Add(pawn);
                        }
                        catch (Exception ex)
                        {
                            ModUtility.LogExceptionError(ex,
                                errorDesc: "generating the knight commander",
                                typeName: nameof(SquadCombatPawnUtility),
                                methodName: nameof(GenerateCombatPawns),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"No usable {nameof(PawnGenOption)} with tag \"KnightCommander\" for select {nameof(PawnGroupOption)}");
                }
            }

            if (parms.NonKnightCount > 0)
            {
                PawnGroupMaker nonKnightMaker = OAFrame_PawnGenerateUtility.GetRandomPawnGroupMakerOfFaction(faction, PawnGroupKindDefOf.Combat, (g) => !g.options.NullOrEmpty());
                if (nonKnightMaker is not null)
                {
                    for (int i = 0; i < parms.NonKnightCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = nonKnightMaker.options.RandomElementByWeight(g => g.selectionWeight).kind;
                            PawnGenerationRequest generationRequest = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile: mapTile);
                            pawns.Add(PawnGenerator.GeneratePawn(generationRequest));
                        }
                        catch (Exception ex)
                        {
                            ModUtility.LogExceptionError(ex,
                                errorDesc: "generating the non-knight unit",
                                typeName: nameof(SquadCombatPawnUtility),
                                methodName: nameof(GenerateCombatPawns),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"No usable {nameof(PawnGroupMaker)} for {PawnGroupKindDefOf.Combat} found in {faction}");
                }
            }
        }
        finally
        {
            tmpBranchPostSquadCombat = null;
        }

        return pawns;
    }

    private static bool TryGetRandomBranchPawnGroupMakerOfKind(Branch branch, PawnGroupKindDef groupKind, out PawnGroupOption groupOption)
    {
        if (branch.HonorDef?.TryGetRandomPawnGroupMaker(groupKind, out groupOption) ?? false)
        {
            return true;
        }
        if (branch.RatkinOrder.Def.TryGetRandomPawnGroupMaker(groupKind, out groupOption))
        {
            return true;
        }
        groupOption = null;
        return false;
    }

    private static void PostSquadCombatPawnGenerate(Pawn p, Branch branch, bool isCommander, bool friendly)
    {
        if (tmpBranchPostSquadCombat is not null && tmpBranchPostSquadCombat.Count > 0)
        {
            for (int i = 0; i < tmpBranchPostSquadCombat.Count; i++)
            {
                try
                {
                    tmpBranchPostSquadCombat[i].PostSquadCombatPawnGenerate(p, branch, isCommander: isCommander, friendly: friendly);
                }
                catch (Exception ex)
                {
                    string processorTypeName = tmpBranchPostSquadCombat[i]?.GetType()?.FullName ?? "UnknownProcessor";
                    Log.Error($"Exception occurred while executing post-squad assist processor: ProcessorType={processorTypeName}, ErrorMessage: {ex.Message}");
                    continue;
                }
            }
        }
    }
}