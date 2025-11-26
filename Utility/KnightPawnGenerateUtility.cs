using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class KnightPawnGenerateUtility
{
    private static readonly List<IPostBranchCombatKnightGenerate> tmpIPostBranchCombatKnightGenerate = [];

    public static List<Pawn> GenerateBranchCombatKnights(CombatKnightGenerateParms parms, bool doBranchPostProcess = true)
    {
        List<Pawn> pawns = [];

        RatkinOrder ratkinOrder = parms.RatkinOrder;
        Branch branch = parms.Branch;
        if (!TryGetRandomPawnGroupMakerForOrder(ratkinOrder, branch, parms.PawnGroupKind, out PawnGroupOption groupOption))
        {
            Log.Error($"[OARO] No usable {nameof(PawnGroupOption)} for {parms.PawnGroupKind} found in {ratkinOrder}");
            return pawns;
        }

        try
        {
            doBranchPostProcess = doBranchPostProcess && branch is not null;
            if (doBranchPostProcess)
            {
                tmpIPostBranchCombatKnightGenerate.Clear();
                tmpIPostBranchCombatKnightGenerate.AddRange(branch.PostSquadCombatPawnGenerate);
            }

            bool isFriendly = parms.IsFriendly;

            Faction faction = parms.Faction;
            int mapTile = parms.Map.Tile;

            IReadOnlyList<PawnGenOption> genOptions;
            int memberCount = branch is null ? parms.MemberCount : Mathf.Min(parms.MemberCount, branch.Squad.MemberCountInt);
            if (memberCount > 0)
            {
                genOptions = groupOption.GetOptionsWithTag("KnightMember");
                if (genOptions is not null && genOptions.Count > 0)
                {
                    for (int i = 0; i < memberCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = genOptions.RandomElementByWeight(g => g.selectionWeight).kind;
                            KnightRecord knightRecord = new(ratkinOrder, branch, isCommander: false);
                            Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, knightRecord, tile: mapTile);
                            if (doBranchPostProcess)
                                PostBranchCombatKnightGenerate(pawn, branch, isCommander: false, friendly: isFriendly);

                            pawns.Add(pawn);
                        }
                        catch (Exception ex)
                        {
                            ModUtility.LogExceptionError(ex,
                                errorDesc: "generating the knight member",
                                typeName: nameof(KnightPawnGenerateUtility),
                                methodName: nameof(GenerateBranchCombatKnights),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"[OARO] No usable {nameof(PawnGenOption)} with tag \"KnightMember\" for select {nameof(PawnGroupOption)}");
                }
            }

            int commanderCount = branch is null ? parms.CommanderCount : Mathf.Min(parms.CommanderCount, branch.Squad.CommanderCountInt);
            if (commanderCount > 0)
            {
                genOptions = groupOption.GetOptionsWithTag("KnightCommander");
                if (genOptions is not null && genOptions.Count > 0)
                {
                    for (int i = 0; i < commanderCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = genOptions.RandomElementByWeight(g => g.selectionWeight).kind;
                            KnightRecord knightRecord = new(ratkinOrder, branch, isCommander: true);
                            Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, knightRecord, tile: mapTile);
                            if (doBranchPostProcess)
                                PostBranchCombatKnightGenerate(pawn, branch, isCommander: true, friendly: isFriendly);

                            pawns.Add(pawn);
                        }
                        catch (Exception ex)
                        {
                            ModUtility.LogExceptionError(ex,
                                errorDesc: "generating the knight commander",
                                typeName: nameof(KnightPawnGenerateUtility),
                                methodName: nameof(GenerateBranchCombatKnights),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"[OARO] No usable {nameof(PawnGenOption)} with tag \"KnightCommander\" for select {nameof(PawnGroupOption)}");
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
                            Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
                            pawns.Add(pawn);
                        }
                        catch (Exception ex)
                        {
                            ModUtility.LogExceptionError(ex,
                                errorDesc: "generating the non-knight unit",
                                typeName: nameof(KnightPawnGenerateUtility),
                                methodName: nameof(GenerateBranchCombatKnights),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"[OARO] No usable {nameof(PawnGroupMaker)} for {parms.PawnGroupKind} found in {faction}");
                }
            }
        }
        finally
        {
            tmpIPostBranchCombatKnightGenerate.Clear();
        }

        return pawns;
    }

    public static bool TryGetRandomPawnGroupMakerForOrder(RatkinOrder ratkinOrder, Branch branch, PawnGroupKindDef groupKind, out PawnGroupOption groupOption)
    {
        if (branch is not null)
        {
            if (branch.HonorDef?.TryGetRandomPawnGroupMaker(groupKind, out groupOption) ?? false)
            {
                return true;
            }
        }

        if (ratkinOrder.Def.TryGetRandomPawnGroupMaker(groupKind, out groupOption))
        {
            return true;
        }
        groupOption = null;
        return false;
    }

    private static void PostBranchCombatKnightGenerate(Pawn p, Branch branch, bool isCommander, bool friendly)
    {
        if (tmpIPostBranchCombatKnightGenerate is not null && tmpIPostBranchCombatKnightGenerate.Count > 0)
        {
            for (int i = 0; i < tmpIPostBranchCombatKnightGenerate.Count; i++)
            {
                try
                {
                    tmpIPostBranchCombatKnightGenerate[i].PostBranchCombatKnightGenerate(p, branch, isCommander: isCommander, friendly: friendly);
                }
                catch (Exception ex)
                {
                    string processorTypeName = tmpIPostBranchCombatKnightGenerate[i]?.GetType()?.FullName ?? "UnknownProcessor";
                    ModUtility.LogExceptionError(ex,
                        errorDesc: $"executing post-squad assist processor: {processorTypeName}",
                        typeName: nameof(KnightPawnGenerateUtility),
                        methodName: nameof(PostBranchCombatKnightGenerate),
                        needStackTrace: true);
                    continue;
                }
            }
        }
    }
}