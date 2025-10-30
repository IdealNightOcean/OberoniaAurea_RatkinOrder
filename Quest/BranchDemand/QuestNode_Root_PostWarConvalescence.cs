using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 任务实现：战后疗养（内部特化类）
/// </summary>
internal sealed class QuestNode_Root_PostWarConvalescence : QuestNode_Root_RefugeeKnightBase
{
    private Branch branch;
    private DemandType demandType;
    private bool giveNormalRecommendation;

    private string outSigalPerfecState;
    private string outSigalMoodFailed;
    private HediffDef specialHediff;

    public override PawnKindDef FixedPawnKind => OARO_PawnKindDefOf.RatkinKnight;

    protected override void InitQuestParameter()
    {
        questParameter = new QuestParameter()
        {
            allowAssaultColony = false,
            allowJoinOffer = false,
            allowFutureReward = false,

            goodwillSuccess = 22,
            goodwillFailure = -22,

            questDurationTicks = 12 * 60000
        };

        Slate slate = QuestGen.slate;
        slate.Set(UniqueLeavingLetterSlate, true);

        giveNormalRecommendation = Rand.Chance(0.25f);
        demandType = slate.Get<DemandType>(KeyLibrary_SlateStoreAs.DemandType);
        if (demandType == DemandType.Supplementary)
        {
            questParameter.LodgerCount = 1;
        }
        else
        {
            questParameter.LodgerCount = Rand.RangeInclusive(3, 4);
        }

        outSigalMoodFailed = QuestGenUtility.HardcodedSignalWithQuestID("Mood_Failed");
        outSigalPerfecState = QuestGenUtility.HardcodedSignalWithQuestID("Quest_PerfectState");

        branch = slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        QuestPart_BranchDemandWatcher questPart_BranchDemandWatcher = new()
        {
            Branch = branch,
            DemandType = demandType
        };
        QuestGen.quest.AddPart(questPart_BranchDemandWatcher);
    }

    protected override void ClearQuestParameter()
    {
        base.ClearQuestParameter();
        branch = null;
        demandType = default;
        specialHediff = null;

        outSigalMoodFailed = null;
        outSigalPerfecState = null;
    }

    protected override void PostPawnGenerated(Pawn pawn)
    {
        base.PostPawnGenerated(pawn);
        OAFrame_PawnUtility.TakeNonLethalDamage(pawn, Rand.RangeInclusive(2, 4), DamageDefOf.Blunt);

        if (demandType == DemandType.Supplementary || specialHediff is null)
        {
            return;
        }

        pawn.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_WarDeepInjury);
    }

    protected override void AddQuestAward(QuestPart_Choice.Choice choice)
    {
        base.AddQuestAward(choice);

        Reward_Items reward_Items = new()
        {
            items = OAFrame_MiscUtility.TryGenerateThing(ThingDefOf.Silver, questParameter.LodgerCount * 280)
        };

        Reward_OrderEsteem reward_OrderEsteem = new()
        {
            RatkinOrder = branch?.RatkinOrder,
            Amount = 1,
        };

        Reward_FriendlyBranch reward_FriendlyBranch = new()
        {
            Branch = branch
        };

        choice.rewards.Add(reward_Items);
        choice.rewards.Add(reward_OrderEsteem);
        choice.rewards.Add(reward_FriendlyBranch);

        if (giveNormalRecommendation)
        {
            Reward_OrderRecommendation reward_OrderRecommendation = new()
            {
                RatkinOrder = branch?.RatkinOrder,
                Count = 1
            };
            choice.rewards.Add(reward_OrderRecommendation);
        }
    }

    protected override void SetPawnsLeaveComp(string lodgerArrivalSignal, string inSignalRemovePawn)
    {
        Quest quest = QuestGen.quest;
        string inSigalTimeToLeave = QuestGenUtility.HardcodedSignalWithQuestID("Quest_TimeToLeave");
        quest.Delay(delayTicks: questParameter.questDurationTicks,
                    inner: null,
                    inSignalEnable: lodgerArrivalSignal,
                    outSignalComplete: inSigalTimeToLeave,
                    reactivatable: false,
                    expiryInfoPart: "GuestsDepartsIn".Translate(),
                    expiryInfoPartTip: "GuestsDepartsOn".Translate(),
                    debugLabel: "QuestDelay");

        string outSigalMoodSuccess = QuestGenUtility.HardcodedSignalWithQuestID("Mood_Success");
        string outSigalMoodNormal = QuestGenUtility.HardcodedSignalWithQuestID("Mood_Normal");

        QuestPart_AvaerageMood questPart_AvaerageMood = new()
        {
            inSignalEnable = QuestGen.slate.Get<string>("inSiganl"),
            InSignal = inSigalTimeToLeave,
            InSignalRemovePawn = inSignalRemovePawn,

            MoodHighThreshold = 0.75f,
            MoodLowThreshold = 0.25f,

            MaxTicksBelowThreshold = 60000,

            OutSignalSuccess = outSigalMoodSuccess,
            OutSignalBelowHighThreshold = outSigalMoodNormal,
            OutSignalBelowLowThreshold = outSigalMoodFailed,
        };
        questPart_AvaerageMood.Pawns.AddRange(questParameter.pawns);
        quest.AddPart(questPart_AvaerageMood);

        string outSignalHas = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_HasInjury");
        string outSignalNoOneHas = QuestGenUtility.HardcodedSignalWithQuestID("Lodgers_NoOneHasInjury");
        if (demandType != DemandType.Supplementary)
        {
            QuestPart_AnyPawnHasSpecialHediff questPart_AnyPawnHasSpecialHediff = new()
            {
                inSignalCheck = inSigalTimeToLeave,
                inSignalRemovePawn = inSignalRemovePawn,
                outSignalHas = outSignalHas,
                outSignalNoOneHas = outSignalNoOneHas,
            };
            questPart_AnyPawnHasSpecialHediff.pawns.AddRange(questParameter.pawns);
            quest.AddPart(questPart_AnyPawnHasSpecialHediff);

            quest.SignalPassAll(inSignals: [outSigalMoodSuccess, outSignalNoOneHas], outSignal: outSigalPerfecState);
        }

        quest.Leave(questParameter.pawns, inSignal: outSigalMoodFailed);
        DefaultDelayLeaveComp(lodgerArrivalSignal, outSigalMoodFailed, inSignalRemovePawn);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;

        // 因心情问题提前离开导致的失败
        string inSignalFailAny = QuestGenUtility.HardcodedSignalWithQuestID("Quest_FailAny");
        quest.AnySignal(inSignals: [failSignal, delayFailSignal, outSigalMoodFailed], outSignals: [inSignalFailAny]);

        QuestPart_OrderEsteemChange questPart_OrderEsteemChange_Fail = new()
        {
            InSignalTrigger = inSignalFailAny,
            RatkinOrder = branch.RatkinOrder,
            Change = -6,
            Reason = "OARO_PostWarConvalescence_Fail".Translate()
        };
        quest.AddPart(questPart_OrderEsteemChange_Fail);

        quest.End(QuestEndOutcome.Fail, questParameter.goodwillFailure, questParameter.faction, inSignal: inSignalFailAny, sendStandardLetter: true, playSound: true);

        // 完美完成
        string inSignalPerfectSuccess = QuestGenUtility.HardcodedSignalWithQuestID("Quest_PerfectSuccess");
        quest.SignalPassAll(inSignals: [outSigalPerfecState, successSignal], outSignal: inSignalPerfectSuccess);
        QuestPart_OrderEsteemChange questPart_OrderEsteemChange_PerfectSuccess = new()
        {
            InSignalTrigger = inSignalPerfectSuccess,
            RatkinOrder = branch.RatkinOrder,
            Change = 4,
            Reason = "OARO_PostWarConvalescence_Success".Translate()
        };
        quest.AddPart(questPart_OrderEsteemChange_PerfectSuccess);
        QuestPart_OrderRecommendation questPart_OrderRecommendation_PerfectSuccess = new()
        {
            InSignalTrigger = inSignalPerfectSuccess,
            RatkinOrder = branch.RatkinOrder,
            Count = 1
        };
        quest.AddPart(questPart_OrderRecommendation_PerfectSuccess);
        QuestPart_SetBranchToFriendly questPart_SetBranchToFriendly_PerfectSuccess = new()
        {
            InSignalTrigger = inSignalPerfectSuccess,
            Branch = branch
        };
        quest.AddPart(questPart_SetBranchToFriendly_PerfectSuccess);

        quest.DropPods(mapParent: questParameter.map.Parent,
                       contents: OAFrame_MiscUtility.TryGenerateThing(ThingDefOf.Silver, questParameter.LodgerCount * 580),
                       useTradeDropSpot: true,
                       inSignal: inSignalPerfectSuccess,
                       faction: questParameter.faction);
        quest.End(QuestEndOutcome.Success, questParameter.goodwillSuccess, questParameter.faction, inSignal: inSignalPerfectSuccess, sendStandardLetter: true, playSound: true);

        // 普通完成
        quest.SignalPassActivable(
            action: delegate
            {
                QuestPart_OrderEsteemChange questPart_OrderEsteemChange_NormalSuccess = new()
                {
                    InSignalTrigger = QuestGen.slate.Get<string>("inSignal"),
                    RatkinOrder = branch.RatkinOrder,
                    Change = 1,
                    Reason = "OARO_PostWarConvalescence_Success".Translate()
                };
                quest.AddPart(questPart_OrderEsteemChange_NormalSuccess);
                if (giveNormalRecommendation)
                {
                    QuestPart_OrderRecommendation questPart_OrderRecommendation_NormalSuccess = new()
                    {
                        InSignalTrigger = QuestGen.slate.Get<string>("inSignal"),
                        RatkinOrder = branch.RatkinOrder,
                        Count = 1
                    };
                    quest.AddPart(questPart_OrderRecommendation_NormalSuccess);
                }
                quest.DropPods(mapParent: questParameter.map.Parent,
                               contents: OAFrame_MiscUtility.TryGenerateThing(ThingDefOf.Silver, questParameter.LodgerCount * 280),
                               useTradeDropSpot: true,
                               faction: questParameter.faction);
            },
            inSignalDisable: outSigalPerfecState,
            inSignal: successSignal);

        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }

}