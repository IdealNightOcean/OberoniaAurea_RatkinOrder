using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_CriticalRatkinOrder : QuestPart, IOnRatkinOrderRemoved
{
    public RatkinOrder RatkinOrder;
    public bool EndQuest = true;
    public QuestEndOutcome EndOutcome = QuestEndOutcome.Unknown;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref RatkinOrder, "RatkinOrder");
        Scribe_Values.Look(ref EndQuest, "EndQuest", defaultValue: true);
        Scribe_Values.Look(ref EndOutcome, "EndOutcome", QuestEndOutcome.Unknown);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        RatkinOrder = null;
        EndQuest = true;
        EndOutcome = QuestEndOutcome.Unknown;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (RatkinOrder == ratkinOrder)
        {
            RatkinOrder = null;
            if (EndQuest)
            {
                if (quest?.State == QuestState.NotYetAccepted)
                {
                    quest.End(QuestEndOutcome.InvalidPreAcceptance);
                }
                else if (quest?.State == QuestState.Ongoing)
                {
                    quest.End(EndOutcome);
                }
            }
        }
    }
}