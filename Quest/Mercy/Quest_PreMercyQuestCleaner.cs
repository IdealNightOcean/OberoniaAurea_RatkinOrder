using RimWorld;
using RimWorld.QuestGen;

using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_PreMercyQuestCleaner : QuestNode
{
    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        QuestGen.quest.AddPart(new QuestPart_PreMercyQuestCleaner());
    }
}

public class QuestPart_PreMercyQuestCleaner : QuestPart
{
    public override void Cleanup()
    {
        base.Cleanup();
        Find.QuestManager.QuestsListForReading.RemoveAll(CanRemove);
    }

    private bool CanRemove(Quest quest)
    {
        //不要移除自己，防止出意外
        if (quest == this.quest)
        {
            return false;
        }

        if (quest.root != this.quest.root)
        {
            return false;
        }

        return quest.State switch
        {
            QuestState.NotYetAccepted => false,
            QuestState.Ongoing => false,
            QuestState.EndedUnknownOutcome => true,
            QuestState.EndedOfferExpired => true,
            QuestState.EndedSuccess => true,
            QuestState.EndedFailed => true,
            QuestState.EndedInvalid => true,
            _ => false
        };
    }
}