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
using Verse.Grammar;


namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 物资收集补给队 QuestNode（特化类）
/// </summary>
public class QuestNode_CollectionTeam : QuestNode
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
    [NoTranslate]
    public SlateRef<string> teamLeaderTag = "teamLeader";

    public SlateRef<int> durationTicks = 30000;

    public SlateRef<IsolatedPawnGroupMakerDef> isolatedPawnGroupMakerDef;

    protected override bool TestRunInt(Slate slate) => questPartClass.GetValue(slate).IsSubclassOf(typeof(QuestPart_CollectionTeam));

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Type questPartClass = this.questPartClass.GetValue(slate);
        if (questPartClass != typeof(QuestPart_CollectionTeam) && !questPartClass.IsSubclassOf(typeof(QuestPart_CollectionTeam)))
        {
            Log.Error($"[OARO] {nameof(this.questPartClass)} 不是 {nameof(QuestPart_CollectionTeam)} 或其子类。");
            return;
        }

        if (!this.requestThingDefCounts.TryGetValue(slate, out IEnumerable<ThingDefCountClass> requestThingDefCounts))
        {
            Log.Error($"[OARO] {nameof(this.requestThingDefCounts)} 为null或为空集合。");
            return;
        }
        Map map = slate.Get<Map>("map");

        QuestPart_CollectionTeam questPart_CollectionTeam = (QuestPart_CollectionTeam)Activator.CreateInstance(questPartClass);

        questPart_CollectionTeam.Branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.branch);
        questPart_CollectionTeam.DemandType = demandType.GetValue(slate) ?? slate.Get<BranchDemand.DemandType>(KeyLibrary_SlateStoreAs.demandType);
        questPart_CollectionTeam.Faction = faction.GetValue(slate) ?? questPart_CollectionTeam.Branch?.RatkinOrder.Faction;

        questPart_CollectionTeam.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable.GetValue(slate)) ?? slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal);
        questPart_CollectionTeam.inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisable.GetValue(slate));
        questPart_CollectionTeam.InSignalDisablePawnsArrival = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisablePawnsArrival.GetValue(slate));
        questPart_CollectionTeam.InSignalMakePawnsLeave = QuestGenUtility.HardcodedSignalWithQuestID(inSignalPawnsLeave.GetValue(slate));
        questPart_CollectionTeam.InSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemovePawn.GetValue(slate));
        questPart_CollectionTeam.InSignalLeftMap = QuestGenUtility.HardcodedSignalWithQuestID(inSignalLeftMap.GetValue(slate));

        questPart_CollectionTeam.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

        questPart_CollectionTeam.OutSignalPawnsArrived = QuestGenUtility.HardcodedSignalWithQuestID(outSignalPawnsArrived.GetValue(slate));
        questPart_CollectionTeam.OutSignalGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalGive.GetValue(slate));
        questPart_CollectionTeam.OutSignalRejectGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalRejectGive.GetValue(slate));
        questPart_CollectionTeam.OutSignalDecided = QuestGenUtility.HardcodedSignalWithQuestID(outSignalDecided.GetValue(slate));
        questPart_CollectionTeam.OutSignalAllLeftMap = QuestGenUtility.HardcodedSignalWithQuestID(outSignalAllLeftMap.GetValue(slate));
        questPart_CollectionTeam.OutSignalAllLeftMapAndGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalAllLeftMapAndGive.GetValue(slate));
        questPart_CollectionTeam.OutSignalFailureToCollect = QuestGenUtility.HardcodedSignalWithQuestID(outSignalFailureToCollect.GetValue(slate));

        questPart_CollectionTeam.DurationTicks = durationTicks.GetValue(slate);

        questPart_CollectionTeam.MapParent = map?.Parent;

        IsolatedPawnGroupMakerDef pawnGroupMakerDef = isolatedPawnGroupMakerDef.GetValue(slate);
        List<Pawn> collectionTeam = QuestPart_CollectionTeam.GenerateCaravanMembers(pawnGroupMakerDef, questPart_CollectionTeam.Faction, map, questPart_CollectionTeam.Branch);
        if (!collectionTeam.NullOrEmpty())
        {
            questPart_CollectionTeam.Pawns = [.. collectionTeam];
            questPart_CollectionTeam.talkWith = collectionTeam[0];

            slate.Set(pawnsTag.GetValue(slate), collectionTeam);
            slate.Set(teamLeaderTag.GetValue(slate), collectionTeam[0]);
        }

        questPart_CollectionTeam.InitTalkTextRequest("[collectionTeamArrivalText]", null);
        questPart_CollectionTeam.InitRequestThingDefCounts(requestThingDefCounts);

        QuestGen.quest.AddPart(questPart_CollectionTeam);

        QuestGen.quest.Letter(
            LetterDefOf.PositiveEvent,
            inSignal: questPart_CollectionTeam.inSignalEnable,
            relatedFaction: questPart_CollectionTeam.Faction,
            lookTargets: collectionTeam,
            label: "[collectionTeamArrivalLetterLabel]",
            text: "[collectionTeamArrivalLetterText]");
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

    public BranchDemand.DemandType? DemandType;

    public int DurationTicks = 30000;
    protected string RawTalkText;

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

    public Pawn talkWith;
    public List<Pawn> Pawns;
    public Pawn TalkWith => talkWith;

    protected bool canTryArrival = true;
    protected bool hasLeft;
    protected bool hasFulfilled;

    protected bool CanMakeLeave => !hasLeft && Pawns is not null;

    public RatkinOrder RatkinOrder => Branch?.RatkinOrder;
    public Faction RelatedFaction => Faction ??= RatkinOrder?.Faction;

    public QuestPart_CollectionTeam() { }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref requestThingDefCounts, nameof(requestThingDefCounts), LookMode.Deep);

        Scribe_References.Look(ref Branch, nameof(Branch));
        Scribe_References.Look(ref Faction, nameof(Faction));
        Scribe_References.Look(ref MapParent, nameof(MapParent));
        Scribe_Values.Look(ref DemandType, nameof(DemandType));

        Scribe_Values.Look(ref DurationTicks, nameof(DurationTicks), 30000);
        Scribe_Values.Look(ref RawTalkText, nameof(RawTalkText));

        Scribe_Values.Look(ref InSignalDisablePawnsArrival, nameof(InSignalDisablePawnsArrival));
        Scribe_Values.Look(ref InSignalMakePawnsLeave, nameof(InSignalMakePawnsLeave));
        Scribe_Values.Look(ref InSignalRemovePawn, nameof(InSignalRemovePawn));
        Scribe_Values.Look(ref InSignalLeftMap, nameof(InSignalLeftMap));

        Scribe_Values.Look(ref OutSignalPawnsArrived, nameof(OutSignalPawnsArrived));
        Scribe_Values.Look(ref OutSignalGive, nameof(OutSignalGive));
        Scribe_Values.Look(ref OutSignalRejectGive, nameof(OutSignalRejectGive));
        Scribe_Values.Look(ref OutSignalDecided, nameof(OutSignalDecided));
        Scribe_Values.Look(ref OutSignalAllLeftMap, nameof(OutSignalAllLeftMap));
        Scribe_Values.Look(ref OutSignalAllLeftMapAndGive, nameof(OutSignalAllLeftMapAndGive));
        Scribe_Values.Look(ref OutSignalFailureToCollect, nameof(OutSignalFailureToCollect));

        Scribe_Values.Look(ref canTryArrival, nameof(canTryArrival), defaultValue: true);
        Scribe_Values.Look(ref hasLeft, nameof(hasLeft), defaultValue: false);
        Scribe_Values.Look(ref hasFulfilled, nameof(hasFulfilled), defaultValue: false);

        Scribe_References.Look(ref talkWith, nameof(talkWith));
        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            requestThingDefCounts?.RemoveAll(item => item.thingDef is null || item.count <= 0);
            Pawns?.RemoveAll(p => p is null);
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
        Pawns = null;
        requestThingDefCounts = null;

        Branch = null;
        Faction = null;
        MapParent = null;
        DemandType = default;

        DurationTicks = 30000;
        RawTalkText = null;

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
    }

    public void InitWithDefaultSignal()
    {
        inSignalEnable = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal);
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
    }

    public void InitTalkTextRequest(string talkText, RulePack talkTextRules = null)
    {
        Slate slate = QuestGen.slate;
        QuestGen.AddTextRequest("root", delegate (string x)
        {
            RawTalkText = x;
        }, QuestGenUtility.MergeRules(talkTextRules, talkText, "root"));
    }

    public virtual void InitRequestThingDefCounts(IEnumerable<ThingDefCountClass> thingDefCounts)
    {
        if (thingDefCounts is null)
        {
            QuestGen.slate.Set("collectionTeamRequestInfo", "None".Translate());
            return;
        }

        requestThingDefCounts = [];
        requestThingDefCounts.AddRange(thingDefCounts);

        if (requestThingDefCounts.NullOrEmpty())
        {
            QuestGen.slate.Set("collectionTeamRequestInfo", "None".Translate());
        }

        StringBuilder sb = new();
        foreach (ThingDefCountClass thingDefCount in requestThingDefCounts)
        {
            sb.AppendInNewLine(thingDefCount.Summary);
        }
        QuestGen.slate.Set("collectionTeamRequestInfo", sb.ToString());
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
    }

    protected override void ProcessQuestSignal(Signal signal)
    {
        if (signal.tag == InSignalMakePawnsLeave)
        {
            canTryArrival = false;
            if (CanMakeLeave)
            {
                MakeLeave();
            }
        }
        if (Pawns is not null)
        {
            if (signal.tag == InSignalLeftMap && signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p1))
            {
                Pawns.Remove(p1);
                if (Pawns.Count == 0)
                {
                    Find.SignalManager.SendSignal(new Signal(hasFulfilled ? OutSignalAllLeftMapAndGive : OutSignalAllLeftMap));
                    Complete();
                }
            }
            else if (signal.tag == InSignalRemovePawn && signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p2))
            {
                Pawns.Remove(p2);
                if (Pawns.Count == 0)
                {
                    Disable();
                }
            }
        }
        else if (signal.tag == InSignalLeftMap)
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalAllLeftMap));
            Complete();
        }

        base.ProcessQuestSignal(signal);
    }

    protected override void Enable(SignalArgs receivedArgs)
    {
        if (canTryArrival)
        {
            canTryArrival = false;
            base.Enable(receivedArgs);
            if (!TryMakeTeamArrive())
            {
                Log.Error($"[OARO] 在 {nameof(QuestPart_CollectionTeam)} 的收集小队到达失败。");
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
    }

    public override void QuestPartTick()
    {
        if (CanMakeLeave && Find.TickManager.TicksGame > enableTick + DurationTicks)
        {
            MakeLeave();
        }
    }

    protected bool TryMakeTeamArrive()
    {
        if (Pawns.NullOrEmpty())
        {
            return false;
        }
        MapParent = OAFrame_QuestUtility.GetAvailableMapParent(quest, MapParent);
        if (MapParent is null)
        {
            return false;
        }
        Map map = MapParent.Map;

        IncidentParms arrivalParms = new()
        {
            target = map,
            faction = RelatedFaction,
            quest = quest
        };

        if (!ModUtility.TryMakePawnArrival(Pawns, arrivalParms, PawnsArrivalModeDefOf.EdgeWalkIn, sendStandardLetter: false))
        {
            return false;
        }

        Find.SignalManager.SendSignal(new Signal(OutSignalPawnsArrived));

        talkWith ??= Pawns[0];
        IntVec3 wanderCell = this.GetTalkPawnWanderCenterCell(nearOrderHall: true);

        LordJob_VisitColonyTalkable lordJob = new(RelatedFaction, wanderCell, DurationTicks);
        lordJob.SetTalkAction(talkWith, OARO_JobDefOf.OARO_Job_CommonTalkWith, initTalkActive: true);
        LordMaker.MakeNewLord(RelatedFaction, lordJob, map, Pawns);

        this.RegisterTalkAction();

        return true;
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

    public static List<Pawn> GenerateCaravanMembers(IsolatedPawnGroupMakerDef groupMakerDef, Faction faction, Map map, Branch relatedBranch = null)
    {
        groupMakerDef.TryGetRandomPawnGroupMaker(PawnGroupKindDefOf.Peaceful, out PawnGroupMaker groupMaker);
        if (groupMaker is null)
        {
            return null;
        }

        List<Pawn> pawns = [];
        bool isKnight = relatedBranch.IsValid();
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
                    KnightRecord knightRecord = new(relatedBranch.RatkinOrder, relatedBranch, isCombatant: false, isCommander: false);
                    pawn = KnightGenerateUtility.GenerateKnight(pawnKind, knightRecord, tile: mapTile);
                }
                else
                {
                    pawn = PawnGenerator.GeneratePawn(OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, faction, tile: mapTile));
                }
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
            return;

        try
        {
            if (!hasFulfilled)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignalFailureToCollect));
            }
            TalkActionUtility.DisableLordJobTalk(TalkWith);
            if (Pawns is not null)
            {
                foreach (Pawn pawn in Pawns)
                {
                    pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedByQuest);
                }
                LeaveQuestPartUtility.MakePawnsLeave(Pawns, sendLetter: true, quest, wakeUp: true);
            }
        }
        finally
        {
            hasLeft = true;
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

    protected Dialog_NodeTreeWithRatkinOrderInfo TalkNodeTree(Pawn talker, Pawn talkWith, Map map)
    {
        DiaNode rootNode = new(RawTalkText.Formatted(talker.Named(KeyLibrary_FormatArgName.TALKER), talkWith.Named(KeyLibrary_FormatArgName.TALKWITH)));

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
        MakeLeave();
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

    public override bool QuestPartReserves(Pawn p) => Pawns?.Contains(p) ?? false;

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

}