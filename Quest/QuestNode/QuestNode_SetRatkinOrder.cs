using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetRatkinOrder : QuestNode_GetRatkinOrderBase
{
    public SlateRef<RatkinOrder> order;

    protected override RatkinOrder GetRatkinOrder(Slate slate)
    {
        return order.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
    }
}