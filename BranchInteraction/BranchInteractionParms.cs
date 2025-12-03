using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public readonly struct BranchInteractionParms
{
    public readonly Branch Branch;
    public readonly Caravan Caravan;
    public readonly Map Map;
    public readonly BranchBuilding Building;

    public BranchInteractionParms(Branch branch, BranchBuilding building = null)
    {
        Branch = branch;
        Building = building;
    }
    public BranchInteractionParms(Branch branch, Caravan caravan, BranchBuilding building = null)
    {
        Branch = branch;
        Caravan = caravan;
        Building = building;
    }

    public BranchInteractionParms(Branch branch, Map map, BranchBuilding building = null)
    {
        Branch = branch;
        Map = map;
        Building = building;
    }

    public readonly WorldObject BaseSite => Branch?.BaseSite;
    public readonly RatkinOrder RatkinOrder => Branch?.RatkinOrder;
    public readonly Faction Faction => RatkinOrder?.Faction;
}