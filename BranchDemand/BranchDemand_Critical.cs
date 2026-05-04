using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemand_Critical : BranchDemand
{
    private IReadOnlyList<KnightChivalryDef> potentialMedals;
    public IReadOnlyList<KnightChivalryDef> PotentialMedals => potentialMedals ??= (Def.GetModExtension<CriticalDemand_Extension>()?.potentialMedals ?? []);

    private List<QuestEffectTag> questEffectTags;
    public IReadOnlyList<QuestEffectTag> QuestEffectTags => questEffectTags;

    public override void PostInit(Branch branch)
    {
        base.PostInit(branch);
        questEffectTags = Def.GetModExtension<CriticalDemand_Extension>()?.GetEffectTags();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref questEffectTags, "questEffectTags", LookMode.Deep);
    }

    protected override Slate GenerateQuestSlate(Branch branch)
    {
        Slate slate = base.GenerateQuestSlate(branch);
        if (questEffectTags is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.preSetQuestEffectTags, questEffectTags);
        }
        if (PotentialMedals is not null)
        {
            slate.Set(KeyLibrary_SlateStoreAs.preSetPotentialMedals, PotentialMedals);
        }
        return slate;
    }
}