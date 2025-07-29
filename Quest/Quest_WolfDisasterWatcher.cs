using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_WolfDisasterWatcher : QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSignalEnable;
    [NoTranslate]
    public SlateRef<string> inSignalDisable;

    [NoTranslate]
    public SlateRef<string> inSignalAdvanced;
    [NoTranslate]
    public SlateRef<string> inSignalFailAdvanced;
    [NoTranslate]
    public SlateRef<string> inSignalReduce;
    [NoTranslate]
    public SlateRef<string> inSignalRemoveExtraPoint;
    [NoTranslate]
    public SlateRef<string> inSignalRemoveGossipPoint;

    [NoTranslate]
    public SlateRef<string> outSignalDiscovered;
    [NoTranslate]
    public SlateRef<string> extraPointTag;
    [NoTranslate]
    public SlateRef<string> gossipPointTag;

    public SlateRef<int> targetCount;

    public SlateRef<Faction> faction;

    public SlateRef<WorldObjectDef> gossipPointDef;
    public SlateRef<IEnumerable<WorldObject>> startPoints;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_WolfDisasterWatcher questPart_WolfDisasterWatcher = new()
        {
            inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ?? slate.Get<string>("inSignal"),
            inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisable.GetValue(slate)),

            inSignalAdvanced = QuestGenUtility.HardcodedSignalWithQuestID(inSignalAdvanced.GetValue(slate)),
            inSignalFailAdvanced = QuestGenUtility.HardcodedSignalWithQuestID(inSignalFailAdvanced.GetValue(slate)),
            inSignalReduce = QuestGenUtility.HardcodedSignalWithQuestID(inSignalReduce.GetValue(slate)),
            inSignalRemoveExtraPoint = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemoveExtraPoint.GetValue(slate)),
            inSignalRemoveGossipPoint = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemoveGossipPoint.GetValue(slate)),

            outSignalDiscovered = QuestGenUtility.HardcodedSignalWithQuestID(outSignalDiscovered.GetValue(slate)),
            extraPointTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(extraPointTag.GetValue(slate)),
            gossipPointTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(gossipPointTag.GetValue(slate)),

            targetCount = targetCount.GetValue(slate),
            centerTile = slate.Get<Map>("map").Parent.Tile,
            faction = faction.GetValue(slate),
            gossipPointDef = gossipPointDef.GetValue(slate)
        };
        IEnumerable<WorldObject> startPoints = this.startPoints.GetValue(slate);
        if (startPoints is not null)
        {
            questPart_WolfDisasterWatcher.disasterPoints = [];
            foreach (WorldObject point in startPoints)
            {
                if (point is WorldObject_WolfDisasterPoint validPoint)
                {
                    questPart_WolfDisasterWatcher.disasterPoints.Add(validPoint);
                }
            }
        }

        QuestGen.quest.AddPart(questPart_WolfDisasterWatcher);
    }
}

public class QuestPart_WolfDisasterWatcher : QuestPartActivable
{
    public string inSignalAdvanced;
    public string inSignalFailAdvanced;
    public string inSignalReduce;

    public string inSignalRemoveExtraPoint;
    public string inSignalRemoveGossipPoint;

    public string outSignalDiscovered;
    public string extraPointTag;
    public string gossipPointTag;

    public int targetCount;
    public PlanetTile centerTile;

    private int validCount;
    public Faction faction;
    public WorldObjectDef gossipPointDef;

    public List<WorldObject_WolfDisasterPoint> disasterPoints;
    private List<WorldObject_WolfDisasterGossipPoint> gossipPoints;

    private int nextGossipTick;

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            if (disasterPoints is not null)
            {
                foreach (WorldObject_WolfDisasterPoint point in disasterPoints)
                {
                    if (point.Spawned)
                    {
                        yield return point;
                    }
                }
            }
            if (gossipPoints is not null)
            {
                foreach (WorldObject_WolfDisasterGossipPoint point in gossipPoints)
                {
                    if (point.Spawned)
                    {
                        yield return point;
                    }
                }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref inSignalAdvanced, "inSignalAdvanced");
        Scribe_Values.Look(ref inSignalFailAdvanced, "inSignalFailAdvanced");
        Scribe_Values.Look(ref inSignalReduce, "inSignalReduce");

        Scribe_Values.Look(ref inSignalRemoveExtraPoint, "inSignalRemoveExtraPoint");
        Scribe_Values.Look(ref inSignalRemoveGossipPoint, "inSignalRemoveGossipPoint");

        Scribe_Values.Look(ref outSignalDiscovered, "outSignalDiscovered");
        Scribe_Values.Look(ref extraPointTag, "extraPointTag");
        Scribe_Values.Look(ref gossipPointTag, "gossipPointTag");

        Scribe_Values.Look(ref targetCount, "targetCount", 0);
        Scribe_Values.Look(ref validCount, "validCount", 0);

        Scribe_References.Look(ref faction, "faction");
        Scribe_Defs.Look(ref gossipPointDef, "gossipPointDef");

        Scribe_Collections.Look(ref disasterPoints, "extraPoints", LookMode.Reference);
        Scribe_Collections.Look(ref gossipPoints, "gossipPoints", LookMode.Reference);

        Scribe_Values.Look(ref nextGossipTick, "nextGossipTick", 0);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            disasterPoints?.RemoveAll(w => w is null || w.Destroyed);
            gossipPoints?.RemoveAll(w => w is null || w.Destroyed);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        inSignalAdvanced = null;
        inSignalFailAdvanced = null;
        inSignalReduce = null;

        inSignalRemoveExtraPoint = null;
        inSignalRemoveGossipPoint = null;

        outSignalDiscovered = null;
        extraPointTag = null;
        gossipPointTag = null;

        faction = null;
        gossipPointDef = null;

        ClearDisasterPoints();
    }

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        nextGossipTick = Find.TickManager.TicksGame + Rand.RangeInclusive(30000, 60000);
    }

    protected override void Disable()
    {
        base.Disable();
        ClearDisasterPoints();
    }

    public override void QuestPartTick()
    {
        base.QuestPartTick();
        if (State == QuestPartState.Enabled && Find.TickManager.TicksGame > nextGossipTick)
        {
            nextGossipTick = Find.TickManager.TicksGame + Rand.RangeInclusive(30000, 60000);
            GossipPoint();
        }
    }

    protected override void ProcessQuestSignal(Signal signal)
    {
        base.ProcessQuestSignal(signal);
        if (signal.tag == inSignalAdvanced)
        {
            if (++validCount >= targetCount)
            {
                Find.SignalManager.SendSignal(new Signal(outSignalDiscovered));
            }
        }
        else if (signal.tag == inSignalFailAdvanced)
        {
            WorldObject_WolfDisasterPoint disasterPoint = signal.args.GetArg<WorldObject_WolfDisasterPoint>("SUBJECT");
            ExtraPoint(disasterPoint);
        }
        else if (signal.tag == inSignalReduce)
        {
            validCount = Mathf.Max(0, validCount - 1);
        }
        else if (disasterPoints is not null && signal.tag == inSignalRemoveExtraPoint)
        {
            WorldObject_WolfDisasterPoint point = signal.args.GetArg<WorldObject_WolfDisasterPoint>("SUBJECT");
            disasterPoints.Remove(point);
        }
        else if (gossipPoints is not null && signal.tag == inSignalRemoveGossipPoint)
        {
            WorldObject_WolfDisasterGossipPoint gossipPoint = signal.args.GetArg<WorldObject_WolfDisasterGossipPoint>("SUBJECT");
            gossipPoints.Remove(gossipPoint);
        }
    }

    private void ClearDisasterPoints()
    {
        if (disasterPoints is not null)
        {
            foreach (WorldObject point in disasterPoints)
            {
                if (point is not null && !point.Destroyed)
                {
                    point.Destroy();
                }
            }
            disasterPoints = null;
        }

        if (gossipPoints is not null)
        {
            foreach (WorldObject_WolfDisasterGossipPoint gossipPoint in gossipPoints)
            {
                if (gossipPoint is not null && !gossipPoint.Destroyed)
                {
                    gossipPoint.Destroy();
                }
            }
            gossipPoints = null;
        }
    }

    private void ExtraPoint(WorldObject_WolfDisasterPoint disasterPoint)
    {
        if (disasterPoints is null)
        {
            return;
        }

        OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile newTile, disasterPoint.Tile, minDist: 3, maxDist: 8);
        WorldObject_WolfDisasterPoint newWolfPoint = (WorldObject_WolfDisasterPoint)WorldObjectMaker.MakeWorldObject(disasterPoint.def);
        newWolfPoint.Tile = newTile;
        newWolfPoint.SetAssociatedQuest(quest);
        QuestUtility.AddQuestTag(newWolfPoint, extraPointTag);
        if (disasterPoint.questTags is not null)
        {
            newWolfPoint.questTags ??= [];
            newWolfPoint.questTags.AddRange(disasterPoint.questTags);
        }
        newWolfPoint.GetComponent<TimeoutComp>()?.StartTimeout(7 * 60000);
        disasterPoints ??= [];
        disasterPoints.Add(newWolfPoint);
        Find.WorldObjects.Add(newWolfPoint);
    }

    private void GossipPoint()
    {
        WorldObject_WolfDisasterGossipPoint gossipPoint = (WorldObject_WolfDisasterGossipPoint)WorldObjectMaker.MakeWorldObject(gossipPointDef);
        gossipPoint.SetAssociatedQuest(quest);
        QuestUtility.AddQuestTag(gossipPoint, gossipPointTag);
        OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile pointTile, centerTile, 3, 8);
        gossipPoint.Tile = pointTile;
        gossipPoint.GetComponent<TimeoutComp>()?.StartTimeout(3 * 60000);
        Find.WorldObjects.Add(gossipPoint);

        gossipPoints ??= [];
        gossipPoints.Add(gossipPoint);

        Find.LetterStack.ReceiveLetter(label: "OARO_LetterLabel_WolfDisasterGossip".Translate(),
                                       text: "OARO_Letter_WolfDisasterGossip".Translate(),
                                       textLetterDef: LetterDefOf.NeutralEvent,
                                       lookTargets: gossipPoint,
                                       quest: quest,
                                       relatedFaction: faction);
    }
}