using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 叛乱镇压 - 贵族领地士兵生成（特化类）
/// </summary>
internal sealed class GenStep_NobilityTerritoryPawns : GenStep
{
    public override int SeedPart => 233548778;

    private IEnumerable<Pawn> GenerateEnemies(MapParent_NobilityTerritory nobilityTerritory)
    {
        Faction faction = nobilityTerritory.Faction;
        PawnGroupMaker groupMaker = OAFrame_PawnGenerateUtility.GetRandomPawnGroupMakerOfFaction(faction, PawnGroupKindDefOf.Combat, predicater: (g) => !g.options.NullOrEmpty());
        if (groupMaker is null)
        {
            yield break;
        }
        int enemyCount = nobilityTerritory.Parent?.Troops ?? 20;
        PlanetTile tile = nobilityTerritory.Tile;
        for (int i = 0; i < enemyCount; i++)
        {
            PawnKindDef pawnKind = groupMaker.options.RandomElementByWeight(p => p.selectionWeight).kind;
            PawnGenerationRequest generationRequest = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile);
            // generationRequest.MustBeCapableOfViolence = true;

            yield return PawnGenerator.GeneratePawn(generationRequest);
        }
    }

    public override void Generate(Map map, GenStepParams parms)
    {
        IntVec3 baseCenter;
        if (!MapGenerator.TryGetVar("SettlementRect", out CellRect settlementRect))
        {
            baseCenter = settlementRect.CenterCell;
            Log.Error($"[OARO] No rect of settlement rect set when running {nameof(GenStep_NobilityTerritoryPawns)}!");
        }
        else
        {
            baseCenter = map.Center;
        }
        Faction faction = map.Parent.Faction;
        Lord singlePawnLord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, baseCenter, 25000), map);
        TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
        ResolveParams resolveParams = default;
        resolveParams.rect = settlementRect;
        resolveParams.faction = faction;
        resolveParams.singlePawnLord = singlePawnLord;
        resolveParams.singlePawnSpawnCellExtraPredicate = c => map.reachability.CanReachMapEdge(c, traverseParms);

        if (map.Parent is MapParent_NobilityTerritory nobilityTerritory)
        {
            IEnumerable<Pawn> fighters = GenerateEnemies(nobilityTerritory);
            foreach (Pawn p in fighters)
            {
                ResolveParams pawnResolveParams = resolveParams;
                pawnResolveParams.singlePawnToSpawn = p;
                BaseGen.symbolStack.Push("pawn", pawnResolveParams);
            }
        }

        BaseGen.globalSettings.map = map;
        BaseGen.Generate();
    }
}