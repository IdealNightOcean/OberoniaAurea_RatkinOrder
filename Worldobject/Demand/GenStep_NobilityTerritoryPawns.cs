using RimWorld;
using RimWorld.BaseGen;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

internal sealed class GenStep_NobilityTerritoryPawns : GenStep
{
    public override int SeedPart => 233548778;

    private IEnumerable<Pawn> GenerateEnemies(MapParent_NobilityTerritory nobilityTerritory)
    {
        return [];
    }

    public override void Generate(Map map, GenStepParams parms)
    {
        IntVec3 baseCenter;
        if (!MapGenerator.TryGetVar("RectOfInterest", out CellRect interestRect))
        {
            baseCenter = interestRect.CenterCell;
            Log.Error($"No rect of interest set when running {nameof(GenStep_NobilityTerritoryPawns)}!");
        }
        else
        {
            baseCenter = map.Center;
        }
        Faction faction = map.Parent.Faction;
        Lord singlePawnLord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, baseCenter, 25000), map);
        TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors);
        ResolveParams resolveParams = default;
        resolveParams.rect = interestRect;
        resolveParams.faction = faction;
        resolveParams.singlePawnLord = singlePawnLord;
        resolveParams.singlePawnSpawnCellExtraPredicate = c => map.reachability.CanReachMapEdge(c, traverseParms);

        if (map?.Parent is MapParent_NobilityTerritory nobilityTerritory)
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