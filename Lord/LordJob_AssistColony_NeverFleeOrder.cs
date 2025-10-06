using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_AssistColony_NeverFleeOrder : LordJob_AssistColony_NeverFlee
{
    public LordJob_AssistColony_NeverFleeOrder() : base() { }
    public LordJob_AssistColony_NeverFleeOrder(Faction faction, IntVec3 fallbackLocation) : base(faction, fallbackLocation)
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
