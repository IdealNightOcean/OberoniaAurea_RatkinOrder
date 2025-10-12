using RimWorld.QuestGen;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemand_Critical : BranchDemand
{
    public BranchDemand_Critical() : base() { }
    protected BranchDemand_Critical(BranchDemandDef def) : base(def) { }

    protected override Slate GenerateQuestSlate(Branch branch)
    {
        Slate slate = base.GenerateQuestSlate(branch);
        List<QuestEffectTag> tags = Def.GetModExtension<DemandPreSetQuestEffectTags>()?.GetEffectTags();
        if (tags is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.PreSetQuestEffectTags, tags);
        }
        return slate;
    }
}
