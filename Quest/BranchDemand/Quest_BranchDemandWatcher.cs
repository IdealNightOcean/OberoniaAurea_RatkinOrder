using OberoniaAurea.RatkinOrder.DataLibrary;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_BranchDemandWatcher : QuestNode
{
    public SlateRef<Branch> branch;
    public SlateRef<BranchDemandDef> demandDef;
    public SlateRef<DemandType?> demandType;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestPart_BranchDemandWatcher questPart_BranchDemandWatcher = new()
        {
            Branch = branch.GetValue(slate) ?? slate.Get<Branch>(OARO_KeyLibrary_SlateStoreAs.branch),
            DemandDef = demandDef.GetValue(slate) ?? slate.Get<BranchDemandDef>(OARO_KeyLibrary_SlateStoreAs.demandDef),
            DemandType = demandType.GetValue(slate) ?? slate.Get<DemandType>(OARO_KeyLibrary_SlateStoreAs.demandType)
        };

        QuestGen.quest.AddPart(questPart_BranchDemandWatcher);
    }
}

public class QuestPart_BranchDemandWatcher : QuestPart, IOnBranchDestroyed
{
    public Branch Branch;
    public BranchDemandDef DemandDef;
    public DemandType DemandType;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Branch, nameof(Branch));
        Scribe_Defs.Look(ref DemandDef, nameof(DemandDef));
        Scribe_Values.Look(ref DemandType, nameof(DemandType));
    }

    public override void Notify_PreCleanup()
    {
        base.Notify_PreCleanup();
        AcceptedBranchDemandHandler.Instance.Notify_DemandQuestPreCleanup(quest);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        DemandType = default;
        DemandDef = null;
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