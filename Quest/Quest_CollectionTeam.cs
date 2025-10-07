using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;


namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 物资收集补给队 QuestNode（特化类）
/// </summary>
public sealed class QuestNode_CollectionTeam : QuestNode
{
    public SlateRef<Type> questPartClass = typeof(QuestPart_CollectionTeam);

    public SlateRef<Branch> branch;
    public SlateRef<Faction> faction;
    public SlateRef<BranchDemand.DemandType?> demandType;

    public SlateRef<IEnumerable<ThingDefCountClass>> requestThingDefCounts;

    [NoTranslate]
    public SlateRef<string> inSignalEnable;
    [NoTranslate]
    public SlateRef<string> inSignalDisable = "CollectionTeam_Disable";
    [NoTranslate]
    public SlateRef<string> inSignalDisablePawnsArrival = "CollectionTeam_DisableArrival";
    [NoTranslate]
    public SlateRef<string> inSignalPawnsLeave = "CollectionTeam_MakePawnsLeave";
    [NoTranslate]
    public SlateRef<string> inSignalRemovePawn = "CollectionTeam_Negative";
    [NoTranslate]
    public SlateRef<string> inSignalLeftMap = "collectionTeam.LeftMap";

    [NoTranslate]
    public SlateRef<string> outSignalPawnsArrived = "CollectionTeam_Arrived";
    [NoTranslate]
    public SlateRef<string> outSignalGive = "CollectionTeam_Give";
    [NoTranslate]
    public SlateRef<string> outSignalRejectGive = "CollectionTeam_RejectGive";
    [NoTranslate]
    public SlateRef<string> outSignalDecided = "CollectionTeam_Decided";
    [NoTranslate]
    public SlateRef<string> outSignalGiveExpired = "CollectionTeam_GiveExpired";
    [NoTranslate]
    public SlateRef<string> outSignalAllLeftMap = "CollectionTeam_AllLeft";
    [NoTranslate]
    public SlateRef<string> outSignalAllLeftMapAndGive = "CollectionTeam_AllLeftAndGive";
    [NoTranslate]
    public SlateRef<string> outSignalFailureToCollect = "CollectionTeam_FailureToCollect";

    [NoTranslate]
    public SlateRef<string> pawnsTag = "collectionTeam";

    [MustTranslate]
    public SlateRef<string> talkText;

    public SlateRef<int> durationTicks = 30000;

    public SlateRef<MapParent> mapParent;

    public SlateRef<IsolatedPawnGroupMakerDef> isolatedPawnGroupMakerDef;

    protected override bool TestRunInt(Slate slate)
    {
        return questPartClass.GetValue(slate).IsSubclassOf(typeof(QuestPart_CollectionTeam));
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Type questPartClass = this.questPartClass.GetValue(slate);
        if (!questPartClass.IsSubclassOf(typeof(QuestPart_CollectionTeam)))
        {
            return;
        }

        if (!this.requestThingDefCounts.TryGetValue(slate, out IEnumerable<ThingDefCountClass> requestThingDefCounts))
        {
            return;
        }
        QuestPart_CollectionTeam questPart_CollectionTeam = (QuestPart_CollectionTeam)Activator.CreateInstance(questPartClass);

        questPart_CollectionTeam.Branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        questPart_CollectionTeam.DemandType = demandType.GetValue(slate) ?? slate.Get<BranchDemand.DemandType>(KeyLibrary_SlateStoreAs.DemandType);
        questPart_CollectionTeam.Faction = faction.GetValue(slate) ?? questPart_CollectionTeam.Branch?.RatkinOrder.Faction;

        questPart_CollectionTeam.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ?? slate.Get<string>("inSignal");
        questPart_CollectionTeam.inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisable.GetValue(slate)) ?? slate.Get<string>("inSignal");
        questPart_CollectionTeam.InSignalDisablePawnsArrival = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisablePawnsArrival.GetValue(slate));
        questPart_CollectionTeam.InSignalMakePawnsLeave = QuestGenUtility.HardcodedSignalWithQuestID(inSignalPawnsLeave.GetValue(slate));
        questPart_CollectionTeam.InSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemovePawn.GetValue(slate));
        questPart_CollectionTeam.InSignalLeftMap = QuestGenUtility.HardcodedSignalWithQuestID(inSignalLeftMap.GetValue(slate));

        questPart_CollectionTeam.OutSignalPawnsArrived = QuestGenUtility.HardcodedSignalWithQuestID(outSignalPawnsArrived.GetValue(slate));
        questPart_CollectionTeam.OutSignalGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalGive.GetValue(slate));
        questPart_CollectionTeam.OutSignalRejectGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalRejectGive.GetValue(slate));
        questPart_CollectionTeam.OutSignalDecided = QuestGenUtility.HardcodedSignalWithQuestID(outSignalDecided.GetValue(slate));
        questPart_CollectionTeam.OutSignalAllLeftMap = QuestGenUtility.HardcodedSignalWithQuestID(outSignalAllLeftMap.GetValue(slate));
        questPart_CollectionTeam.OutSignalAllLeftMapAndGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalAllLeftMapAndGive.GetValue(slate));
        questPart_CollectionTeam.OutSignalFailureToCollect = QuestGenUtility.HardcodedSignalWithQuestID(outSignalFailureToCollect.GetValue(slate));

        questPart_CollectionTeam.PawnsTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(pawnsTag.GetValue(slate));

        questPart_CollectionTeam.DurationTicks = durationTicks.GetValue(slate);
        questPart_CollectionTeam.TalkText = talkText.GetValue(slate);

        questPart_CollectionTeam.MapParent = mapParent.GetValue(slate) ?? slate.Get<Map>("map")?.Parent;
        questPart_CollectionTeam.PawnGroupMakerDef = isolatedPawnGroupMakerDef.GetValue(slate);

        questPart_CollectionTeam.InitRequestThingDefCounts(requestThingDefCounts);

        QuestGen.quest.AddPart(questPart_CollectionTeam);
    }
}

/// <summary>
/// 物资收集补给队 QuestPart（可继承）
/// </summary>
public class QuestPart_CollectionTeam : QuestPartActivable, IOnBranchDestroyed, ITalkAction
{
    protected List<ThingDefCountClass> requestThingDefCounts;

    public Branch Branch;
    public Faction Faction;
    public MapParent MapParent;
    public IsolatedPawnGroupMakerDef PawnGroupMakerDef;

    public BranchDemand.DemandType? DemandType;

    public int DurationTicks = 30000;
    public string TalkText;

    public string InSignalDisablePawnsArrival;
    public string InSignalMakePawnsLeave;
    public string InSignalRemovePawn;
    public string InSignalLeftMap;

    public string OutSignalPawnsArrived;
    public string OutSignalGive;
    public string OutSignalRejectGive;
    public string OutSignalDecided;
    public string OutSignalAllLeftMap;
    public string OutSignalAllLeftMapAndGive;
    public string OutSignalFailureToCollect;

    public string PawnsTag;

    protected bool canTryArrival = true;
    protected bool hasLeft;
    protected bool hasFulfilled;

    protected bool CanMakeLeave => !hasLeft && pawns is not null;

    public RatkinOrder RatkinOrder => Branch?.RatkinOrder;
    public Faction RelatedFaction => Faction ??= RatkinOrder?.Faction;

    protected Pawn talkWith;
    protected List<Pawn> pawns;
    public Pawn TalkWith => talkWith;

    public QuestPart_CollectionTeam() { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref requestThingDefCounts, "requestThingDefCounts", LookMode.Deep);

        Scribe_References.Look(ref Branch, "Branch");
        Scribe_References.Look(ref Faction, "Faction");
        Scribe_References.Look(ref MapParent, "MapParent");
        Scribe_Defs.Look(ref PawnGroupMakerDef, "PawnGroupMakerDef");
        Scribe_Values.Look(ref DemandType, "DemandType");

        Scribe_Values.Look(ref DurationTicks, "DurationTicks", 30000);
        Scribe_Values.Look(ref TalkText, "TalkText");

        Scribe_Values.Look(ref InSignalDisablePawnsArrival, "InSignalDisablePawnsArrival");
        Scribe_Values.Look(ref InSignalMakePawnsLeave, "InSignalMakePawnsLeave");
        Scribe_Values.Look(ref InSignalRemovePawn, "InSignalRemovePawn");
        Scribe_Values.Look(ref InSignalLeftMap, "InSignalLeftMap");

        Scribe_Values.Look(ref OutSignalPawnsArrived, "OutSignalPawnsArrived");
        Scribe_Values.Look(ref OutSignalGive, "OutSignalGive");
        Scribe_Values.Look(ref OutSignalRejectGive, "OutSignalRejectGive");
        Scribe_Values.Look(ref OutSignalDecided, "OutSignalDecided");
        Scribe_Values.Look(ref OutSignalAllLeftMap, "OutSignalAllLeftMap");
        Scribe_Values.Look(ref OutSignalAllLeftMapAndGive, "OutSignalAllLeftMapAndGive");
        Scribe_Values.Look(ref OutSignalFailureToCollect, "OutSignalFailureToCollect");

        Scribe_Values.Look(ref PawnsTag, "PawnsTag");

        Scribe_Values.Look(ref canTryArrival, "canTryArrival", defaultValue: true);
        Scribe_Values.Look(ref hasLeft, "hasLeft", defaultValue: false);
        Scribe_Values.Look(ref hasFulfilled, "hasFulfilled", defaultValue: false);


        Scribe_References.Look(ref talkWith, "talkWith");
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            requestThingDefCounts?.RemoveAll(item => item.thingDef is null || item.count <= 0);
            if (quest?.State == QuestState.Ongoing)
            {
                this.RegisterTalkAction();
            }
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();

        MakeLeave();
        this.DeregisterTalkAction();

        talkWith = null;
        pawns = null;
        requestThingDefCounts = null;

        Branch = null;
        Faction = null;
        MapParent = null;
        PawnGroupMakerDef = null;
        DemandType = default;

        DurationTicks = 30000;
        TalkText = null;

        InSignalDisablePawnsArrival = null;
        InSignalMakePawnsLeave = null;
        InSignalRemovePawn = null;
        InSignalLeftMap = null;

        OutSignalGive = null;
        OutSignalRejectGive = null;
        OutSignalDecided = null;
        OutSignalAllLeftMap = null;
        OutSignalAllLeftMapAndGive = null;
        OutSignalFailureToCollect = null;

        PawnsTag = null;
    }

    public void InitWithDefaultSignal()
    {
        inSignalEnable = QuestGen.slate.Get<string>("inSignal");
        inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Disable");
        InSignalDisablePawnsArrival = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_DisableArrival");
        InSignalMakePawnsLeave = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_MakePawnsLeave");
        InSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Negative");
        InSignalLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("collectionTeam.LeftMap");

        OutSignalPawnsArrived = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Arrived");
        OutSignalGive = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Give");
        OutSignalRejectGive = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_RejectGive");
        OutSignalPawnsArrived = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Arrived");
        OutSignalDecided = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_Decided");
        OutSignalAllLeftMap = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_AllLeft");
        OutSignalAllLeftMapAndGive = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_AllLeftAndGive");
        OutSignalFailureToCollect = QuestGenUtility.HardcodedSignalWithQuestID("CollectionTeam_FailureToCollect");

        PawnsTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID("collectionTeam");
    }

    public virtual void InitRequestThingDefCounts(IEnumerable<ThingDefCountClass> thingDefCounts)
    {
        if (thingDefCounts is null)
        {
            return;
        }

        requestThingDefCounts = [];
        requestThingDefCounts.AddRange(thingDefCounts);
    }

    public void AddRequestThingDefCount(ThingDefCountClass thingDefCount)
    {
        requestThingDefCounts ??= [];
        requestThingDefCounts.Add(thingDefCount);
    }
    public void AddRequestThingDefCounts(IEnumerable<ThingDefCountClass> thingDefCounts)
    {
        requestThingDefCounts ??= [];
        requestThingDefCounts.AddRange(thingDefCounts);
    }

    public void SetRequestThingDefCount(ThingDef def, int count, bool addIfMiss = true)
    {
        if (requestThingDefCounts is null)
        {
            if (addIfMiss && count > 0)
            {
                requestThingDefCounts = [new ThingDefCountClass(def, count)];
            }
            return;
        }

        int index = -1;
        for (int i = 0; i <= requestThingDefCounts.Count; i++)
        {
            if (requestThingDefCounts[i].thingDef == def)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            if (addIfMiss && count > 0)
            {
                requestThingDefCounts.Add(new ThingDefCountClass(def, count));
            }
        }
        else
        {
            if (count > 0)
            {
                requestThingDefCounts[index].count = count;
            }
            else
            {
                requestThingDefCounts.RemoveAt(index);
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);

        if (State != QuestPartState.Enabled && signal.tag == InSignalDisablePawnsArrival)
        {
            canTryArrival = false;
        }

        if (pawns is not null)
        {
            if (signal.tag == InSignalRemovePawn)
            {
                if (signal.args.TryGetArg("SUBJECT", out Pawn p))
                {
                    pawns.Remove(p);
                }
            }
            else if (signal.tag == InSignalLeftMap)
            {
                if (signal.args.TryGetArg("SUBJECT", out Pawn p))
                {
                    if (pawns.Remove(p) && pawns.Count == 0)
                    {
                        if (hasFulfilled)
                        {
                            Find.SignalManager.SendSignal(new Signal(OutSignalAllLeftMapAndGive));
                        }
                        Find.SignalManager.SendSignal(new Signal(OutSignalAllLeftMap));
                    }
                }
            }
        }
    }

    protected override void ProcessQuestSignal(Signal signal)
    {
        base.ProcessQuestSignal(signal);
        if (CanMakeLeave && signal.tag == InSignalMakePawnsLeave)
        {
            Disable();
        }
    }

    protected override void Enable(SignalArgs receivedArgs)
    {
        if (canTryArrival)
        {
            canTryArrival = false;
            base.Enable(receivedArgs);
            if (!TryGetTeamArrive())
            {
                Log.Error($"Failed to get team arrival in {nameof(QuestPart_CollectionTeam)}.");
                Disable();
                quest.End(QuestEndOutcome.Unknown, sendLetter: false, playSound: false);
            }
        }
    }

    protected override void Disable()
    {
        base.Disable();
        canTryArrival = false;
        if (CanMakeLeave)
        {
            MakeLeave();
        }
        hasLeft = true;
    }

    public override void QuestPartTick()
    {
        if (!hasLeft && Find.TickManager.TicksGame > enableTick + DurationTicks)
        {
            Disable();
        }
    }

    protected virtual bool TryGetTeamArrive()
    {
        MapParent = OAFrame_QuestUtility.GetAvailableMapParent(quest, MapParent);
        if (MapParent is null)
        {
            quest.End(QuestEndOutcome.Unknown, sendLetter: false, playSound: false);
        }
        Map map = MapParent.Map;
        pawns = GenerateCaravanMembers(map);
        if (pawns.NullOrEmpty())
        {
            return false;
        }

        IncidentParms arrivalParms = new()
        {
            target = map,
            faction = RelatedFaction,
            quest = quest
        };

        if (!ModUtility.TryMakePawnArrival(pawns, arrivalParms, PawnsArrivalModeDefOf.EdgeWalkIn))
        {
            return false;
        }

        Find.SignalManager.SendSignal(new Signal(OutSignalPawnsArrived));

        talkWith = SetTalkPawn();
        IntVec3 wanderCell = this.GetTalkPawnWanderCenterCell(nearOrderHall: true);

        LordJob_VisitColonyTalkable lordJob = new(RelatedFaction, wanderCell, DurationTicks);
        lordJob.SetTalkAction(talkWith, OARO_JobDefOf.OARO_Job_CommonTalkWith, initTalkActive: true);
        LordMaker.MakeNewLord(RelatedFaction, lordJob, map, pawns);

        this.RegisterTalkAction();

        return true;
    }

    protected virtual Pawn SetTalkPawn()
    {
        return pawns[0];
    }

    public virtual void TalkAction(Pawn talker, Pawn talkWith)
    {
        if (State != QuestPartState.Enabled)
        {
            Messages.Message("OARO_CollectionTeam_Leaving".Translate(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        Map map = talkWith?.Map ?? this.talkWith?.Map;
        if (map is null || requestThingDefCounts.NullOrEmpty())
        {
            return;
        }
        Find.WindowStack.Add(TalkNodeTree(talker, talkWith, map));
    }

    protected virtual List<Pawn> GenerateCaravanMembers(Map map)
    {
        if (RelatedFaction is null)
        {
            return null;
        }

        PawnGroupMakerDef.TryGetRandomPawnGroupMaker(PawnGroupKindDefOf.Peaceful, out PawnGroupMaker groupMaker);
        if (groupMaker is null)
        {
            return null;
        }

        List<Pawn> pawns = [];
        bool isKnight = Branch is not null;
        PlanetTile mapTile = map.Tile;
        for (int i = 0; i < groupMaker.options.Count; i++)
        {
            PawnKindDef pawnKind = groupMaker.options[i].kind;
            int pawnCount = (int)groupMaker.options[i].selectionWeight;
            for (int j = 0; j < pawnCount; j++)
            {
                Pawn pawn;
                if (isKnight)
                {
                    pawn = OARO_PawnUtility.GenerateOrderKnight(pawnKind, RatkinOrder, Branch, tile: mapTile);
                }
                else
                {
                    pawn = PawnGenerator.GeneratePawn(OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, RelatedFaction, tile: mapTile));
                }
                QuestUtility.AddQuestTag(pawn, PawnsTag);
                if (!pawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(pawn);
                }
                pawns.Add(pawn);
            }
        }

        if (pawns.NullOrEmpty())
        {
            return null;
        }

        return pawns;
    }

    protected void MakeLeave()
    {
        if (hasLeft)
        {
            return;
        }

        if (!hasFulfilled)
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalFailureToCollect));
        }
        TalkActionUtility.DisableLordJobTalk(TalkWith);
        if (pawns is not null)
        {
            foreach (Pawn pawn in pawns)
            {
                // pawn.pather.StopDead();
                pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedByQuest);
            }
            LeaveQuestPartUtility.MakePawnsLeave(pawns, sendLetter: true, quest, wakeUp: true);
        }
        hasLeft = true;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (Branch?.RatkinOrder is not null && RatkinOrder == order)
        {
            Branch = null;
            canTryArrival = false;
        }
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (Branch is not null && Branch == branch)
        {
            Branch = null;
            canTryArrival = false;
        }
    }

    protected AcceptanceReport CanGiveAllRequestThings(Map map)
    {
        if (map is null)
        {
            return false;
        }
        if (requestThingDefCounts.NullOrEmpty())
        {
            return true;
        }
        foreach (ThingDefCountClass thingDefCount in requestThingDefCounts)
        {
            if (!map.HasEnoughThingsOfDef(thingDefCount.thingDef, thingDefCount.count))
            {
                return "OAFrame_NeedCountOfThing".Translate(thingDefCount.thingDef.label, thingDefCount.count);
            }
        }
        return true;
    }

    protected virtual TaggedString GetTalkNodeText(Pawn talker, Pawn talkWith)
    {
        if (TalkText.NullOrEmpty())
        {
            return "OARO_CollectionTeam_DefaultTalkText".Translate(talker.Named("TALKER"), talkWith.Named("TALKWITH")) + "\n\n" + RequestThingsSummary();
        }
        return TalkText.Formatted(talker.Named("TALKER"), talkWith.Named("TALKWITH")) + "\n\n" + RequestThingsSummary();
    }

    protected Dialog_NodeTreeWithRatkinOrderInfo TalkNodeTree(Pawn talker, Pawn talkWith, Map map)
    {
        DiaNode rootNode = new(GetTalkNodeText(talker, talkWith));

        DiaOption giveOpt = new("OARO_CollectionTeam_GiveRequestThings".Translate())
        {
            action = delegate
            {
                GiveAction(talker, talkWith, map);
            },
            resolveTree = true,
        };
        AcceptanceReport acceptanceReport = CanGiveAllRequestThings(map);
        if (!acceptanceReport.Accepted)
        {
            giveOpt.Disable(acceptanceReport.Reason);
        }

        DiaOption rejectOpt = new("OARO_CollectionTeam_RejectGive".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalRejectGive));
                PostMakeDecision();
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

    protected virtual void GiveAction(Pawn talker, Pawn talkWith, Map map)
    {
        hasFulfilled = true;
        foreach (ThingDefCountClass thingDefCount in requestThingDefCounts)
        {
            if (thingDefCount.count <= 0)
            {
                continue;
            }
            map.DestoryThingsOfDef(thingDefCount.thingDef, thingDefCount.count);
        }
        requestThingDefCounts.Clear();
        Find.SignalManager.SendSignal(new Signal(OutSignalGive));
        PostMakeDecision();
    }

    protected void PostMakeDecision()
    {
        Find.SignalManager.SendSignal(new Signal(OutSignalDecided));
        Disable();
    }

    public override void DoDebugWindowContents(Rect innerRect, ref float curY)
    {
        if (State == QuestPartState.Enabled)
        {
            Rect rect = new(innerRect.x, curY, 500f, 25f);
            if (Widgets.ButtonText(rect, "End " + ToString()))
            {
                Disable();
            }
            curY += rect.height + 4f;
        }
    }

    protected string RequestThingsSummary()
    {
        if (requestThingDefCounts.NullOrEmpty())
        {
            return "None".Translate();
        }

        StringBuilder sb = new();
        foreach (ThingDefCountClass thingDefCount in requestThingDefCounts)
        {
            sb.AppendInNewLine(thingDefCount.Summary);
        }
        return sb.ToString();
    }
}