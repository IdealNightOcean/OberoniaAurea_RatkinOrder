using OberoniaAurea_Frame;
using RimWorld;
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
    private int yesterdayChange;
    private readonly SimpleValueCache<float> naturalPopulationCeilingCache;
    public float PopulationRatio => population / naturalPopulationCeilingCache.GetCachedResult();

    private List<BranchContract> contracts = [];
    public IReadOnlyList<BranchContract> Contracts => contracts;
    private bool hasContractBuff;
    public bool HasContractBuff => hasContractBuff;
    public int ContractCeiling
    {
        get
        {
            return population switch
            {
                >= 3000 => 4,
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
        Scribe_Values.Look(ref yesterdayChange, "yesterdayChange", 0);
        Scribe_Collections.Look(ref contracts, "contracts", LookMode.Deep);
        Scribe_Values.Look(ref hasContractBuff, "hasContractBuff", defaultValue: false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            contracts.RemoveAll(c => c is null);
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"Population: {population}");
        listing_Rect.Gap(6f);
        listing_Rect.Label($"YesterdayPopulation: {yesterdayPopulation}");
        listing_Rect.Label($"YesterdayChange: {yesterdayChange}");

        listing_Rect.Label("AllContracts:");
        for (int i = 0; i < contracts.Count; i++)
        {
            BranchContract contract = contracts[i];
            if (listing_Rect.ButtonTextLabeled($"{contract.RequestThingDef.label}×{contract.RequestCount} ({contract.CurState})", "Accept"))
            {
                contracts[i].OnAccepted(branch);
                break;
            }
        }
    }

    public void TickDay()
    {
        DailyPopulationChange();
        DailyContractCheck();
        if (!branch.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.ContractAddCheck))
        {
            ContractAddCheck();
        }
    }

    public void Notify_ContractFinished(Quest quest)
    {
        for (int i = 0; i < contracts.Count; i++)
        {
            if (contracts[i].RelatedQuest == quest)
            {
                bool succeeded = quest.State == QuestState.EndedSuccess;
                hasContractBuff |= succeeded;
                contracts[i].OnContractFinished(succeeded);

                if (contracts[i].CurState != BranchContract.ContractState.Cooling)
                {
                    contracts.RemoveAt(i);
                }

                GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchContractCompleted, 1f, addIfMiss: true);
                break;
            }
        }
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

    private void ContractAddCheck()
    {
        branch.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.ContractAddCheck, cdTicks: 5 * 60000, shouldRemoveWhenExpired: false);
        int startInex = contracts.Count;
        int endIndex = ContractCeiling;
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
        if (contract.CurState == BranchContract.ContractState.NotAccepted)
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
    }
}