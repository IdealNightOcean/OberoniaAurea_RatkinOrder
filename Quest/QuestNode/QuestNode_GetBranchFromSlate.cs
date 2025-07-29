using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetBranchFromSlate : QuestNode_GetBranchBase
{
    protected override Branch GetBranch(Slate slate)
    {
        slate.TryGet(storeAs.GetValue(slate), out Branch branch);
        return branch;
    }
}