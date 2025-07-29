using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_Root_TaxCollectorTreat : QuestNode_Root_RefugeeBase
{
    protected override Faction GetOrGenerateFaction()
    {
        return QuestGen.slate.Get<Faction>("faction");
    }

    protected override void InitQuestParameter()
    {
        Faction subFaction = QuestGen.slate.Get<Faction>(KeyLibrary_SlateStoreAs.SubRatkinFactionStoreAs);
        questParameter = new()
        {
            allowAssaultColony = false,
            allowBadThought = false,
            allowLeave = true,

            LodgerCount = 7,
            ChildCount = 0,

            goodwillSuccess = 12,
            goodwillFailure = -12,

            questDurationTicks = 3 * 60000
        };

        QuestPart_InvolvedFactions questPart_InvolvedFactions = new()
        {
            factions = [subFaction]
        };
        QuestGen.quest.AddPart(questPart_InvolvedFactions);
        QuestGen.quest.ReserveFaction(subFaction);
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
            amount = 3,
            reason = "OARO_PutOffTaxCollector".Translate()
        };
        choice.rewards.Add(reward);
    }

    protected override void SetQuestEndComp(QuestPart_OARefugeeInteractions questPart_Interactions, string failSignal, string bigFailSignal, string successSignal)
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
                     text: "OARO_Letter_TaxCollector".Translate(),
                     label: "OARO_LetterLabel_TaxCollector".Translate());

        quest.End(QuestEndOutcome.Fail, questParameter.goodwillFailure, questParameter.faction, outSignalsUnhappy, sendStandardLetter: true);

        QuestPart_AllOrdersEsteemChange questPart_AllOrdersEsteemChange = new()
        {
            inSignalTrigger = successSignal,
            change = 3,
            reason = "OARO_PutOffTaxCollector".Translate(),
        };
        quest.AddPart(questPart_AllOrdersEsteemChange);
        base.SetQuestEndComp(questPart_Interactions, failSignal, bigFailSignal, successSignal);
    }
}
