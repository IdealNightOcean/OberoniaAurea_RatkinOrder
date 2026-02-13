using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 招待征收官
/// </summary>
internal sealed class QuestNode_Root_TaxCollectorTreat : QuestNode_Root_RefugeeBase
{
    protected override ThoughtDef ThoughtToAdd => OARO_ThoughtDefOf.OARO_Thought_TaxTreatment;

    protected override Faction GetOrGenerateFaction()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;

        Faction subFaction = slate.Get<Faction>(KeyLibrary_SlateStoreAs.subFaction);
        QuestPart_InvolvedFactions questPart_InvolvedFactions = new()
        {
            factions = [subFaction]
        };
        quest.AddPart(questPart_InvolvedFactions);
        quest.ReserveFaction(subFaction);

        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            MercyQuestDef = slate.Get<MercyQuestDef>(KeyLibrary_SlateStoreAs.mercyQuestDef),
            SubFaction = subFaction,
            ParentFaction = slate.Get<Faction>(KeyLibrary_SlateStoreAs.parentFaction),
        };
        quest.AddPart(questPart_MercyQuestWatcher);

        slate.Set(IsMainFactionSlate, true);
        return slate.Get<Faction>("faction");
    }

    protected override bool InitQuestParameter()
    {
        questParameter = new()
        {
            allowAssaultColony = false,
            allowBadThought = false,
            allowLeave = true,
            allowFutureReward = false,
            allowJoinOffer = false,

            LodgerCount = 7,
            ChildCount = 0,

            goodwillSuccess = 0,
            goodwillFailure = -12,

            questDurationTicks = 3 * 60000
        };

        QuestGen.slate.Set(UniqueLeavingLetterSlate, true);

        return true;
    }

    protected override List<Pawn> GeneratePawns(string lodgerRecruitedSignal = null)
    {
        List<Pawn> pawns = QuestGen.slate.Get<List<Pawn>>("pawns");

        QuestPart_WorkDisabled questPart_WorkDisabled = new()
        {
            inSignalEnable = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            disabledWorkTags = WorkTags.AllWork,
        };
        questPart_WorkDisabled.pawns ??= new(pawns.Count);
        questPart_WorkDisabled.pawns.AddRange(pawns);
        QuestGen.quest.AddPart(questPart_WorkDisabled);

        foreach (Pawn p in pawns)
        {
            PostPawnGenerated(p, lodgerRecruitedSignal);
        }
        return pawns;
    }

    protected override void PawnArrival(string lodgerArrivalSignal)
    {
        QuestGen.quest.JoinPlayer(questParameter.map.Parent, questParameter.pawns, joinPlayer: true);
        QuestGen.quest.SendSignals(outSignals: [lodgerArrivalSignal]);
    }

    protected override void AddQuestAward(QuestPart_Choice.Choice choice)
    {
        base.AddQuestAward(choice);
        Reward_AllOrdersEsteem reward_Esteem = new()
        {
            Amount = 3,
            Reason = "OARO_PutOffTaxCollector".Translate()
        };
        Reward_OrderRecommendation reward_Recommendation = new()
        {
            Count = 1
        };
        choice.rewards.Add(reward_Esteem);
        choice.rewards.Add(reward_Recommendation);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;

        string outSignalsUnhappy = QuestGenUtility.HardcodedSignalWithQuestID("Lodger_Unhappy");
        QuestPart_MoodBelow questPart_MoodBelow = new()
        {
            threshold = 0.4f,
        };
        questPart_MoodBelow.outSignalsCompleted.Add(outSignalsUnhappy);
        questPart_MoodBelow.pawns.AddRange(questParameter.pawns);
        quest.AddPart(questPart_MoodBelow);

        quest.Letter(letterDef: LetterDefOf.NegativeEvent,
                     inSignal: outSignalsUnhappy,
                     text: "[taxCollectorLeaveUnhappyText]",
                     label: "[taxCollectorLeaveUnhappyLabel]",
                     relatedFaction: questParameter.faction);

        quest.End(QuestEndOutcome.Fail, questParameter.goodwillFailure, questParameter.faction, inSignal: outSignalsUnhappy, sendStandardLetter: true);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange = new()
        {
            InSignalTrigger = successSignal,
            Change = 3,
            Reason = "OARO_PutOffTaxCollector".Translate(),
        };
        quest.AddPart(questPart_AllOrdersEsteemChange);
        QuestPart_OrderRecommendation questPart_OrderRecommendation_Success = new()
        {
            InSignalTrigger = successSignal,
            Count = 1
        };
        quest.AddPart(questPart_OrderRecommendation_Success);
        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }
}