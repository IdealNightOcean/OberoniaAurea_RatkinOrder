using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_CriticalBranch : QuestPart, IOnBranchDestroyed
{
    public Branch Branch;
    public bool EndQuest = true;
    public QuestEndOutcome EndOutcome = QuestEndOutcome.Unknown;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref EndQuest, "EndQuest", defaultValue: true);
        Scribe_Values.Look(ref EndOutcome, "EndOutcome", QuestEndOutcome.Unknown);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        Branch = null;
        EndQuest = true;
        EndOutcome = QuestEndOutcome.Unknown;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (Branch.RatkinOrder == order && EndQuest)
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

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (Branch == branch)
        {
            Branch = null;
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
