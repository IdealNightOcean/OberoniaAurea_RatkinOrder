using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestExtension : DefModExtension
{
    public QuestScriptDef preQuestDef;
    public FactionDef subFactionDef;
    public FactionDef parentFactionDef;
    public PawnKindDef helpSeekerPawnKind;

    [MayTranslate]
    public string fixedQuestDesc;

    public RulePackDef questDescMaker;

    public bool TrySetQuestSlateValue(Slate slate)
    {
        if (parentFactionDef is not null)
        {
            Faction parentFaction = OAFrame_FactionUtility.RandomAvailableFactionOfDef(parentFactionDef, OAFrame_FactionUtility.NonHostileNormalFactionParams);
            if (parentFaction is null)
            {
                return false;
            }
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionDef, parentFaction.def);
            slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFaction, parentFaction);
        }

        slate.Set(KeyLibrary_SlateStoreAs.SubRatkinFactionDef, subFactionDef ?? OARO_ModDefOf.OARO_Rakinia_Sub);
        slate.Set(KeyLibrary_SlateStoreAs.HelpSeekerPawnKind, helpSeekerPawnKind ?? OARO_PawnKindDefOf.RatkinColonist);
        return true;
    }
}
