using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchDemandDef : Def
{
    private static readonly Type DefaultWeighterClass = typeof(BranchDemandWeighter);
    private static readonly BranchDemandWeighter DefaultWeighter = new();
    public Type weighterClass = DefaultWeighterClass;
    private BranchDemandWeighter weighter;
    public BranchDemandWeighter Weighter => weighter ??= (weighterClass == DefaultWeighterClass) ? DefaultWeighter : (BranchDemandWeighter)Activator.CreateInstance(weighterClass);

    public Type demandClass = typeof(BranchDemand);

    public BranchDemandType demandType;
    public float durationDays; //未接取时的持续时间，超过该时间仍未接取则会被移除
    public QuestScriptDef relatedQuestDef;
    public float baseSelectWeight = 100f;

    public int DurationTicks => (int)(durationDays * 60000f);
    public bool IsCriticalDemand => demandType == BranchDemandType.Important || demandType == BranchDemandType.Core;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (relatedQuestDef is null)
        {
            yield return $"{defName} has a null relatedQuestDef.";
        }
        if (durationDays <= 0f)
        {
            yield return $"{defName} should has a positive durationnDays.";
        }
    }

    public BranchDemand MakeBranchDemand()
    {
        return (BranchDemand)Activator.CreateInstance(demandClass, this);
    }
}
