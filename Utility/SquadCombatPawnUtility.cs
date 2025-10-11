using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class SquadCombatPawnUtility
{
    private static IReadOnlyList<IPostSquadCombatPawnGenerate> tmpBranchPostSquadCombat;

    public static List<Pawn> GenerateCombatPawns(Branch branch, Map map, int memberCount, int commanderCount, bool friendly)
    {
        List<Pawn> pawns = [];

        if (!TryGetRandomBranchPawnGroupMakerOfKind(branch, PawnGroupKindDefOf.Combat, out PawnGroupMaker groupMaker))
        {
            Log.Error($"No usable PawnGroupMaker for {PawnGroupKindDefOf.Combat} found in {branch.RatkinOrder}");
            return pawns;
        }

        try
        {
            tmpBranchPostSquadCombat = branch.PostSquadCombatPawnGenerate;

            Faction faction = branch.RatkinOrder.Faction;
            int mapTile = map.Tile;

            if (memberCount > 0 && !groupMaker.options.NullOrEmpty())
            {
                for (int i = 0; i < memberCount; i++)
                {
                    PawnKindDef pawnKind = groupMaker.guards.RandomElementByWeight(g => g.selectionWeight).kind;
                    Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, branch.RatkinOrder, branch, isCommander: false, tile: mapTile);
                    PostSquadCombatPawnGenerate(pawn, branch, isCommander: false, friendly: friendly);
                    pawns.Add(pawn);
                }
            }

            if (commanderCount > 0 && !groupMaker.options.NullOrEmpty())
            {
                for (int i = 0; i < commanderCount; i++)
                {
                    PawnKindDef pawnKind = groupMaker.options.RandomElementByWeight(g => g.selectionWeight).kind;
                    Pawn pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, branch.RatkinOrder, branch, isCommander: true, tile: mapTile);
                    PostSquadCombatPawnGenerate(pawn, branch, isCommander: true, friendly: friendly);
                    pawns.Add(pawn);
                }
            }
        }
        finally
        {
            tmpBranchPostSquadCombat = null;
        }

        return pawns;
    }

    private static bool TryGetRandomBranchPawnGroupMakerOfKind(Branch branch, PawnGroupKindDef groupKind, out PawnGroupMaker groupMaker)
    {
        if (branch.HonorProperties?.TryGetRandomPawnGroupMaker(groupKind, out groupMaker) ?? false)
        {
            return true;
        }
        if (branch.RatkinOrder.Def.TryGetRandomPawnGroupMaker(groupKind, out groupMaker))
        {
            return true;
        }
        groupMaker = null;
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