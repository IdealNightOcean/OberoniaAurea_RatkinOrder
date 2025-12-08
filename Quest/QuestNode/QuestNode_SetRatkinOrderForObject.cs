using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetRatkinOrderForObject : QuestNode
{
    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<object> target;

    protected override bool TestRunInt(Slate slate)
    {
        return false;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        object target = this.target.GetValue(slate);
        if (target is null)
        {
            return;
        }

        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
        if (ratkinOrder.IsValid() && target is ISingleRatkinOrderRelated orderRelated)
        {
            orderRelated.InitRatkinOrder(ratkinOrder);
        }
    }
}