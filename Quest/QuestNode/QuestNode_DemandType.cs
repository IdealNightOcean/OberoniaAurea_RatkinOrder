using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_DemandTypeSwitch : QuestNode
{
    public SlateRef<BranchDemandType?> demandType;
    public Dictionary<BranchDemandType, QuestNode> demandNode;

    protected override bool TestRunInt(Slate slate)
    {
        if (demandNode.NullOrEmpty())
        {
            return true;
        }
        BranchDemandType? demandType = this.demandType.GetValue(slate) ?? slate.Get<BranchDemandType>(KeyLibrary_SlateStoreAs.DemandType);
        if (!demandType.HasValue)
        {
            return false;
        }
        if (demandNode.TryGetValue(demandType.Value, out QuestNode node))
        {
            return node.TestRun(slate);
        }
        return true;
    }

    protected override void RunInt()
    {
        if (demandNode.NullOrEmpty())
        {
            return;
        }
        Slate slate = QuestGen.slate;
        BranchDemandType? demandType = this.demandType.GetValue(slate) ?? slate.Get<BranchDemandType>(KeyLibrary_SlateStoreAs.DemandType);
        if (!demandType.HasValue)
        {
            return;
        }
        if (demandNode.TryGetValue(demandType.Value, out QuestNode node))
        {
            node.Run();
        }
    }
}
