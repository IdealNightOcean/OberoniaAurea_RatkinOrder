using OberoniaAurea_Frame;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchPopulationHandler : IExposable, ITickDay
{
    [Unsaved] private readonly Branch branch;
    private int population;
    private int yesterdayPopulation;
    private int yesterdayChange;
    public float PopulationRatio => population / naturalPopulationCeilingCache.GetCachedResult();

    public int Population
    {
        get { return population; }
        set { population = value > 0 ? value : 0; }
    }

    private readonly SimpleValueCache<float> naturalPopulationCeilingCache;

    internal BranchPopulationHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        naturalPopulationCeilingCache = new SimpleValueCache<float>(
            cacheInterval: 60000,
            defaultValue: BranchStatDefOf.OARO_NaturalPopulationCeiling.baseValue,
            checker: () => this.branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref population, "population", 0);
        Scribe_Values.Look(ref yesterdayPopulation, "yesterdayPopulation", 0);
        Scribe_Values.Look(ref yesterdayChange, "yesterdayChange", 0);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"Population: {population}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"YesterdayPopulation: {yesterdayPopulation}");
        listing_Rect.Label($"YesterdayChange: {yesterdayChange}");
    }

    public void TickDay()
    {
        DailyPopulationChange();
    }

    /// <summary>
    /// 每日人口变化
    /// </summary>
    private void DailyPopulationChange()
    {
        float dailyGrowth = branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);

        dailyGrowth *= Rand.Range(0.75f, 1.25f);

        population += Mathf.RoundToInt(dailyGrowth);
        yesterdayChange = population - yesterdayPopulation;
        yesterdayPopulation = population;
    }

    internal void PostBranchGenerated()
    {
        population = (int)(naturalPopulationCeilingCache.GetCachedResult() * 0.5f);
    }
}