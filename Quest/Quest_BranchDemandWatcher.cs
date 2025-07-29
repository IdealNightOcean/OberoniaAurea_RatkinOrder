using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_BranchDemandWatcher : QuestNode
{
    public SlateRef<Branch> branch;

    public SlateRef<BranchDemandType?> demandType;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestPart_BranchDemandWatcher questPart_BranchDemandWatcher = new()
        {
            branch = branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.BranchStoreAs),
            demandType = demandType.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<BranchDemandType>(KeyLibrary_SlateStoreAs.DemandTypeStoreAs)
        };

        QuestGen.quest.AddPart(questPart_BranchDemandWatcher);
    }
}

public class QuestPart_BranchDemandWatcher : QuestPart, IBranchRelated
{

    public Branch branch;
    public BranchDemandType demandType;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref demandType, "demandType", default);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        OrderInteractionHandler.Instance.Notify_DemandQuestClean(quest);
        demandType = default;
        branch = null;
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        if (branch?.RatkinOrder == order)
        {
            branch = null;
        }
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
        }
    }
}