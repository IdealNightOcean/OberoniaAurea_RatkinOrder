using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 善行任务Def
/// </summary>
public class MercyQuestDef : Def
{
    /// <summary>
    /// 是否需要前置任务
    /// </summary>
    public bool needPreQuest = true;
    /// <summary>
    /// 前置任务Def（可选）
    /// </summary>
    public QuestScriptDef preQuestDef;

    /// <summary>
    /// 善行任务对应的主任务Def
    /// </summary>
    public QuestScriptDef mainQuestDef;

    /// <summary>
    /// 子派系Def
    /// </summary>
    public FactionDef subFactionDef;
    /// <summary>
    /// 求助者的 PawnKindDef 类型。
    /// </summary>
    public PawnKindDef helpSeekerPawnKind;

    /// <summary>
    /// 是否有父派系
    /// </summary>
    public bool hasParentFaction;

    /// <summary>
    /// 父派系验证参数（可选）
    /// </summary>
    public FactionValidationParams? parentFactionValidationParams;
    /// <summary>
    /// 父派系固定Def（可选）
    /// </summary>
    protected FactionDef fixedParentFactionDef;

    /// <summary>
    /// 父派系寻找器类（可选，需继承自 <see cref="MercyQuestParentFactionFinder"/>）
    /// </summary>
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

    /// <summary>
    /// 求助原因
    /// </summary>
    [MayTranslate]
    public string reasonForHelp;

    /// <summary>
    /// 随机选择权重
    /// </summary>
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