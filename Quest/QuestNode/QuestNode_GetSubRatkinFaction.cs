using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetSubRatkinFaction : QuestNode
{
    [NoTranslate]
    public SlateRef<string> storeAs = KeyLibrary_SlateStoreAs.SubRatkinFactionStoreAs;

    public SlateRef<Faction> parentFaction;
    public SlateRef<FactionDef> parentFactionDef;

    public SlateRef<FactionDef> subFactionDef;

    protected override bool TestRunInt(Slate slate)
    {
        return subFactionDef.GetValue(slate) is not null || slate.TryGet(storeAs.GetValue(slate), out Faction _);
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        if (!slate.TryGet(storeAs.GetValue(slate), out Faction subFaction))
        {
            FactionDef subFactionDef = this.subFactionDef.GetValue(slate) ?? OARO_ModDefOf.OARO_Rakinia_Sub;
            subFaction = ModUtility.GenerateSubRatkinFaction(subFactionDef, parentFactionDef.GetValue(slate), parentFaction.GetValue(slate));
            slate.Set(storeAs.GetValue(slate), subFaction);
        }

        QuestGen.quest.AddInvolvedFaction(subFaction);
    }
}