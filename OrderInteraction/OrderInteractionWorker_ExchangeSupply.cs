using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderInteractionWorker_ExchangeSupply(OrderInteractionDef def) : OrderInteractionWorker(def)
{
    protected override (bool succeeded, bool doPostApply) InteractionEffect(RatkinOrder ratkinOrder, Map map)
    {
        Window_OrderInteraction_ExchangeSupply exchangeSupplyWin = new(ratkinOrder, map);
        exchangeSupplyWin.PostCloseAction += () => PostApplyInteraction(ratkinOrder, map, succeeded: true);
        Find.WindowStack.Add(exchangeSupplyWin);
        return (succeeded: true, doPostApply: false);
    }
}