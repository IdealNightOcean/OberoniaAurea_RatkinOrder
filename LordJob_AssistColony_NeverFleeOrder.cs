using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_AssistColony_NeverFleeOrder : LordJob_AssistColony_NeverFlee
{
    private Squad squad;
    private bool isCommander;
    public LordJob_AssistColony_NeverFleeOrder() : base() { }
    public LordJob_AssistColony_NeverFleeOrder(Faction faction, IntVec3 fallbackLocation, Squad squad, bool isCommander) : base(faction, fallbackLocation)
    {
        this.squad = squad;
        this.isCommander = isCommander;
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap && squad is not null)
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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref squad, "squad");
        Scribe_Values.Look(ref isCommander, "isCommander");
    }
}
