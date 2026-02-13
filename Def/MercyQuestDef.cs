using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
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

    public Type parentFactionFinderClass;
    private MercyQuestParentFactionFinder parentFactionFinder;
    public MercyQuestParentFactionFinder ParentFactionFinder
    {
        get
        {
            if (parentFactionFinder is null)
            {
                parentFactionFinderClass ??= typeof(MercyQuestParentFactionFinder_Default);
                parentFactionFinder = (MercyQuestParentFactionFinder)Activator.CreateInstance(parentFactionFinderClass);
            }
            return parentFactionFinder;
        }
    }

    [MayTranslate]
    public string reasonForHelp;

    public float selectWeight = 1f;

    public override IEnumerable<string> ConfigErrors()
    {
        if (needPreQuest && preQuestDef is null)
        {
            yield return $"'{nameof(preQuestDef)}' 为 null。";
        }
        if (mainQuestDef is null)
        {
            yield return $"'{nameof(mainQuestDef)}' 为 null。";
        }
        if (subFactionDef is null)
        {
            yield return $"'{nameof(subFactionDef)}' 为 null。";
        }
        if (needPreQuest && helpSeekerPawnKind is null)
        {
            yield return $"'{nameof(needPreQuest)}' 为 true，但 '{nameof(helpSeekerPawnKind)}' 为 null。";
        }
        if (hasParentFaction && parentFactionFinderClass is null)
        {
            parentFactionFinderClass = typeof(MercyQuestParentFactionFinder_Default);
            yield return $"'{nameof(hasParentFaction)}' 为 true，但 '{nameof(parentFactionFinderClass)}' 为 null，已设置为 {nameof(MercyQuestParentFactionFinder_Default)}。";
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
            Faction parentFaction = ParentFactionFinder?.FindParentFaction(this, parentFactionValidationParams, fixedParentFactionDef);
            if (parentFaction is null)
                return false;

            slate.Set(KeyLibrary_SlateStoreAs.parentFactionDef, parentFaction.def);
            slate.Set(KeyLibrary_SlateStoreAs.parentFaction, parentFaction);
        }

        return true;
    }
}