using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.Utility;

public static class KnightGenerateUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pawn GenerateKnight(PawnKindDef pawnKind, KnightRecord knightRecord, PlanetTile? tile = null, bool forceNew = true)
    {
        PawnGenerationRequest generationRequest = DefaultKnightGenerationRequest(pawnKind, knightRecord.RatkinOrder.Faction, tile, forceNew);
        Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
        PostKnightGenerate(pawn, knightRecord);
        return pawn;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pawn GenerateKnight(PawnGenerationRequest generationRequest, KnightRecord knightRecord)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
        PostKnightGenerate(pawn, knightRecord);
        return pawn;
    }

    public static void PostKnightGenerate(Pawn pawn, KnightRecord knightRecord)
    {
        KnightPawnsManager.Instance.RegisterKnight(pawn, knightRecord);
        Branch branch = knightRecord.Branch;
        if (branch.IsValid())
        {
            Hediff_BranchMedal medalHediff = (Hediff_BranchMedal)pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_BranchMedal);
            medalHediff.SetOrderBranch(branch);

            if (branch.HonorDef?.buffHediff is not null)
            {
                pawn.health.AddHediff(branch.HonorDef.buffHediff);
            }
        }
        if (knightRecord.IsCombatant)
        {
            PostCombatantGenerate(pawn, knightRecord);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PawnGenerationRequest DefaultKnightGenerationRequest(PawnKindDef pawnKind, Faction faction, PlanetTile? tile = null, bool forceNew = true)
    {
        PawnGenerationRequest generationRequest = OberoniaAurea_Frame.Utility.OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile, forceNew: forceNew);
        generationRequest.ForcedTraits = [OARO_ModDefOf.OARO_OrderKnight];
        generationRequest.AllowAddictions = false;
        return generationRequest;
    }

    public static List<Pawn> GenerateCombatantKnights(CombatKnightGenerateParms parms)
    {
        List<Pawn> pawns = [];

        RatkinOrder ratkinOrder = parms.RatkinOrder;
        Branch branch = parms.Branch;
        if (!TryGetRandomPawnGroupMakerForOrder(ratkinOrder, branch, parms.PawnGroupKind, out PawnGroupOption groupOption))
        {
            Log.Error($"[OARO] 在 {ratkinOrder} 中未找到可用于 {parms.PawnGroupKind} 的 {nameof(PawnGroupOption)}");
            return pawns;
        }

        try
        {
            Faction faction = parms.Faction;
            bool isFriendly = parms.IsFriendly;
            int mapTile = parms.Map.Tile;

            IReadOnlyList<PawnGenOption> genOptions;
            int memberCount = branch is null ? parms.MemberCount : Mathf.Min(parms.MemberCount, branch.Squad.MemberCountInt);
            if (memberCount > 0)
            {
                genOptions = groupOption.GetRandomGroupOptionsWithTag("KnightMember");
                if (genOptions is not null && genOptions.Count > 0)
                {
                    for (int i = 0; i < memberCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = genOptions.RandomElementByWeight(g => g.selectionWeight).kind;
                            KnightRecord knightRecord = new(ratkinOrder, branch, isCombatant: true, isCommander: false);
                            Pawn pawn = GenerateKnight(pawnKind, knightRecord, tile: mapTile);
                            pawns.Add(pawn);
                        }
                        catch (Exception subEx1)
                        {
                            ModUtility.LogExceptionError(subEx1,
                                errorDesc: "生成普通骑士",
                                typeName: nameof(KnightGenerateUtility),
                                methodName: nameof(GenerateCombatantKnights),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"[OARO] 未找到带有标签 \"KnightMember\" 的可用 {nameof(PawnGenOption)} 用于选择 {nameof(PawnGroupOption)}");
                }
            }

            int commanderCount = branch is null ? parms.CommanderCount : Mathf.Min(parms.CommanderCount, branch.Squad.CommanderCountInt);
            if (commanderCount > 0)
            {
                genOptions = groupOption.GetRandomGroupOptionsWithTag("KnightCommander");
                if (genOptions is not null && genOptions.Count > 0)
                {
                    for (int i = 0; i < commanderCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = genOptions.RandomElementByWeight(g => g.selectionWeight).kind;
                            KnightRecord knightRecord = new(ratkinOrder, branch, isCombatant: true, isCommander: true);
                            Pawn pawn = GenerateKnight(pawnKind, knightRecord, tile: mapTile);
                            pawns.Add(pawn);
                        }
                        catch (Exception subEx2)
                        {
                            ModUtility.LogExceptionError(subEx2,
                                errorDesc: "生成骑士长",
                                typeName: nameof(KnightGenerateUtility),
                                methodName: nameof(GenerateCombatantKnights),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"[OARO] 未找到带有标签 \"KnightCommander\" 的可用 {nameof(PawnGenOption)} 用于选择 {nameof(PawnGroupOption)}");
                }
            }

            if (parms.NonKnightCount > 0)
            {
                PawnGroupMaker nonKnightMaker = OberoniaAurea_Frame.Utility.OAFrame_PawnGenerateUtility.GetRandomPawnGroupMakerOfFaction(faction, PawnGroupKindDefOf.Combat, (g) => !g.options.NullOrEmpty());
                if (nonKnightMaker is not null)
                {
                    for (int i = 0; i < parms.NonKnightCount; i++)
                    {
                        try
                        {
                            PawnKindDef pawnKind = nonKnightMaker.options.RandomElementByWeight(g => g.selectionWeight).kind;
                            PawnGenerationRequest generationRequest = OberoniaAurea_Frame.Utility.OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile: mapTile);
                            Pawn pawn = PawnGenerator.GeneratePawn(generationRequest);
                            pawns.Add(pawn);
                        }
                        catch (Exception subEx3)
                        {
                            ModUtility.LogExceptionError(subEx3,
                                errorDesc: "生成非骑士单位",
                                typeName: nameof(KnightGenerateUtility),
                                methodName: nameof(GenerateCombatantKnights),
                                needStackTrace: true);
                        }
                    }
                }
                else
                {
                    Log.Error($"[OARO] 在 {faction} 中未找到可用于 {parms.PawnGroupKind} 的 {nameof(PawnGroupMaker)}");
                }
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "生成战斗骑士",
                typeName: nameof(KnightGenerateUtility),
                methodName: nameof(GenerateCombatantKnights),
                needStackTrace: true);
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

    private static void PostCombatantGenerate(Pawn p, KnightRecord record)
    {
        record.RatkinOrder.ReformationManager.PostCombatantGenerate(p, record);
        record.Branch?.PostCombatantGenerate(p, record);
    }
}