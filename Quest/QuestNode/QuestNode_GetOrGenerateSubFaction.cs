using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_GetOrGenerateSubFaction : QuestNode
{
    [NoTranslate]
    public SlateRef<string> storeAs = KeyLibrary_SlateStoreAs.SubFaction;

    public SlateRef<Faction> parentFaction;
    public SlateRef<FactionDef> parentFactionDef;

    public SlateRef<FactionDef> subFactionDef;

    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;

        if (!slate.TryGet(storeAs.GetValue(slate), out Faction subFaction))
        {
            Faction parentFaction = this.parentFaction.GetValue(slate)
                                    ?? slate.Get<Faction>(KeyLibrary_SlateStoreAs.ParentFaction);

            FactionDef parentFactionDef = this.parentFactionDef.GetValue(slate)
                                          ?? slate.Get<FactionDef>(KeyLibrary_SlateStoreAs.ParentFactionDef);

            FactionDef subFactionDef = this.subFactionDef.GetValue(slate)
                                       ?? slate.Get<FactionDef>(KeyLibrary_SlateStoreAs.SubFactionDef)
                                       ?? OARO_ModDefOf.OARO_SubRakinia_Neutral;

            subFaction = ModUtility.GenerateSubRatkinFaction(subFactionDef, parentFactionDef, parentFaction);
            slate.Set(storeAs.GetValue(slate), subFaction);
        }

        QuestGen.quest.AddInvolvedFaction(subFaction);
    }
}