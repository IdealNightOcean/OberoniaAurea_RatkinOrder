using OberoniaAurea_Frame;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchPopulationHandler : IExposable
{
    private readonly Branch branch;
    private int population;
    private int lastDayChange;
    public int Population => population;

    private readonly SimpleValueCache<int> naturalPopulationCeilingCache;

    public void ExposeData()
    {
        Scribe_Values.Look(ref population, "population", 0);
        Scribe_Values.Look(ref lastDayChange, "lastDayChange", 0);
    }

    public BranchPopulationHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        naturalPopulationCeilingCache = new SimpleValueCache<int>(
            cacheInterval: 60000,
            defaultValue: (int)BranchStatDefOf.OARO_NaturalPopulationCeiling.baseValue,
            checker: () => (int)this.branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling));
    }

    public void AdjustPopulation(float change) => AdjustPopulation(Mathf.RoundToInt(change));
    public void AdjustPopulation(int change)
    {

    }

    /// <summary>
    /// 每日人口变化
    /// </summary>
    private int GetDailyPopulationDecline()
    {
        float populationRatio = population / (float)naturalPopulationCeilingCache.GetCachedResult();


        return 0;
    }

}