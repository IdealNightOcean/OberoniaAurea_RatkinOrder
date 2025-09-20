using RimWorld.Planet;
using RimWorld.QuestGen;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_SetRatkinOrderForWorldObject : QuestNode
{
    public SlateRef<RatkinOrder> ratkinOrder;
    public SlateRef<Branch> branch;
    public SlateRef<WorldObject> worldObject;

    protected override bool TestRunInt(Slate slate)
    {
        return false;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        WorldObject worldObject = this.worldObject.GetValue(slate);
        if (worldObject is null)
        {
            return;
        }

        RatkinOrder ratkinOrder = this.ratkinOrder.GetValue(slate) ?? slate.Get<RatkinOrder>(KeyLibrary_SlateStoreAs.RatkinOrder);
        if (ratkinOrder is not null && worldObject is ISingleRatkinOrderRelated orderRelated)
        {
            orderRelated.InitRatkinOrder(ratkinOrder);
        }

        Branch branch = this.branch.GetValue(slate) ?? slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch);
        if (branch is not null && worldObject is ISingleBranchRelated branchRelated)
        {
            branchRelated.InitOrderBranch(branch);
        }
    }
}