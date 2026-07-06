using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_CheckGroupPatrolStart : QuestNode
{
    public SlateRef<RatkinOrder> ratkinOrder;

    protected override bool TestRunInt(Slate slate)
    {
        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(OARO_KeyLibrary_SlateStoreAs.ratkinOrder);
        return ratkinOrder.IsValid() && ratkinOrder.JointPatrolManager.CurState == JointPatrolManager.PatrolState.Ongoing;
    }

    protected override void RunInt() { }

}