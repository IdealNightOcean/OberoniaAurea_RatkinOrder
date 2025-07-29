using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_CheckGroupPatrolStart : QuestNode
{
    public SlateRef<RatkinOrder> order;

    protected override bool TestRunInt(Slate slate)
    {
        RatkinOrder ratkinOrder = order.GetValue(slate);
        return ratkinOrder is not null && ratkinOrder.SquadManager.GroupPatrolManager.IsPatrolStarted;
    }

    protected override void RunInt() { }

}