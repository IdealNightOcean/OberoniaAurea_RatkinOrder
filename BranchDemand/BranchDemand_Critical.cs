using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemand_Critical : BranchDemand
{
    public BranchDemand_Critical() : base() { }
    public BranchDemand_Critical(BranchDemandDef def) : base(def) { }

    protected override Slate GenerateQuestSlate(Branch branch)
    {
        Slate slate = base.GenerateQuestSlate(branch);
        Log.Message("try0");
        SetPreSetQuestEffectTags(slate);
        return slate;
    }

    protected void SetPreSetQuestEffectTags(Slate slate)
    {
        Log.Message("try1");
        List<string> tags = Def.GetModExtension<DemandPreSetQuestEffectTags>()?.GetEffectTags();
        if (!tags.NullOrEmpty())
        {
            Log.Message($"[BranchDemand_Critical] Set PreSetQuestEffectTags: {string.Join(", ", tags)}");
            slate.Set(KeyLibrary_SlateStoreAs.PreSetQuestEffectTags, tags);
        }
    }
}
