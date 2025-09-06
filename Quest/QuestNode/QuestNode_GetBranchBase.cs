using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class QuestNode_GetBranchBase : QuestNode
{
    [NoTranslate]
    public SlateRef<string> storeAs = KeyLibrary_SlateStoreAs.Branch;

    public SlateRef<bool> storeRatkinOrder;
    [NoTranslate]
    public SlateRef<string> storeRatkinOrderAs = KeyLibrary_SlateStoreAs.RatkinOrder;

    public SlateRef<bool> addRatkinOrderToQuest;

    public SlateRef<bool> isCritical;
    public SlateRef<bool> endQuestWhenOrderInvalid;
    public SlateRef<QuestEndOutcome> questEndOutcome = QuestEndOutcome.Unknown;

    protected abstract Branch GetBranch(Slate slate);

    protected override bool TestRunInt(Slate slate)
    {
        Branch branch = GetBranch(slate);
        if (branch is null)
        {
            return false;
        }
        else
        {
            slate.Set(storeAs.GetValue(slate), branch);
            return true;
        }
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        Branch branch = GetBranch(slate);

        if (branch is null)
        {
            return;
        }

        Quest quest = QuestGen.quest;

        slate.Set(storeAs.GetValue(slate), branch);
        if (storeRatkinOrder.GetValue(slate))
        {
            slate.Set(storeRatkinOrderAs.GetValue(slate), branch.RatkinOrder);
        }
        if (isCritical.GetValue(slate))
        {
            QuestPart_CriticalBranch questPart_CriticalBranch = new()
            {
                Branch = branch,
                EndQuest = endQuestWhenOrderInvalid.GetValue(slate),
                EndOutcome = questEndOutcome.GetValue(slate)
            };
            quest.AddPart(questPart_CriticalBranch);
        }

        QuestPart_InvolvedBranches.AddInvolvedBranch(quest, branch);
        if (addRatkinOrderToQuest.GetValue(slate))
        {
            QuestPart_InvolvedRatkinOrders.AddInvolvedRatkinOrder(quest, branch.RatkinOrder);
        }
    }
}