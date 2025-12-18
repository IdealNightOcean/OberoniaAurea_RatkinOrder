using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_AllRatkinFactionsGoogwillChange : QuestNode_AllFactionsGoodwillChange
{
    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_AllRatkinFactionsGoodwillChange part = new()
        {
            inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
            goodwillChange = goodwillChange.GetValue(slate),
            historyEvent = historyEvent.GetValue(slate),

            canSendMessage = canSendMessage.GetValue(slate),
            canSendHostilityLetter = canSendHostilityLetter.GetValue(slate),

            canApplyOnAlly = canApplyOnAlly.GetValue(slate),
            canApplyOnNeutral = canApplyOnNeutral.GetValue(slate),
            canApplyOnHostile = canApplyOnHostile.GetValue(slate)
        };
        QuestGen.quest.AddPart(part);
    }
}

public class QuestPart_AllRatkinFactionsGoodwillChange : QuestPart_AllFactionsGoodwillChange
{
    protected override bool IsAvailableFaction(Faction faction)
    {
        if (base.IsAvailableFaction(faction))
        {
            return faction.IsRatkinFaction();
        }
        return false;
    }
}