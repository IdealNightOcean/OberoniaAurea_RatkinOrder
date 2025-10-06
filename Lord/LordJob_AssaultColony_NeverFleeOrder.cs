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
        Hediff_Knight knightHediff = pawn.GetKnightHediff();
        if (knightHediff is not null && knightHediff.Squad is not null)
        {
            if (knightHediff.IsCommander)
            {
                knightHediff.Squad.SquadStat.CommanderCount += 1f;
            }
            else
            {
                knightHediff.Squad.SquadStat.MemberCount += 1f;
            }
        }
    }
}
