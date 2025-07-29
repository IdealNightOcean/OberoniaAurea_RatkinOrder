using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

internal class QuestNode_MaterialSupply : QuestNode
{
    public SlateRef<ThingDef> requestThingDef;
    public SlateRef<int> requestCount;

    public SlateRef<Branch> branch;
    public SlateRef<BranchDemandType?> demandType;

    [NoTranslate]
    public SlateRef<string> inSignalCaravanArrival;
    [NoTranslate]
    public SlateRef<string> inSignalCaravanLeave;
    [NoTranslate]
    public SlateRef<string> inSignalRemovePawn;
    [NoTranslate]
    public SlateRef<string> inSignalLeftMap;

    [NoTranslate]
    public SlateRef<string> outSignalGive;
    [NoTranslate]
    public SlateRef<string> outSignalRejectGive;
    [NoTranslate]
    public SlateRef<string> outSignalAllLeftMap;

    [NoTranslate]
    public SlateRef<string> pawnsTag;

    public SlateRef<int> durationTicks = 30000;

    public SlateRef<MapParent> mapParent;

    public SlateRef<IsolatedPawnGroupMakerDef> isolatedPawnGroupMakerDef;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        if (!requestThingDef.TryGetValue(slate, out ThingDef thingDef))
        {
            return;
        }

        QuestPart_MaterialSupply questPart_MaterialSupply = new(thingDef, requestCount.GetValue(slate))
        {
            branch = branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.BranchStoreAs),
            demandType = demandType.GetValue(slate) ?? slate.Get<BranchDemandType>(KeyLibrary_SlateStoreAs.DemandTypeStoreAs),

            inSignalCaravanArrival = QuestGenUtility.HardcodedSignalWithQuestID(inSignalCaravanArrival.GetValue(slate)),
            inSignalCaravanLeave = QuestGenUtility.HardcodedSignalWithQuestID(inSignalCaravanLeave.GetValue(slate)),
            inSignalRemovePawn = QuestGenUtility.HardcodedSignalWithQuestID(inSignalRemovePawn.GetValue(slate)),
            inSignalLeftMap = QuestGenUtility.HardcodedSignalWithQuestID(inSignalLeftMap.GetValue(slate)),

            outSignalGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalGive.GetValue(slate)),
            outSignalRejectGive = QuestGenUtility.HardcodedSignalWithQuestID(outSignalRejectGive.GetValue(slate)),
            outSignalAllLeftMap = QuestGenUtility.HardcodedSignalWithQuestID(outSignalAllLeftMap.GetValue(slate)),

            pawnsTag = QuestGenUtility.HardcodedTargetQuestTagWithQuestID(pawnsTag.GetValue(slate)),

            durationTicks = durationTicks.GetValue(slate),

            mapParent = mapParent.GetValue(slate) ?? slate.Get<Map>("map")?.Parent,
            isolatedPawnGroupMakerDef = isolatedPawnGroupMakerDef.GetValue(slate)
        };

        QuestGen.quest.AddPart(questPart_MaterialSupply);
    }
}

internal class QuestPart_MaterialSupply : QuestPart, IBranchRelated, ITalkAction
{
    private ThingDef requestThingDef;
    private int requestCount;

    public Branch branch;
    public BranchDemandType demandType;

    public string inSignalCaravanArrival;
    public string inSignalCaravanLeave;
    public string inSignalRemovePawn;
    public string inSignalLeftMap;

    public string outSignalGive;
    public string outSignalRejectGive;
    public string outSignalAllLeftMap;

    public string pawnsTag;

    private bool hasTryArrival;

    public int durationTicks = 30000;

    public MapParent mapParent;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;
    public Faction Faction => RatkinOrder?.Faction;
    public IsolatedPawnGroupMakerDef isolatedPawnGroupMakerDef;

    private Pawn caravanLeader;
    public Pawn TalkWith => caravanLeader;
    private List<Pawn> caravanMembers;

    public QuestPart_MaterialSupply() { }
    public QuestPart_MaterialSupply(ThingDef requestThingDef, int requestCount)
    {
        this.requestThingDef = requestThingDef;
        this.requestCount = Mathf.Max(1, requestCount);
    }
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref requestThingDef, "requestThingDef");
        Scribe_Values.Look(ref requestCount, "requestCount", 0);

        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref demandType, "demandType");

        Scribe_Values.Look(ref inSignalCaravanArrival, "inSignalCaravanArrival");
        Scribe_Values.Look(ref inSignalCaravanLeave, "inSignalCaravanLeave");
        Scribe_Values.Look(ref inSignalRemovePawn, "inSignalRemovePawn");
        Scribe_Values.Look(ref inSignalLeftMap, "inSignalLeftMap");

        Scribe_Values.Look(ref outSignalGive, "outSignalGive");
        Scribe_Values.Look(ref outSignalRejectGive, "outSignalRejectGive");
        Scribe_Values.Look(ref outSignalAllLeftMap, "outSignalAllLeftMap");

        Scribe_Values.Look(ref pawnsTag, "pawnsTag");

        Scribe_Values.Look(ref hasTryArrival, "hasTryArrival", defaultValue: false);
        Scribe_Values.Look(ref durationTicks, "durationTicks", 30000);

        Scribe_References.Look(ref mapParent, "mapParent");

        Scribe_Defs.Look(ref isolatedPawnGroupMakerDef, "isolatedPawnGroupMakerDef");

        Scribe_References.Look(ref caravanLeader, "caravanLeader");
        Scribe_Collections.Look(ref caravanMembers, "caravanMembers", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && quest?.State == QuestState.Ongoing)
        {
            this.RegisterTalkAction();
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        requestThingDef = null;
        requestCount = 0;

        branch = null;
        demandType = default;

        inSignalCaravanArrival = null;
        inSignalCaravanLeave = null;
        inSignalRemovePawn = null;
        inSignalLeftMap = null;

        outSignalGive = null;
        outSignalRejectGive = null;
        outSignalAllLeftMap = null;

        pawnsTag = null;

        durationTicks = 30000;

        mapParent = null;
        isolatedPawnGroupMakerDef = null;

        MakeLeave();
        this.DeregisterTalkAction();
        caravanLeader = null;
        caravanMembers = null;
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        if (branch is not null && quest.State == QuestState.EndedSuccess)
        {
            branch.Squad.SquadStat.Supply += 0.5f;
            if (demandType == BranchDemandType.Urgency)
            {
                branch.SetFriendly(friendly: true);
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!hasTryArrival && signal.tag == inSignalCaravanArrival)
        {
            hasTryArrival = true;
            CaravanArrival();
        }
        else if (caravanMembers is not null && signal.tag == inSignalRemovePawn)
        {
            signal.args.TryGetArg("SUBJECT", out Pawn pawn);
            caravanMembers.Remove(pawn);
        }
        else if (caravanMembers is not null)
        {
            if (signal.tag == inSignalCaravanLeave)
            {
                MakeLeave();
            }
            else if (signal.tag == inSignalLeftMap)
            {
                signal.args.TryGetArg("SUBJECT", out Pawn pawn);
                if (caravanMembers.Remove(pawn) && caravanMembers.Count == 0)
                {
                    Find.SignalManager.SendSignal(new Signal(outSignalAllLeftMap));
                }
            }
        }
    }

    private void CaravanArrival()
    {
        mapParent = OAFrame_QuestUtility.GetAvailableMapParent(quest, mapParent);
        if (mapParent is null)
        {
            quest.End(QuestEndOutcome.Unknown, sendLetter: false, playSound: false);
        }
        Map map = mapParent.Map;
        caravanMembers = GenerateCaravanMembers();
        if (caravanMembers.NullOrEmpty())
        {
            quest.End(QuestEndOutcome.Unknown, sendLetter: false, playSound: false);
            return;
        }

        IncidentParms arrivalParms = new()
        {
            target = map,
            faction = Faction,
            quest = quest
        };

        if (!ModUtility.TryMakePawnArrival(caravanMembers, arrivalParms, PawnsArrivalModeDefOf.EdgeWalkIn))
        {
            quest.End(QuestEndOutcome.Unknown, sendLetter: false, playSound: false);
            return;
        }

        caravanLeader = caravanMembers[0];
        IntVec3 wanderCell = this.GetTalkPawnWanderCenterCell(nearOrderHall: true);


        LordJob_VisitColonyTalkable lordJob = new(Faction, wanderCell, durationTicks);
        lordJob.SetTalkAction(caravanLeader, OARO_ModDefOf.OARO_Job_CommonTalkWith, initTalkActive: true);
        LordMaker.MakeNewLord(Faction, lordJob, map, caravanMembers);

        this.RegisterTalkAction();
    }

    public void TalkAction(Pawn talker, Pawn talkWith)
    {
        Map map = talkWith?.Map ?? caravanLeader?.Map;
        if (requestThingDef is null || map is null)
        {
            return;
        }
        Find.WindowStack.Add(TalkNodeTree(map));
    }

    private List<Pawn> GenerateCaravanMembers()
    {
        if (Faction is null)
        {
            return null;
        }

        OAFrame_PawnGenerateUtility.TryGetRandomPawnGroupMaker(PawnGroupKindDefOf.Peaceful, isolatedPawnGroupMakerDef, out PawnGroupMaker groupMaker);
        if (groupMaker is null)
        {
            return null;
        }

        List<Pawn> pawns = [];
        for (int i = 0; i < groupMaker.options.Count; i++)
        {
            PawnKindDef pawnKind = groupMaker.options[i].kind;
            int pawnCount = (int)groupMaker.options[i].selectionWeight;
            for (int j = 0; j < pawnCount; j++)
            {
                PawnGenerationRequest request = OAFrame_PawnGenerateUtility.CommonPawnGenerationRequest(pawnKind, Faction, forceNew: true);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                QuestUtility.AddQuestTag(pawn, pawnsTag);
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
    private Dialog_NodeTreeWithRatkinOrderInfo TalkNodeTree(Map map)
    {
        DiaNode rootNode = new("OARO_Demand_MaterialSupplyInfo".Translate(requestThingDef.label, requestCount));

        DiaOption giveOpt = new("OARO_Demand_MaterialSupply_Give".Translate())
        {
            action = delegate
            {
                map.DestoryThingsOfDef(requestThingDef, requestCount);
                Find.SignalManager.SendSignal(new Signal(outSignalGive));
                MakeLeave();
            },
            resolveTree = true,
        };
        if (!map.HasEnoughThingsOfDef(requestThingDef, requestCount))
        {
            giveOpt.Disable("OAFrame_NeedCountOfThing".Translate(requestThingDef.LabelCap, requestCount));
        }

        DiaOption rejectOpt = new("OARO_Demand_MaterialSupply_Reject".Translate())
        {
            action = delegate
            {
                Find.SignalManager.SendSignal(new Signal(outSignalRejectGive));
                MakeLeave();
            },
            resolveTree = true,
        };
        DiaOption waitOpt = new("OARO_Demand_MaterialSupply_Wait".Translate())
        {
            resolveTree = true,
        };

        rootNode.options.Add(giveOpt);
        rootNode.options.Add(rejectOpt);
        rootNode.options.Add(waitOpt);

        Dialog_NodeTreeWithRatkinOrderInfo nodeTree = new(rootNode, RatkinOrder);
        return nodeTree;
    }

    private void MakeLeave()
    {
        TalkActionUtility.DisableLordJobTalk(TalkWith);
        if (caravanMembers is not null)
        {
            LeaveQuestPartUtility.MakePawnsLeave(caravanMembers, sendLetter: true, quest, wakeUp: true);
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (RatkinOrder == order)
        {
            branch = null;
        }
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
        }
    }
}
