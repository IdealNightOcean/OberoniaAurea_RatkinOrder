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

    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<int> count;
    public SlateRef<bool> giveToCaravan;
    public SlateRef<WorldObject> worldObject;

    public SlateRef<bool> isReward;
    public SlateRef<bool> isSingleReward;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate);
        int recommendationCount = count.GetValue(slate);
        if (recommendationCount <= 0)
        {
            return;
        }

        QuestPart_OrderRecommendation questPart_OrderRecommendation = new()
        {
            InSignalTrigger = inSignal.GetValue(slate) ?? slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            RatkinOrder = ratkinOrder ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.ratkinOrder),
            Count = recommendationCount,
            MapParent = slate.Get<Map>("map")?.Parent,
            WorldObject = worldObject.GetValue(slate),
            GiveToCaravan = giveToCaravan.GetValue(slate),
        };

        QuestGen.quest.AddPart(questPart_OrderRecommendation);

        if (isReward.GetValue(slate))
        {
            QuestPart_Choice questPart_Choice;
            Reward_OrderRecommendation reward = new()
            {
                RatkinOrder = ratkinOrder,
                Count = recommendationCount,
                MapParent = questPart_OrderRecommendation.MapParent,
                GiveToCaravan = questPart_OrderRecommendation.GiveToCaravan
            };

            if (isSingleReward.GetValue(slate))
            {
                questPart_Choice = new QuestPart_Choice()
                {
                    inSignalChoiceUsed = questPart_OrderRecommendation.InSignalTrigger,
                };

                questPart_Choice.choices.Add(new QuestPart_Choice.Choice() { rewards = [reward] });
                QuestGen.quest.AddPart(questPart_Choice);
            }
            else
            {
                questPart_Choice = QuestGen.quest.PartsListForReading.OfType<QuestPart_Choice>().FirstOrFallback(null);
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

public class QuestPart_OrderRecommendation : QuestPart, IOnRatkinOrderRemoved
{
    public string InSignalTrigger;
    public RatkinOrder RatkinOrder;
    public int Count;

    public WorldObject WorldObject;
    public MapParent MapParent;
    public bool GiveToCaravan;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalTrigger, "InSignalTrigger");
        Scribe_References.Look(ref RatkinOrder, "RatkinOrder");
        Scribe_Values.Look(ref Count, "Count", 0);
        Scribe_References.Look(ref WorldObject, "WorldObject");
        Scribe_References.Look(ref MapParent, "MapParent");
        Scribe_Values.Look(ref GiveToCaravan, "GiveToCaravan", defaultValue: false);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalTrigger = null;
        RatkinOrder = null;
        Count = 0;
        WorldObject = null;
        MapParent = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag == InSignalTrigger)
        {
            if (GiveToCaravan && GetCaravan(signal, out Caravan caravan))
            {
                RecommendationUtility.GiveRecommendationsToCaravan(caravan, Count);
            }
            else
            {
                MapParent = OAFrame_QuestUtility.GetAvailableMapParent(quest, MapParent);
                if (MapParent is not null)
                {
                    RecommendationUtility.GiveRecommendationsToPlayerMap(MapParent.Map, count: Count, sendStandLetter: true, ratkinOrder: RatkinOrder, dropPod: true);
                }
            }
        }
    }

    private bool GetCaravan(Signal signal, out Caravan caravan)
    {
        signal.args.TryGetArg("CARAVAN", out caravan);
        if (caravan is not null)
        {
            return true;
        }

        if (WorldObject is not null && WorldObject.Spawned)
        {
            caravan = Find.WorldObjects.Caravans?.Where(c => c.Tile == WorldObject.Tile).FirstOrFallback(null);
            if (caravan is not null)
            {
                return true;
            }
        }

        caravan = Find.WorldObjects.Caravans?.RandomElementWithFallback(null);
        return caravan is not null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (RatkinOrder == ratkinOrder)
        {
            RatkinOrder = null;
        }
    }
}