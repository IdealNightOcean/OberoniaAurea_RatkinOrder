using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_InDistressKnight : QuestNode_Root_RefugeeKnightBase
{
    protected override PawnKindDef FixedPawnKind => OARO_PawnKindDefOf.RatkinKnight;

    protected override bool IsCombatant => true;
    protected override bool IsCommander => true;

    private string InSignalRecruited { get; set; }

    protected override bool InitQuestParameter()
    {
        questParameter = new()
        {
            allowAssaultColony = false,
            allowBadThought = true,
            allowLeave = true,

            allowFutureReward = false,
            allowJoinOffer = false,

            LodgerCount = 1,
            ChildCount = 0,

            questDurationTicks = 8 * 60000
        };

        InSignalRecruited = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_Recruited");
        QuestGen.slate.Set(UniqueQuestDescSlate, true);
        QuestGen.slate.Set(UniqueLeavingLetterSlate, true);

        return InitRatkinOrder(initBranch: true);
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        InSignalRecruited = null;
    }

    protected override bool InitRatkinOrder(bool initBranch)
    {
        PlanetTile tile = questParameter.map.Tile;
        Branch targetBranch = null;
        float minDistance = float.MaxValue;

        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            foreach (Branch branch in ratkinOrder.BranchManager.AllBranches)
            {
                float distance = branch.DistanceTo(tile);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetBranch = branch;
                }
            }
        }

        if (!targetBranch.IsValid())
        {
            targetBranch = RatkinOrderManager.Instance.AllRatkinOrders.RandomElementWithFallback(fallback: null)?.BranchManager.AllBranches.RandomElementWithFallback(fallback: null);
        }

        if (!targetBranch.IsValid())
        {
            return false;
        }

        Branch = targetBranch;
        QuestGen.slate.Set(KeyLibrary_SlateStoreAs.Branch, Branch);
        RatkinOrder = targetBranch.RatkinOrder;
        QuestGen.slate.Set(KeyLibrary_SlateStoreAs.RatkinOrder, RatkinOrder);

        return base.InitRatkinOrder(initBranch);
    }

    protected override void PostPawnGenerated(Pawn pawn, string lodgerRecruitedSignal)
    {
        base.PostPawnGenerated(pawn, lodgerRecruitedSignal);
        OAFrame_PawnUtility.TakeNonLethalDamage(pawn, injuriesCount: 6, fixedDamageDef: DamageDefOf.Blunt);
        pawn.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_InDistressKnight);
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;

        string inSignalCanRecruited = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_CanRecruitedNow");
        QuestPart_DistressKnightCanRecruitNow questPart_DistressKnightCanRecruitNow = new()
        {
            OutSignal = inSignalCanRecruited,
            InSignalRemovePawn = inSignalRemovePawn,
            Pawns = []
        };
        questPart_DistressKnightCanRecruitNow.Pawns.AddRange(questParameter.pawns);
        quest.AddPart(questPart_DistressKnightCanRecruitNow);

        quest.Delay(questParameter.questDurationTicks, inner: null, outSignalComplete: InSignalRecruited);

        QuestPart_DistressKnightRecruit questPart_DistressKnightRecruit = new()
        {
            Branch = Branch,
            InSignalRemovePawn = inSignalRemovePawn,
            InSignalRecruit = InSignalRecruited,
            Pawns = []
        };
        questPart_DistressKnightRecruit.Pawns.AddRange(questParameter.pawns);
        quest.AddPart(questPart_DistressKnightRecruit);

        quest.Letter(letterDef: LetterDefOf.PositiveEvent,
                     inSignal: InSignalRecruited,
                     relatedFaction: questParameter.faction,
                     lookTargets: questParameter.pawns,
                     filterDeadPawnsFromLookTargets: true,
                     text: "[distressKnightRecruitLetterText]",
                     label: "[distressKnightRecruitLetterLabel]");
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
        QuestGen.quest.End(QuestEndOutcome.Success, inSignal: InSignalRecruited);
    }
}


public class QuestPart_DistressKnightCanRecruitNow : QuestPartActivable
{
    private const int CheckInterval = 30000;
    public string OutSignal;
    public string InSignalRemovePawn;

    public List<Pawn> Pawns;

    private int ticksToNextCheck = 2500;

    public bool CanRecruitNow
    {
        get
        {
            if (Pawns.NullOrEmpty())
            {
                return false;
            }
            for (int i = 0; i < Pawns.Count; i++)
            {
                if (!IsHealthyPawn(Pawns[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OutSignal = null;
        InSignalRemovePawn = null;

        Pawns = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref ticksToNextCheck, nameof(ticksToNextCheck), 2500);
        Scribe_Values.Look(ref OutSignal, nameof(OutSignal));
        Scribe_Values.Look(ref InSignalRemovePawn, nameof(InSignalRemovePawn));
        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void QuestPartTick()
    {
        if ((--ticksToNextCheck) <= 0)
        {
            ticksToNextCheck = CheckInterval;
            if (CanRecruitNow)
            {
                Find.SignalManager.SendSignal(new Signal(OutSignal));
                Complete();
            }
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (!Pawns.NullOrEmpty() && signal.tag == InSignalRemovePawn)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p))
            {
                Pawns.Remove(p);
            }
        }
    }

    private static bool IsHealthyPawn(Pawn pawn) //判断一个Pawn是否健康
    {
        if (pawn.Destroyed || pawn.InMentalState)
        {
            return false;
        }
        HediffSet pawnHediffSet = pawn.health.hediffSet;
        if (pawnHediffSet is null) //没有健康状态属性那肯定是健康的（确信）
        {
            return true;
        }
        if (pawnHediffSet.BleedRateTotal > 0.001f)
        {
            return false;
        }
        if (pawnHediffSet.HasNaturallyHealingInjury())
        {
            return false;
        }
        return true;
    }
}


public class QuestPart_DistressKnightRecruit : QuestPart
{
    public string InSignalRemovePawn;
    public string InSignalRecruit;
    public Branch Branch;

    public List<Pawn> Pawns;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref InSignalRemovePawn, nameof(InSignalRemovePawn));
        Scribe_Values.Look(ref InSignalRecruit, nameof(InSignalRecruit));

        Scribe_References.Look(ref Branch, nameof(Branch));

        Scribe_Collections.Look(ref Pawns, nameof(Pawns), LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            Pawns?.RemoveAll(p => p is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalRemovePawn = null;
        InSignalRecruit = null;
        Branch = null;

        Pawns = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (Pawns.NullOrEmpty())
        {
            return;
        }

        if (signal.tag == InSignalRecruit)
        {
            RecruitKnight();
        }
        else if (signal.tag == InSignalRemovePawn)
        {
            if (signal.args.TryGetArg(KeyLibrary_FormatArgName.SUBJECT, out Pawn p))
            {
                Pawns.Remove(p);
            }
        }
    }

    private void RecruitKnight()
    {
        Pawns.RemoveAll(p => p.DestroyedOrNull());
        foreach (Pawn p in Pawns)
        {
            OAFrame_PawnUtility.MakePawnJoinPlayer(p);
            if (KnightPawnsManager.Instance.TryGetKnightRecord(p, out KnightRecord record))
            {
                ResidentKnightsManager.Instance.AddResidentKnight(p, record);
            }
        }

        OrderLetter_InDistressKnight orderLetter = (OrderLetter_InDistressKnight)OrderLetterUtility.MakeOrderLetter(
            label: "OARO_InDistressKnightThankLabel".Translate(Branch.RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)),
            text: "OARO_InDistressKnightThankText".Translate(Branch.RatkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName), Branch.NameColored.Named(KeyLibrary_FormatArgName.BranchName), Pawns[0].Named(KeyLibrary_FormatArgName.PAWN)),
            def: OrderLetterDefOf.OARO_OfficialLetter_InDistressKnight,
            relatedOrder: Branch.RatkinOrder,
            relatedBranch: Branch,
            sender: Branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);

        OrderRecommendation recommendation = (OrderRecommendation)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_OrderRecommendation);
        recommendation.SetRatkinOrder(Branch.RatkinOrder);

        orderLetter.Attachments = [recommendation];

        OrderLetterBox.Instance.ReceiveLetter(orderLetter, delayDays: 3);
    }
}