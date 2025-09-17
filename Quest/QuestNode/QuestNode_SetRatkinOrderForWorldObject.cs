using RimWorld.Planet;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetRatkinOrderForWorldObject : QuestNode
{
    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<WorldObject> worldObject;

    protected override bool TestRunInt(Slate slate)
    {
        return false;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
        if (ratkinOrder is null)
        {
            return;
        }
        if (worldObject.GetValue(slate) is ISingleRatkinOrderRelated orderRelated)
        {
            orderRelated.InitRatkinOrder(ratkinOrder);
        }
    }
}