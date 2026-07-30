using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetRatkinOrderForObject : QuestNode
{
    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<object> target;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        object target = this.target.GetValue(slate);
        if (target is null || target is not ISingleRatkinOrderRelated orderRelated)
        {
            return;
        }

        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(OARO_KeyLibrary_SlateStoreAs.ratkinOrder);
        if (ratkinOrder.IsValid())
        {
            orderRelated.InitRatkinOrder(ratkinOrder);
        }
    }
}