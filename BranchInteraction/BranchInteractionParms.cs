using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchInteractionParms
{
    public Branch Branch { get; }
    public RatkinOrder RatkinOrder => Branch?.RatkinOrder;

    public BranchBuilding Building { get; }

    public IThingHolder Target { get; }
    public Caravan TargetCaravan => Target as Caravan;
    public Map TargetMap => Target as Map;


    public BranchInteractionParms(Branch branch, IThingHolder target = null, BranchBuilding building = null)
    {
        Branch = branch;
        Target = target;
        Building = building;
    }
}