using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_AssaultColony_NeverFleeOrder : LordJob_AssaultColony_NeverFlee
{
    private Squad squad;
    private bool isCommander;

    public LordJob_AssaultColony_NeverFleeOrder() { }

    public LordJob_AssaultColony_NeverFleeOrder(SpawnedPawnParams parms) : base(parms) { }

    public LordJob_AssaultColony_NeverFleeOrder(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool sappers = false, bool useAvoidGridSmart = false, bool canSteal = true, bool breachers = false, bool canPickUpOpportunisticWeapons = false)
        : base(assaulterFaction, canKidnap, canTimeoutOrFlee, sappers, useAvoidGridSmart, canSteal, breachers, canPickUpOpportunisticWeapons)
    { }

    public void SetSquadIdentity(Squad squad, bool isCommander)
    {
        this.squad = squad;
        this.isCommander = isCommander;
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap)
        {
            if (squad is not null)
            {
                if (isCommander)
                {
                    squad.SquadStat.CommanderCount++;
                }
                else
                {
                    squad.SquadStat.MemberCount++;
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref squad, "squad");
        Scribe_Values.Look(ref isCommander, "isCommander");
    }
}
