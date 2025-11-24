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
        if (!pawn.CanBeKnight())
        {
            return;
        }
        KnightRecord knightRecord = pawn.GetKnightRecord();
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
