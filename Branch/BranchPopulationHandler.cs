using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchPopulationHandler : IExposable, ITickDay
{
    [Unsaved] private readonly Branch branch;
    private int population;
    public int Population
    {
        get { return population; }
        set { population = value > 0 ? value : 0; }
    }

    private int yesterdayPopulation;
    private int yesterdayPopChange;
    private readonly SimpleValueCache<float> naturalPopulationCeilingCache;
    public float PopulationRatio => population / naturalPopulationCeilingCache.GetCachedResult();


    private float publicSecurity = 1f;
    public float PublicSecurity
    {
        get => publicSecurity;
        set => publicSecurity = Mathf.Clamp(value, 0.5f, 1.5f);
    }
    private float yesterdayPublicSecurity = 1f;
    private float yesterdayPublicSecChange;

    private List<BranchContract> contracts = [];
    public IReadOnlyList<BranchContract> Contracts => contracts;
    private bool hasContractBuff;
    public bool HasContractBuff => hasContractBuff;

    private static readonly int[] contractCeilingArr = [0, 500, 1500, 3000];
    public int PopulationLimitByIndex(int index) => contractCeilingArr[Mathf.Clamp(index, 0, 3)];
    public int ContractCeilingByPop
    {
        get
        {
            return population switch
            {
                >= 3000 => RatkinOrderSettings.MaxConcurrentContractPerBranch,
                >= 1500 => 3,
                >= 500 => 2,
                _ => 1
            };
        }
    }

    internal BranchPopulationHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        naturalPopulationCeilingCache = new SimpleValueCache<float>(
            cacheInterval: 2500,
            defaultValue: BranchStatDefOf.OARO_NaturalPopulationCeiling.baseValue,
            checker: () => this.branch.GetStatValue(BranchStatDefOf.OARO_NaturalPopulationCeiling));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref population, "population", 0);
        Scribe_Values.Look(ref yesterdayPopulation, "yesterdayPopulation", 0);
        Scribe_Values.Look(ref yesterdayPopChange, "yesterdayChange", 0);

        Scribe_Values.Look(ref publicSecurity, "publicSecurity", 1f);
        Scribe_Values.Look(ref yesterdayPublicSecurity, "yesterdayPublicSecurity", 1f);
        Scribe_Values.Look(ref yesterdayPublicSecChange, "yesterdayPublicSecChange", 0f);

        Scribe_Collections.Look(ref contracts, "contracts", LookMode.Deep);
        Scribe_Values.Look(ref hasContractBuff, "hasContractBuff", defaultValue: false);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"人口数: {population}");
        listing_Rect.Label($"昨日人口数: {yesterdayPopulation}");
        listing_Rect.Label($"昨日人口变化: {yesterdayPopChange}");

        listing_Rect.Gap(6f);
        listing_Rect.Label($"治安度: {publicSecurity.ToStringPercent()}");
        listing_Rect.Label($"昨日治安度: {yesterdayPublicSecurity.ToStringPercent()}");
        listing_Rect.Label($"昨日治安度变化: {yesterdayPublicSecChange.ToStringPercentSigned()}");

        listing_Rect.Gap(6f);
        listing_Rect.Label($"有无平民需求（合约）完成Buff: {hasContractBuff}");
        listing_Rect.Label($"平民需求（合约）: {contracts.Count}");
        foreach (BranchContract contract in contracts)
        {
            listing_Rect.SubLabel($"{contract.RequestThingDef.label} × {contract.RequestCount}", 0.8f);
        }
    }

    public void TickDay()
    {
        bool onMartialLaw = branch.EffectTags.HasTag(KeyLibrary_EffectTag.MartialLaw);
        DailyPopulationChange(onMartialLaw);
        DailyPublicSecurityCheck(onMartialLaw);
        DailyContractCheck();

        if (!branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.ContractAddCheck))
        {
            ContractAddCheck();
        }
    }

    public void Notify_ContractCompleted()
    {
        hasContractBuff = true;
    }

    /// <summary>
    /// 每日人口变化
    /// </summary>
    private void DailyPopulationChange(bool onMartialLaw)
    {
        if (!onMartialLaw)
        {
            float dailyGrowth = branch.GetStatValue(BranchStatDefOf.OARO_DailyPopulationGrowth);
            dailyGrowth *= Rand.Range(0.75f, 1.25f);
            population += Mathf.RoundToInt(dailyGrowth);
        }

        yesterdayPopChange = population - yesterdayPopulation;
        yesterdayPopulation = population;
    }

    private void DailyContractCheck()
    {
        contracts.RemoveAll(c => c.ShouldRemove);
        for (int i = 0; i < contracts.Count; i++)
        {
            if (contracts[i].CurState == BranchContract.ContractState.Cooling)
            {
                hasContractBuff = true;
                return;
            }
        }
        hasContractBuff = false;
    }

    private void DailyPublicSecurityCheck(bool onMartialLaw)
    {
        if (!branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.PublicSecurityCheck))
        {
            branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.PublicSecurityCheck, cdTicks: 3 * 60000, removeWhenExpired: false);
            if (Rand.Chance(0.2f))
            {
                PublicSecurity -= (publicSecurity * 0.05f) * Rand.Range(0.5f, 1.5f);
            }
        }
        if (onMartialLaw)
        {
            PublicSecurity += 0.02f;
        }

        yesterdayPublicSecChange = publicSecurity - yesterdayPopulation;
        yesterdayPublicSecurity = publicSecurity;
    }

    private void ContractAddCheck()
    {
        branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.ContractAddCheck, cdTicks: 5 * 60000, removeWhenExpired: false);
        int startInex = contracts.Count;
        int endIndex = ContractCeilingByPop;
        if (startInex >= endIndex)
        {
            return;
        }

        for (int i = startInex; i < endIndex; i++)
        {
            if (Rand.Chance(0.2f))
            {
                BranchContractDef contractDef = DefDatabase<BranchContractDef>.AllDefsListForReading.RandomElement();
                TryAddContract(contractDef);
            }
        }
    }

    public bool TryAddContract(BranchContractDef contractDef)
    {
        BranchContract contract = BranchContract.MakeBranchContract(contractDef);
        contract.PostInit(branch);
        if (contract.CurState == BranchContract.ContractState.Ongoing)
        {
            contracts.Add(contract);
            return true;
        }
        return false;
    }

    internal void PostBranchGenerated()
    {
        population = (int)(naturalPopulationCeilingCache.GetCachedResult() * Rand.Range(0.3f, 0.7f));
        yesterdayPopulation = population;

        publicSecurity = 1f;
        yesterdayPublicSecurity = 1f;
    }

    internal void PostLoadInit()
    {
        if (contracts.RemoveAll(c => c is null) > 0)
        {
            Log.Error($"[OARO] Some of contracts in {branch} were null after loading. Removed.");
        }
    }
}