using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetRatkinOrderFromSlate : QuestNode_GetRatkinOrderBase
{
    protected override RatkinOrder GetRatkinOrder(Slate slate)
    {
        slate.TryGet(storeAs.GetValue(slate), out RatkinOrder ratkinOrder);
        return ratkinOrder;
    }
}