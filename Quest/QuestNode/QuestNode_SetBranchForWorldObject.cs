using RimWorld.Planet;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetBranchForWorldObject : QuestNode
{
    public SlateRef<Branch> branch;
    public SlateRef<WorldObject> worldObject;

    protected override bool TestRunInt(Slate slate)
    {
        return false;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Branch branch = this.branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        if (branch is null)
        {
            return;
        }
        if (worldObject.GetValue(slate) is ISingleBranchRelated branchRelated)
        {
            branchRelated.InitOrderBranch(branch);
        }
        if (worldObject.GetValue(slate) is ISingleRatkinOrderRelated orderRelated)
        {
            orderRelated.InitRatkinOrder(branch.RatkinOrder);
        }
    }
}
