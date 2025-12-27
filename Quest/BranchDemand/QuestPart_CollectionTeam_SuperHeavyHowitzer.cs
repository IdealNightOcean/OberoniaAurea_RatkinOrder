using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 需求-榴弹炮维护收集队伍 QuestPart | TalkAction（内部特化类）
/// </summary>
internal sealed class QuestPart_CollectionTeam_SuperHeavyHowitzer : QuestPart_CollectionTeam
{
    private string outSignalRepaired;
    private string outSignalPerfectRepaired;

    public override void InitRequestThingDefCounts(IEnumerable<ThingDefCountClass> thingDefCounts)
    {
        outSignalRepaired ??= QuestGenUtility.HardcodedSignalWithQuestID("Howitzer_Repaired");
        outSignalPerfectRepaired ??= QuestGenUtility.HardcodedSignalWithQuestID("Howitzer_PerfectRepaired");
        base.InitRequestThingDefCounts(thingDefCounts);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref outSignalRepaired, "outSignalRepaired");
        Scribe_Values.Look(ref outSignalPerfectRepaired, "outSignalPerfectRepaired");
    }

    public override void Cleanup()
    {
        base.Cleanup();
        outSignalRepaired = null;
        outSignalPerfectRepaired = null;
    }

    public override void TalkAction(Pawn talker, Pawn talkWith)
    {
        Map map = talkWith?.Map ?? this.talkWith?.Map;
        if (map is null || requestThingDefCounts.NullOrEmpty())
        {
            return;
        }
        Find.WindowStack.Add(TalkNodeTree(talker, talkWith, map));
    }

    private new Dialog_NodeTreeWithRatkinOrderInfo TalkNodeTree(Pawn talker, Pawn talkWith, Map map)
    {
        DiaNode rootNode = new(RawTalkText.Formatted(talker.Named(KeyLibrary_FormatArgName.TALKER), talkWith.Named(KeyLibrary_FormatArgName.TALKWITH)));

        DiaOption giveOpt = new("OARO_GiveRequestThings".Translate())
        {
            action = delegate
            {
                GiveAction(talker, talkWith, map);
            },
            resolveTree = true,
        };

        if (!CanGiveHowitzer(map))
        {
            giveOpt.Disable("OAFrame_NeedCountOfThing".Translate(OARO_ThingDefOf.OARO_Turret_OrderSuperHeavyHowitzer.label, 2));
        }

        DiaOption rejectOpt = new("OARO_RejectGive".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalRejectGive));
                MakeLeave();
            },
            resolveTree = true,
        };
        DiaOption waitOpt = new("PostponeLetter".Translate())
        {
            resolveTree = true,
        };

        rootNode.options.Add(giveOpt);
        rootNode.options.Add(rejectOpt);
        rootNode.options.Add(waitOpt);

        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(rootNode, RatkinOrder);
        return nodeTree;
    }

    private AcceptanceReport CanGiveHowitzer(Map map)
    {
        ThingDef requestDef = requestThingDefCounts[0].thingDef;
        int requestCount = requestThingDefCounts[0].count;

        List<Thing> takeThings = OAFrame_MapUtility.TakeThingsOfDef(map, requestDef, requestCount, out int actualTakeCount);
        int remainingCount = requestCount - actualTakeCount;
        if (remainingCount <= 0)
        {
            return true;
        }
        List<Thing> miniThings = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing));
        for (int i = 0; i < miniThings.Count; i++)
        {
            if (miniThings[i].GetInnerIfMinified()?.def == OARO_ThingDefOf.OARO_Turret_OrderSuperHeavyHowitzer)
            {
                remainingCount--;
                takeThings.Add(miniThings[i]);
                if (remainingCount <= 0)
                {
                    break;
                }
            }
        }
        return remainingCount <= 0;
    }

    protected override void GiveAction(Pawn talker, Pawn talkWith, Map map)
    {
        ThingDef requestDef = requestThingDefCounts[0].thingDef;
        int requestCount = requestThingDefCounts[0].count;

        List<Thing> takeThings = OAFrame_MapUtility.TakeThingsOfDef(map, requestDef, requestCount, out int actualTakeCount);
        List<Thing> toCheck = [];
        toCheck.AddRange(takeThings);

        int remainingCount = requestCount - actualTakeCount;
        if (remainingCount > 0)
        {
            List<Thing> miniThings = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing));
            for (int i = 0; i < miniThings.Count; i++)
            {
                if (miniThings[i].GetInnerIfMinified()?.def == requestDef)
                {
                    remainingCount--;
                    takeThings.Add(miniThings[i]);
                    toCheck.Add(miniThings[i].GetInnerIfMinified());
                    if (remainingCount <= 0)
                    {
                        break;
                    }
                }
            }
        }
        bool repaired = true;
        bool perfectRepaired = true;

        foreach (Thing checkT in toCheck)
        {
            CompSuperHeavyHowitzer howitzerComp = checkT.TryGetComp<CompSuperHeavyHowitzer>();
            if (howitzerComp is null || !howitzerComp.Repaired)
            {
                repaired = false;
                perfectRepaired = false;
                break;
            }
            if (!howitzerComp.PerfectRepaired)
            {
                perfectRepaired = false;

            }
        }

        if (repaired)
        {
            hasFulfilled = true;
            if (perfectRepaired)
            {
                Find.SignalManager.SendSignal(new Signal(outSignalPerfectRepaired));
            }
            Find.SignalManager.SendSignal(new Signal(outSignalRepaired));
        }
        else
        {
            hasFulfilled = false;
            Find.SignalManager.SendSignal(new Signal(OutSignalFailureToCollect));
        }

        foreach (Thing t in takeThings)
        {
            t.Destroy();
        }

        requestThingDefCounts.Clear();
        Find.SignalManager.SendSignal(new Signal(OutSignalGive));
        PostMakeDecision();
    }
}
