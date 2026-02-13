using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandDef : Def
{
    private static readonly Type defaultWeighterClass = typeof(BranchDemandWeighter);
    private static readonly BranchDemandWeighter defaultWeighter = new();
    private static readonly Type defaultWorkClass = typeof(BranchDemand);

    public Type weighterClass = defaultWeighterClass;
    private BranchDemandWeighter weighter;
    public BranchDemandWeighter Weighter => weighter ??= (weighterClass == defaultWeighterClass) ? defaultWeighter : (BranchDemandWeighter)Activator.CreateInstance(weighterClass);

    public Type demandClass = defaultWorkClass;

    /// <summary>
    /// 需求类型
    /// </summary>
    public DemandType demandType;

    /// <summary>
    /// 需求在[接取前]的持续时间（Day）
    /// </summary>
    /// <remarks>超过该时间仍未接取则会被移除</remarks>
    public float durationDays;

    /// <summary>
    /// 需求相关的<see cref="QuestScriptDef"/>
    /// </summary>
    public QuestScriptDef relatedQuestDef;

    /// <summary>
    /// 需求随机权重
    /// </summary>
    public float baseSelectWeight = 100f;

    /// <summary>
    /// 目标描述
    /// </summary>
    [MustTranslate]
    public string targetDesc;

    /// <summary>
    /// 奖励描述
    /// </summary>
    [MustTranslate]
    public string rewardDesc;

    public int DurationTicks => (int)(durationDays * 60000f);

    /// <summary>
    /// 是否为关键需求（<see cref="DemandType.Critical"/>）
    /// </summary>
    public bool IsCritical => demandType == DemandType.Critical;

    /// <summary>
    /// 关键需求背景图标
    /// </summary>
    /// <remarks>- 目前仅在 <see cref="demandType"/> 为 <see cref="DemandType.Critical"/> 时使用</remarks>
    [NoTranslate]
    protected string backgroundPath;
    protected Texture2D backgroundTexture;
    public Texture2D BackgroundTexture
    {
        get
        {
            if (backgroundTexture is null)
            {
                if (string.IsNullOrEmpty(backgroundPath))
                {
                    return null;
                }
                backgroundTexture = ContentFinder<Texture2D>.Get(backgroundPath);
            }
            return backgroundTexture;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (weighterClass is null)
        {
            weighterClass = defaultWeighterClass;
            yield return $"'{nameof(weighterClass)}' 为 null，已设置为默认值。";
        }
        if (demandClass is null)
        {
            demandClass = defaultWorkClass;
            yield return $"'{nameof(demandClass)}' 为 null，已设置为默认值。";
        }
        if (relatedQuestDef is null)
        {
            yield return $"'{nameof(relatedQuestDef)}' 为 null。";
        }
        if (durationDays <= 0f)
        {
            yield return $"'{nameof(durationDays)}' 必须大于 0。";
        }
    }
}