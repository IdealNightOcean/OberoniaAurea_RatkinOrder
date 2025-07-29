using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_CriticalRatkinOrder : QuestPart, IRatkinOrderRelated
{
    public RatkinOrder order;
    public bool endQuest = true;
    public QuestEndOutcome endOutcome = QuestEndOutcome.Unknown;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref order, "order");
        Scribe_Values.Look(ref endQuest, "endQuest", defaultValue: true);
        Scribe_Values.Look(ref endOutcome, "endOutcome", QuestEndOutcome.Unknown);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        order = null;
        endQuest = true;
        endOutcome = QuestEndOutcome.Unknown;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (this.order == order)
        {
            this.order = null;
            if (endQuest)
            {
                if (quest?.State == QuestState.NotYetAccepted)
                {
                    quest.End(QuestEndOutcome.InvalidPreAcceptance);
                }
                else if (quest?.State == QuestState.Ongoing)
                {
                    quest.End(endOutcome);
                }
            }
        }
    }
}