using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_DemandTypeSwitch : QuestNode
{
    public SlateRef<DemandType?> demandType;
    public Dictionary<DemandType, QuestNode> demandNode;

    protected override bool TestRunInt(Slate slate)
    {
        if (demandNode.NullOrEmpty())
        {
            return true;
        }
        DemandType? demandType = this.demandType.GetValue(slate) ?? slate.Get<DemandType>(KeyLibrary_SlateStoreAs.demandType);
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
        DemandType? demandType = this.demandType.GetValue(slate) ?? slate.Get<DemandType>(KeyLibrary_SlateStoreAs.demandType);
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