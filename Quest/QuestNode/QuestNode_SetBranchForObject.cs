using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetBranchForObject : QuestNode
{
    public SlateRef<Branch> branch;
    public SlateRef<bool> alsoSetRatkinOrder = true;
    public SlateRef<object> target;

    protected override bool TestRunInt(Slate slate)
    {
        return false;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        object target = this.target.GetValue(slate);
        if (target is null || target is not ISingleBranchRelated branchRelated)
        {
            return;
        }

        Branch branch = this.branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        if (branch.IsValid())
        {
            branchRelated.SetOrderBranch(branch);
            if (alsoSetRatkinOrder.GetValue(slate) && target is ISingleRatkinOrderRelated orderRelated)
            {
                orderRelated.InitRatkinOrder(branch.RatkinOrder);
            }
        }
    }
}