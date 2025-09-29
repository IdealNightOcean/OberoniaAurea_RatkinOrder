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
    public bool needParentFaction;
    public PawnKindDef helpSeekerPawnKind;

    [MayTranslate]
    public string fixedQuestDesc;

    public RulePackDef questDescMaker;

    public bool TrySetQuestSlateValue(Slate slate)
    {
        if (parentFactionDef is not null)
        {
            if (needParentFaction)
            {
                Faction parentFaction = OAFrame_FactionUtility.RandomAvailableFactionOfDef(parentFactionDef, FactionValidationParams.NonHostileNormalFaction);
                if (parentFaction is null)
                {
                    return false;
                }
                slate.Set(KeyLibrary_SlateStoreAs.ParentFaction, parentFaction);
            }

            slate.Set(KeyLibrary_SlateStoreAs.ParentFactionDef, parentFactionDef);
        }

        slate.Set(KeyLibrary_SlateStoreAs.SubFactionDef, subFactionDef ?? OARO_ModDefOf.OARO_Rakinia_Sub);
        slate.Set(KeyLibrary_SlateStoreAs.HelpSeekerPawnKind, helpSeekerPawnKind ?? OARO_PawnKindDefOf.RatkinColonist);
        return true;
    }
}
