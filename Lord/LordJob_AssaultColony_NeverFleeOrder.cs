using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_AssaultColony_NeverFleeOrder : LordJob_AssaultColony_NeverFlee
{
    public LordJob_AssaultColony_NeverFleeOrder() { }

    public LordJob_AssaultColony_NeverFleeOrder(SpawnedPawnParams parms) : base(parms) { }

    public LordJob_AssaultColony_NeverFleeOrder(Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool sappers = false, bool useAvoidGridSmart = false, bool canSteal = true, bool breachers = false, bool canPickUpOpportunisticWeapons = false)
        : base(assaulterFaction, canKidnap, canTimeoutOrFlee, sappers, useAvoidGridSmart, canSteal, breachers, canPickUpOpportunisticWeapons)
    { }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap)
        {
            Notify_PawnLeftMap(p);
        }
    }

    private static void Notify_PawnLeftMap(Pawn pawn)
    {
        KnightRecord knightRecord = pawn.GetKnightRecord();
        if (knightRecord?.Branch?.Squad is not null)
        {
            if (knightRecord.IsCommander)
            {
                knightRecord.Branch.Squad.CommanderCount += 1f;
            }
            else
            {
                knightRecord.Branch.Squad.MemberCount += 1f;
            }
        }
    }
}
