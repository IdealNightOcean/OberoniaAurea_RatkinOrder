using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_MercyQuestWatcher : QuestNode
{
    public SlateRef<Faction> subFaction;
    public SlateRef<Faction> parentFaction;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            SubFaction = subFaction.GetValue(slate) ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.SubFaction),
            ParentFaction = parentFaction.GetValue(slate) ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentFaction),
        };
        QuestGen.quest.AddPart(questPart_MercyQuestWatcher);
    }
}

public class QuestPart_MercyQuestWatcher : QuestPart
{
    public Faction SubFaction;
    public Faction ParentFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref SubFaction, "SubFaction");
        Scribe_References.Look(ref ParentFaction, "ParentFaction");
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (quest.State != QuestState.EndedSuccess)
        {
            return;
        }
        RatkinOrder ratkinOrder = RatkinOrderManager.Instance.AllRatkinOrders.RandomElementWithFallback(null);
        if (ratkinOrder is null)
        {
            return;
        }
        OrderLetter_SimpleAttachments orderLetter = (OrderLetter_SimpleAttachments)OrderLetterUtility.MakeOrderLetter(
              label: "OARO_Offical_MercyQuestSuccessLabel".Translate(ratkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)),
              text: "OARO_Offical_MercyQuestSuccessText".Translate(ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName), quest.name.Named("QuestName")),
              def: OrderLetterDefOf.OARO_OfficialLetter_SimpleAttachments,
              relatedOrder: ratkinOrder,
              sender: ratkinOrder.NameColored,
              relatedLetterType: OrderLetter.RelatedLetterType.Positive);
        OrderRecommendation orderRecommendation = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
        orderRecommendation.SetRatkinOrder(ratkinOrder);
        orderLetter.Attachments = [orderRecommendation];
        OrderLetterBox.Instance.ReceiveLetter(orderLetter);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (quest.State == QuestState.EndedSuccess)
        {
            MercyQuestHandler.Instance.Notify_MercyQuestSucceed(quest);
        }
        SubFaction = null;
        ParentFaction = null;
    }
}