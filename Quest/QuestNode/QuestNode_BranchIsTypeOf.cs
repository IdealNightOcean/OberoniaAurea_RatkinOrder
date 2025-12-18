using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_BranchIsTypeOf : QuestNode
{
    public SlateRef<Branch> branch;
    public SlateRef<Branch.BranchType?> branchType;

    public QuestNode matchNode;
    public QuestNode noMatchNode;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Branch branch = this.branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.branch);
        if (!branch.IsValid())
        {
            noMatchNode?.Run();
            return;
        }
        Branch.BranchType? branchType = this.branchType.GetValue(slate);
        if (!branchType.HasValue)
        {
            noMatchNode?.Run();
            return;
        }

        if (branch.IsBranchOfType(branchType.Value))
        {
            matchNode?.Run();
        }
        else
        {
            noMatchNode?.Run();
        }
    }
}