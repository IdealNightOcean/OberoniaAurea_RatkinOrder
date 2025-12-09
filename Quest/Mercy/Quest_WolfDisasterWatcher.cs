using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 狼灾任务监控 QuestNode（内部特化类）
/// </summary>
internal sealed class QuestNode_WolfDisasterWatcher : QuestNode
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

            InSignalAdvanced = QuestGenUtility.HardcodedSignalWithQuestID(inSignalAdvanced.GetValue(slate)),
            InSignalFailAdvanced = QuestGenUtility.HardcodedSignalWithQuestID(inSignalFailAdvanced.GetValue(slate)),
            InSignalReduce = QuestGenUtility.HardcodedSignalWithQuestID(inSignalReduce.GetValue(slate)),
            InSignalRemoveExtraPoint = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemoveExtraPoint.GetValue(slate)),
            InSignalRemoveGossipPoint = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemoveGossipPoint.GetValue(slate)),

            OutSignalDiscovered = QuestGenUtility.HardcodedSignalWithQuestID(outSignalDiscovered.GetValue(slate)),
            ExtraPointTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(extraPointTag.GetValue(slate)),
            GossipPointTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(gossipPointTag.GetValue(slate)),

            TargetCount = targetCount.GetValue(slate),
            CenterTile = slate.Get<Map>("map").Parent.Tile,
            Faction = faction.GetValue(slate),
            GossipPointDef = gossipPointDef.GetValue(slate)
        };
        IEnumerable<WorldObject> startPoints = this.startPoints.GetValue(slate);
        if (startPoints is not null)
        {
            questPart_WolfDisasterWatcher.DisasterPoints = [];
            foreach (WorldObject point in startPoints)
            {
                if (point is WorldObject_WolfDisasterPoint validPoint)
                {
                    questPart_WolfDisasterWatcher.DisasterPoints.Add(validPoint);
                }
            }
        }

        QuestGen.quest.AddPart(questPart_WolfDisasterWatcher);
    }
}

/// <summary>
/// 狼灾任务监控 QuestPartActivable（内部特化类）
/// </summary>
internal sealed class QuestPart_WolfDisasterWatcher : QuestPartActivable
{
    public string InSignalAdvanced;
    public string InSignalFailAdvanced;
    public string InSignalReduce;

    public string InSignalRemoveExtraPoint;
    public string InSignalRemoveGossipPoint;

    public string OutSignalDiscovered;
    public string ExtraPointTag;
    public string GossipPointTag;

    public int TargetCount;
    public PlanetTile CenterTile;

    private int validCount;
    public Faction Faction;
    public WorldObjectDef GossipPointDef;

    public List<WorldObject_WolfDisasterPoint> DisasterPoints;
    private List<WorldObject_WolfDisasterGossipPoint> gossipPoints;

    private int nextGossipTick;

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            if (DisasterPoints is not null)
            {
                foreach (WorldObject_WolfDisasterPoint point in DisasterPoints)
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

        Scribe_Values.Look(ref InSignalAdvanced, "InSignalAdvanced");
        Scribe_Values.Look(ref InSignalFailAdvanced, "InSignalFailAdvanced");
        Scribe_Values.Look(ref InSignalReduce, "InSignalReduce");

        Scribe_Values.Look(ref InSignalRemoveExtraPoint, "InSignalRemoveExtraPoint");
        Scribe_Values.Look(ref InSignalRemoveGossipPoint, "InSignalRemoveGossipPoint");

        Scribe_Values.Look(ref OutSignalDiscovered, "OutSignalDiscovered");
        Scribe_Values.Look(ref ExtraPointTag, "ExtraPointTag");
        Scribe_Values.Look(ref GossipPointTag, "GossipPointTag");

        Scribe_Values.Look(ref TargetCount, "TargetCount", 0);
        Scribe_Values.Look(ref validCount, "validCount", 0);

        Scribe_References.Look(ref Faction, "Faction");
        Scribe_Defs.Look(ref GossipPointDef, "GossipPointDef");

        Scribe_Collections.Look(ref DisasterPoints, "DisasterPoints", LookMode.Reference);
        Scribe_Collections.Look(ref gossipPoints, "gossipPoints", LookMode.Reference);

        Scribe_Values.Look(ref nextGossipTick, "nextGossipTick", 0);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            DisasterPoints?.RemoveAll(w => w is null || w.Destroyed);
            gossipPoints?.RemoveAll(w => w is null || w.Destroyed);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalAdvanced = null;
        InSignalFailAdvanced = null;
        InSignalReduce = null;

        InSignalRemoveExtraPoint = null;
        InSignalRemoveGossipPoint = null;

        OutSignalDiscovered = null;
        ExtraPointTag = null;
        GossipPointTag = null;

        Faction = null;
        GossipPointDef = null;

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
        if (signal.tag == InSignalAdvanced)
        {
            AddValidInfo();
        }
        else if (signal.tag == InSignalFailAdvanced)
        {
            WorldObject_WolfDisasterPoint disasterPoint = signal.args.GetArg<WorldObject_WolfDisasterPoint>(KeyLibrary_FormatArgName.SUBJECT);
            ExtraPoint(disasterPoint);
        }
        else if (signal.tag == InSignalReduce)
        {
            validCount = Mathf.Max(0, validCount - 1);
        }
        else if (DisasterPoints is not null && signal.tag == InSignalRemoveExtraPoint)
        {
            WorldObject_WolfDisasterPoint point = signal.args.GetArg<WorldObject_WolfDisasterPoint>(KeyLibrary_FormatArgName.SUBJECT);
            DisasterPoints.Remove(point);
        }
        else if (gossipPoints is not null && signal.tag == InSignalRemoveGossipPoint)
        {
            WorldObject_WolfDisasterGossipPoint gossipPoint = signal.args.GetArg<WorldObject_WolfDisasterGossipPoint>(KeyLibrary_FormatArgName.SUBJECT);
            gossipPoints.Remove(gossipPoint);
        }
    }

    private void AddValidInfo()
    {
        if ((++validCount) >= TargetCount)
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalDiscovered));
        }
    }

    private void ClearDisasterPoints()
    {
        if (DisasterPoints is not null)
        {
            foreach (WorldObject point in DisasterPoints)
            {
                point.SafeDestroy();
            }
            DisasterPoints = null;
        }

        if (gossipPoints is not null)
        {
            foreach (WorldObject_WolfDisasterGossipPoint gossipPoint in gossipPoints)
            {
                gossipPoint.SafeDestroy();
            }
            gossipPoints = null;
        }
    }

    private void ExtraPoint(WorldObject_WolfDisasterPoint disasterPoint)
    {
        if (DisasterPoints is null)
        {
            return;
        }

        OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile newTile, disasterPoint.Tile, minDist: 3, maxDist: 8);
        WorldObject_WolfDisasterPoint newWolfPoint = (WorldObject_WolfDisasterPoint)WorldObjectMaker.MakeWorldObject(disasterPoint.def);
        newWolfPoint.Tile = newTile;
        newWolfPoint.SetAssociatedQuest(quest);
        QuestUtility.AddQuestTag(newWolfPoint, ExtraPointTag);
        if (disasterPoint.questTags is not null)
        {
            newWolfPoint.questTags ??= [];
            newWolfPoint.questTags.AddRange(disasterPoint.questTags);
        }
        newWolfPoint.GetComponent<TimeoutComp>()?.StartTimeout(7 * 60000);
        DisasterPoints ??= [];
        DisasterPoints.Add(newWolfPoint);
        Find.WorldObjects.Add(newWolfPoint);
    }

    private void GossipPoint()
    {
        WorldObject_WolfDisasterGossipPoint gossipPoint = (WorldObject_WolfDisasterGossipPoint)WorldObjectMaker.MakeWorldObject(GossipPointDef);
        gossipPoint.SetAssociatedQuest(quest);
        QuestUtility.AddQuestTag(gossipPoint, GossipPointTag);
        OAFrame_TileFinderUtility.TryFindNewAvaliableTile(out PlanetTile pointTile, CenterTile, 3, 8);
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
                                       relatedFaction: Faction);
    }

    public override void DoDebugWindowContents(Rect innerRect, ref float curY)
    {
        if (State == QuestPartState.Enabled)
        {
            Rect rect = new(innerRect.x, curY, 500f, 25f);
            if (Widgets.ButtonText(rect, "+1 valid info" + ToString()))
            {
                AddValidInfo();
            }
            curY += rect.height + 4f;
        }
    }
}