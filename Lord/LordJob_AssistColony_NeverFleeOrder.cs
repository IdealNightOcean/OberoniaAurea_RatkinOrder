using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class LordJob_AssistColony_NeverFleeOrder : LordJob_AssistColony_NeverFlee
{
    private Branch branch;
    private bool isCommander;

    private Squad Squad => branch.Squad;

    public LordJob_AssistColony_NeverFleeOrder() : base() { }
    public LordJob_AssistColony_NeverFleeOrder(Faction faction, IntVec3 fallbackLocation, Branch branch, bool isCommander) : base(faction, fallbackLocation)
    {
        this.branch = branch;
        this.isCommander = isCommander;
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        if (condition == PawnLostCondition.ExitedMap && Squad is not null)
        {
            if (isCommander)
            {
                Squad.SquadStat.CommanderCount++;
            }
            else
            {
                Squad.SquadStat.MemberCount++;
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref isCommander, "isCommander");
    }
}
