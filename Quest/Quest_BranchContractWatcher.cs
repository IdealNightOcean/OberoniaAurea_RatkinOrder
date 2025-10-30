using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_BranchContractWatcher : QuestNode
{
    public SlateRef<Branch> branch;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestPart_BranchContractWatcher questPart_BranchContractWatcher = new()
        {
            Branch = branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
        };

        QuestGen.quest.AddPart(questPart_BranchContractWatcher);
    }
}

public class QuestPart_BranchContractWatcher : QuestPart, IOnBranchDestroyed
{
    public Branch Branch;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, "Branch");
    }

    public override void Cleanup()
    {
        base.Cleanup();

        Branch?.PopulationHandler.Notify_ContractFinished(quest);
        Branch = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (Branch?.RatkinOrder == order)
        {
            Branch = null;
        }
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (Branch == branch)
        {
            Branch = null;
        }
    }
}