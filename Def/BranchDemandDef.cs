using RimWorld;
using System;
using System.Collections.Generic;
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

    public DemandType demandType;

    /// <summary>
    /// 未接取时的持续时间，超过该时间仍未接取则会被移除
    /// </summary>
    public float durationDays;
    public QuestScriptDef relatedQuestDef;
    public float baseSelectWeight = 100f;

    [MustTranslate]
    public string targetDesc;
    [MustTranslate]
    public string rewardDesc;

    public int DurationTicks => (int)(durationDays * 60000f);
    public bool IsCritical => demandType == DemandType.Critical;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (weighterClass is null)
        {
            weighterClass = defaultWeighterClass;
            yield return "has a null weighterClass. Set to default.";
        }
        if (demandClass is null)
        {
            demandClass = defaultWorkClass;
            yield return "has a null demandClass. Set to default.";
        }
        if (relatedQuestDef is null)
        {
            yield return "has a null relatedQuestDef.";
        }
        if (durationDays <= 0f)
        {
            yield return "should has a positive durationnDays.";
        }
    }
}