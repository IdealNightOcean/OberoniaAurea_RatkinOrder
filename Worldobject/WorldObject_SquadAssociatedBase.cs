using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class WorldObject_SquadAssociatedBase : WorldObject_InteractWithFixedCarvanBase
{
    protected Squad squad;
    public Branch Branch => squad.Branch;
    public RatkinOrder RatkinOrder => squad.RatkinOrder;

    public void SetSquad(Squad squad)
    {
        this.squad = squad;
        SetFaction(RatkinOrder.Faction);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref squad, "squad");
    }
}
