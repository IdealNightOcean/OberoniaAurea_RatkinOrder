using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_CriticalBranch : QuestPart, IBranchRelated
{
    public Branch branch;
    public bool endQuest = true;
    public QuestEndOutcome endOutcome = QuestEndOutcome.Unknown;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref endQuest, "endQuest", defaultValue: true);
        Scribe_Values.Look(ref endOutcome, "endOutcome", QuestEndOutcome.Unknown);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        branch = null;
        endQuest = true;
        endOutcome = QuestEndOutcome.Unknown;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (branch.RatkinOrder == order && endQuest)
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

    public void Notify_BranchDestoryed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
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
