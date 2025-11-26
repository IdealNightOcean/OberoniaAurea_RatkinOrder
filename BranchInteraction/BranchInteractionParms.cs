using RimWorld;
using RimWorld.Planet;

namespace OberoniaAurea.RatkinOrder;

public readonly struct BranchInteractionParms(Branch branch, Caravan caravan, BranchBuilding building = null)
{
    public readonly Branch Branch = branch;
    public readonly Caravan Caravan = caravan;
    public readonly BranchBuilding Building = building;

    public readonly WorldObject BaseSite => Branch?.BaseSite;
    public readonly RatkinOrder RatkinOrder => Branch?.RatkinOrder;
    public readonly Faction Faction => RatkinOrder?.Faction;
}