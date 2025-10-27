using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetRatkinOrderOfFaction : QuestNode_GetRatkinOrderBase
{
    public SlateRef<Faction> faction;

    protected override RatkinOrder GetRatkinOrder(Slate slate)
    {
        return RatkinOrderManager.GetRatkinOrderForFaction(faction.GetValue(slate));
    }
}
