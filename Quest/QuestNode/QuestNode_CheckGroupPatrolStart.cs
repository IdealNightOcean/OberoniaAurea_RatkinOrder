using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_CheckGroupPatrolStart : QuestNode
{
    public SlateRef<RatkinOrder> ratkinOrder;

    protected override bool TestRunInt(Slate slate)
    {
        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
        return ratkinOrder is not null && ratkinOrder.SquadManager.GroupPatrolManager.IsPatrolStarted;
    }

    protected override void RunInt() { }

}