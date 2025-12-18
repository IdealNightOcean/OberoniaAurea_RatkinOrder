using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_ExitMapBestForJointPatrol : LordJob_ExitMapBest
{
    private RatkinOrder ratkinOrder;
    public LordJob_ExitMapBestForJointPatrol(RatkinOrder ratkinOrder) : base() { this.ratkinOrder = ratkinOrder; }
    public LordJob_ExitMapBestForJointPatrol(RatkinOrder ratkinOrder, LocomotionUrgency locomotion, bool canDig = false, bool canDefendSelf = false) : base(locomotion, canDig, canDefendSelf)
    {
        this.ratkinOrder = ratkinOrder;
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap)
        {
            p.DeSpawnOrDeselect();
            ratkinOrder.JointPatrolManager.OnResidentKnightBackTeam(p);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ratkinOrder, nameof(ratkinOrder));
    }
}