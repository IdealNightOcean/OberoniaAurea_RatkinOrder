using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_OrderRecommendation : QuestNode
{
    public SlateRef<string> inSignal;

    public SlateRef<RatkinOrder> order;
    public SlateRef<int> count;
    public SlateRef<bool> giveToCaravan;
    public SlateRef<WorldObject> worldObject;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        RatkinOrder order = this.order.GetValue(slate);
        int recommendationCount = count.GetValue(slate);
        if (order is null || recommendationCount <= 0)
        {
            return;
        }

        Quest quest = QuestGen.quest;

        QuestPart_OrderRecommendation questPart_OrderRecommendation = new()
        {
            inSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>("inSignal"),
            order = order ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrderStoreAs),
            count = recommendationCount,
            mapParent = slate.Get<Map>("map")?.Parent,
            worldObject = worldObject.GetValue(slate),
            giveToCaravan = giveToCaravan.GetValue(slate),
        };

        quest.AddPart(questPart_OrderRecommendation);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_OrderRecommendation reward = new()
            {
                order = order,
                count = recommendationCount,
                mapParent = slate.Get<Map>("map")?.Parent,
                giveToCaravan = giveToCaravan.GetValue(slate),
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_OrderRecommendation.inSignalTrigger,
                };

                questPart_Choice.choices.Add(new QuestPart_Choice.Choice() { rewards = [reward] });
                quest.AddPart(questPart_Choice);
            }
            else
            {
                questPart_Choice = quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrFallback(null);
                if (questPart_Choice is not null)
                {
                    foreach (QuestPart_Choice.Choice singelChoice in questPart_Choice.choices)
                    {
                        singelChoice.rewards.Add(reward);
                    }
                }
            }
        }
    }
}

public class QuestPart_OrderRecommendation : QuestPart, IRatkinOrderRelated
{
    public string inSignalTrigger;
    public RatkinOrder order;
    public int count;

    public WorldObject worldObject;
    public MapParent mapParent;
    public bool giveToCaravan;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignalTrigger, "inSignalTrigger");
        Scribe_References.Look(ref order, "order");
        Scribe_Values.Look(ref count, "count", 0);
        Scribe_References.Look(ref worldObject, "worldObject");
        Scribe_References.Look(ref mapParent, "mapParent");
        Scribe_Values.Look(ref giveToCaravan, "giveToCaravan", defaultValue: false);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalTrigger = null;
        order = null;
        count = 0;
        worldObject = null;
        mapParent = null;
        giveToCaravan = false;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == inSignalTrigger)
        {
            if (order is null)
            {
                return;
            }

            if (giveToCaravan && GetCaravan(signal, out Caravan caravan))
            {
                RecommendationUtility.GiveRecommendationsToPlayer(
                    order: order,
                    count: count,
                    giveAction: delegate (Thing t)
                    {
                        CaravanInventoryUtility.GiveThing(caravan, t);
                    });
            }
            else
            {
                mapParent = OAFrame_QuestUtility.GetAvailableMapParent(quest, mapParent);
                if (mapParent is not null)
                {
                    RecommendationUtility.GiveRecommendationsToPlayer_Map(order, count, mapParent.Map, spawnCell: null, drop: true);
                }
            }
        }
    }

    private bool GetCaravan(Signal signal, out Caravan caravan)
    {
        signal.args.TryGetArg("CARAVAN", out caravan);
        if (caravan is not null)
        {
            Log.Message("get caravan");
            return true;
        }

        if (worldObject is not null && worldObject.Spawned)
        {
            caravan = Find.WorldObjects.Caravans?.Where(c => c.Tile == worldObject.Tile).FirstOrFallback(null);
            if (caravan is not null)
            {
                return true;
            }
        }

        caravan = Find.WorldObjects.Caravans?.RandomElementWithFallback(null);
        return caravan is not null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (this.order == order)
        {
            this.order = null;
        }
    }
}