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
    protected override Faction GetOrGenerateFaction()
    {
        QuestPart_MercyQuestWatcher questPart_MercyQuestWatcher = new()
        {
            SubFaction = QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.SubFaction),
            ParentFaction = QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentFaction),
        };
        QuestGen.quest.AddPart(questPart_MercyQuestWatcher);

        QuestGen.slate.Set(IsMainFactionSlate, true);
        return QuestGen.slate.Get<Faction>("faction");
    }

    protected override bool InitQuestParameter()
    {
        Faction subFaction = QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.SubFaction);
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

        QuestGen.slate.Set(IsMainFactionSlate, true);
        QuestGen.slate.Set(UniqueLeavingLetterSlate, true);
        QuestPart_InvolvedFactions questPart_InvolvedFactions = new()
        {
            factions = [subFaction]
        };
        QuestGen.quest.AddPart(questPart_InvolvedFactions);
        QuestGen.quest.ReserveFaction(subFaction);

        return true;
    }

    protected override List<Pawn> GeneratePawns(string lodgerRecruitedSignal = null)
    {
        List<Pawn> pawns = QuestGen.slate.Get<List<Pawn>>("pawns");

        QuestPart_WorkDisabled questPart_WorkDisabled = new()
        {
            disabledWorkTags = WorkTags.AllWork,
        };
        questPart_WorkDisabled.pawns.AddRange(pawns);
        QuestGen.quest.AddPart(questPart_WorkDisabled);
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
        Reward_AllOrdersEsteem reward = new()
        {
            Amount = 3,
            Reason = "OARO_PutOffTaxCollector".Translate()
        };
        choice.rewards.Add(reward);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string delayFailSignal, string successSignal)
    {
        Quest quest = QuestGen.quest;

        string outSignalsUnhappy = QuestGenUtility.HardcodedSignalWithQuestID("Lodger_Unhappy");
        QuestPart_MoodBelow questPart_MoodBelow = new()
        {
            threshold = 0.7f,
        };
        questPart_MoodBelow.outSignalsCompleted.Add(outSignalsUnhappy);
        questPart_MoodBelow.pawns.AddRange(questParameter.pawns);
        quest.AddPart(questPart_MoodBelow);

        quest.Letter(letterDef: LetterDefOf.NegativeEvent,
                     inSignal: outSignalsUnhappy,
                     text: "[taxCollectorLeaveUnhappyText]",
                     label: "[taxCollectorLeaveUnhappyLabel]",
                     relatedFaction: questParameter.faction);

        quest.End(QuestEndOutcome.Fail, questParameter.goodwillFailure, questParameter.faction, outSignalsUnhappy, sendStandardLetter: true);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange = new()
        {
            InSignalTrigger = successSignal,
            Change = 3,
            Reason = "OARO_PutOffTaxCollector".Translate(),
        };
        quest.AddPart(questPart_AllOrdersEsteemChange);
        base.SetQuestEndComp(questPart_Interactions, failSignal, delayFailSignal, successSignal);
    }
}
