using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestDef : Def
{
    public bool needPreQuest = true;
    public QuestScriptDef preQuestDef;

    public QuestScriptDef mainQuestDef;

    public FactionDef subFactionDef;
    public PawnKindDef helpSeekerPawnKind;

    public bool hasParentFaction;
    public FactionValidationParams? parentFactionValidationParams;
    protected FactionDef fixedParentFactionDef;

    [MayTranslate]
    public string fixedHelpDesc;
    public RulePackDef helpDescRulePack;

    public float secondSelectWeight = 1f;

    public override IEnumerable<string> ConfigErrors()
    {
        if (needPreQuest && preQuestDef is null)
        {
            yield return $"has a null {nameof(preQuestDef)}";
        }
        if (mainQuestDef is null)
        {
            yield return $"has a null {nameof(mainQuestDef)}";
        }
        if (subFactionDef is null)
        {
            yield return $"has a null {nameof(subFactionDef)}";
        }
        if (needPreQuest && helpSeekerPawnKind is null)
        {
            yield return $"{nameof(needPreQuest)} is true, but has a null {nameof(helpSeekerPawnKind)}";
        }
    }

    public bool TrySetQuestSlateValue(Slate slate)
    {
        slate.Set(KeyLibrary_SlateStoreAs.mercyQuestDef, this);

        if (subFactionDef is null)
            return false;
        else
            slate.Set(KeyLibrary_SlateStoreAs.subFactionDef, subFactionDef);

        if (needPreQuest && helpSeekerPawnKind is null)
            return false;
        else
            slate.Set(KeyLibrary_SlateStoreAs.helpSeekerPawnKind, helpSeekerPawnKind);

        if (hasParentFaction)
        {
            Faction parentFaction;
            if (fixedParentFactionDef is not null)
            {
                parentFaction = OAFrame_FactionUtility.RandomAvailableFactionOfDef(
                    def: fixedParentFactionDef,
                    validationParams: parentFactionValidationParams ?? FactionValidationParams.NonHostileNormalFaction);
            }
            else
            {
                parentFaction = OAFrame_FactionUtility.RandomAvailableFactionOf(parentFactionValidationParams ?? FactionValidationParams.NonHostileNormalFaction);
            }

            if (parentFaction is null)
            {
                return false;
            }

            slate.Set(KeyLibrary_SlateStoreAs.parentFactionDef, parentFaction.def);
            slate.Set(KeyLibrary_SlateStoreAs.parentFaction, parentFaction);

        }

        return true;
    }
}