using OberoniaAurea.RatkinOrder.Utility;
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

    public override void Cleanup()
    {
        base.Cleanup();
        foreach (Pawn p in lord.ownedPawns)
        {
            if (!p.Spawned && !p.Dead)
            {
                KnightReturnToBranch(p);
            }
        }
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap)
        {
            KnightReturnToBranch(p);
        }
    }

    private static void KnightReturnToBranch(Pawn pawn)
    {
        if (!pawn.CanBeKnight())
        {
            return;
        }
        KnightRecord knightRecord = KnightPawnsManager.Instance.GetKnightRecord(pawn);
        if (knightRecord?.Branch?.Squad is not null)
        {
            if (knightRecord.IsCommander)
            {
                knightRecord.Branch.Squad.AdjustCrew(member: 0f, commander: 1f);
            }
            else
            {
                knightRecord.Branch.Squad.AdjustCrew(member: 1f, commander: 0f);
            }
        }
    }
}
