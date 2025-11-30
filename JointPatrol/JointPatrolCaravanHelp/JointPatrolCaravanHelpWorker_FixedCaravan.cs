using OberoniaAurea_Frame;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class JointPatrolCaravanHelpWorker_FixedCaravan : JointPatrolCaravanHelpWorker
{
    public override bool Notify_CaravanArrived(Caravan caravan, Branch branch, WorldObject_InteractiveBase incidentSite)
    {
        if (incidentSite is not WorldObject_JointPatrolCaravanHelpSite_FixedCaravan fixedCaravanIncidentSite)
        {
            Log.Error($"[OARO] Failed to cast {nameof(incidentSite)} to {nameof(WorldObject_JointPatrolCaravanHelpSite_FixedCaravan)}.");
            return false;
        }

        fixedCaravanIncidentSite.StartWork(caravan);
        return true;
    }

    public abstract bool PostStartWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite);
    public abstract void InterruptWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite);
    public abstract void FinishWork(FixedCaravan fixedCaravan, Branch branch, WorldObject_JointPatrolCaravanHelpSite_FixedCaravan incidentSite);

}