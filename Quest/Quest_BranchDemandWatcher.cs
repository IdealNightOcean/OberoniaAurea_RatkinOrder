using RimWorld;
using RimWorld.QuestGen;
using System.Linq;
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
            Branch = branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch),
            DemandType = demandType.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<BranchDemandType>(KeyLibrary_SlateStoreAs.DemandType)
        };

        QuestGen.quest.AddPart(questPart_BranchDemandWatcher);
    }
}

public class QuestPart_BranchDemandWatcher : QuestPart, IOnBranchDestroyed
{

    public Branch Branch;
    public BranchDemandType DemandType;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref DemandType, "DemandType", default);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        GlobalOrderInteractionManager.AcceptedBranchDemandHandler.Notify_DemandQuestClean(quest);
        DemandType = default;
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

    public static (Branch branch, BranchDemandType demandType) GetBranchDemand(Quest quest)
    {
        QuestPart_BranchDemandWatcher questPart_BranchDemandWatcher = quest?.PartsListForReading.OfType<QuestPart_BranchDemandWatcher>()?.FirstOrFallback(null);

        if (questPart_BranchDemandWatcher is null)
        {
            return (null, BranchDemandType.Normal);
        }

        return (questPart_BranchDemandWatcher.Branch, questPart_BranchDemandWatcher.DemandType);
    }
}