using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class BranchStoresReserveHandler : IExposable, ITickHourOfDay
{
    [Unsaved] private readonly Branch branch;

    private static readonly float[] CostRateReduceArr = [0.02f, 0.01f, 0.005f, 0.0025f];
    private const int StoresReserveCeiling = 3;

    private List<ReserveRecord> storesReserves = new(StoresReserveCeiling);
    public IReadOnlyList<ReserveRecord> StoresReserves => storesReserves;

    public ReserveRecord PrimaryReserves => storesReserves.FirstOrFallback(null);
    public float PrimaryCostRateReduce => PrimaryReserves?.CostRateReduce ?? 0f;

    internal BranchStoresReserveHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref storesReserves, nameof(storesReserves), LookMode.Deep);
    }

    internal void PostLoadInit()
    {
        if (storesReserves.RemoveAll(r => r.Target is null) > 0)
        {
            Log.Error($"[OARO] Some Reserves of {branch} were null after loading and have been removed.");
        }
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"建材储备目标数: {storesReserves.Count}");
        if (storesReserves.Count > 0)
        {
            for (int i = 0; i < storesReserves.Count; i++)
            {
                listing_Rect.SubLabel(storesReserves[i].ToString(), 0.8f);
            }
        }
        else
        {
            listing_Rect.SubLabel("None".Translate(), 0.8f);
        }
    }

    public void SetPrimaryReserves(BranchConstructionDef def)
    {
        if (storesReserves.Count == 0)
        {
            storesReserves.Add(ReserveRecord.GenrateNewRecord(def));
        }

        if (storesReserves[0].Target == def)
        {
            return;
        }

        for (int i = 1; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                storesReserves.Swap(0, i);
                return;
            }
        }

        storesReserves.Insert(0, ReserveRecord.GenrateNewRecord(def));
    }

    public void AddNewReserve(BranchConstructionDef def)
    {
        if (def is null)
        {
            return;
        }

        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                return;
            }
        }

        storesReserves.Add(ReserveRecord.GenrateNewRecord(def));
    }

    /// <summary>
    /// 应该只在分部无任何建设时执行
    /// </summary>
    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == 5)
        {
            float maxReduce = 0.3f;// ThoroughPreparation ? 0.4f : 0.3f;
            float reduceMulti = 1f;// ThoroughPreparation ? 2f : 1f;
            for (int i = 0; i < storesReserves.Count; i++)
            {
                float costRateReduce = storesReserves[i].CostRateReduce - (CostRateReduceArr[Mathf.Min(i, 3)] * reduceMulti);
                storesReserves[i].CostRateReduce = Mathf.Min(costRateReduce, maxReduce);
            }
        }
        else if (hourOfDay == 17)
        {
            if (storesReserves.Count < StoresReserveCeiling && Rand.Chance(0.1f))
            {
                TryCreateNewReserves();
            }
        }
    }

    public void RemoveReserve(BranchConstructionDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                storesReserves.RemoveAt(i);
                break;
            }
        }
    }

    public void Notify_BranchConstructStarted(BranchConstructionDef def)
    {
        RemoveReserve(def);
    }

    public float GetReserveCostReduce(BranchConstructionDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                return storesReserves[i].CostRateReduce;
            }
        }
        return 0f;
    }

    private void TryCreateNewReserves()
    {
        BranchBuildingHandler buildingHandler = branch.BuildingHandler;

        if (!buildingHandler.IsNormalBuildingFullyCompleted && buildingHandler.HasUnusedNormalSlots)
        {
            if (TryAddBuildingReserve(isSpecial: false))
            {
                return;
            }
        }

        BranchFacilityHandler facilityHandler = branch.FacilityHandler;
        if (!facilityHandler.IsFacilityFullyCompleted)
        {
            (BranchFacilityDef fDef, BranchFacilityLevel fLevel) = facilityHandler.Facilities.MinBy(kv => kv.Value);
            if (fDef is not null && fLevel < BranchFacilityLevel.Excellent)
            {
                AddNewReserve(fDef);
                return;
            }
        }

        if (buildingHandler.SpecialBuilding is null)
        {
            TryAddBuildingReserve(isSpecial: true);
        }

        bool TryAddBuildingReserve(bool isSpecial)
        {
            List<BranchBuildingDef> allDefs = DefDatabase<BranchBuildingDef>.AllDefsListForReading;
            ConcurrentBag<BranchBuildingDef> potentialBuildings = [];
            ParallelOptions options = new() { MaxDegreeOfParallelism = 8 }; // 最多使用8个线程
            Parallel.For(0, allDefs.Count, options, (i, state) =>
            {
                if (potentialBuildings.Count >= 10)
                {
                    state.Stop();
                    return;
                }

                if (allDefs[i].isSpecial != isSpecial)
                {
                    return;
                }

                BranchBuildingConstructParms constructParam = new()
                {
                    Branch = branch,
                    BuildingDef = allDefs[i],
                };
                if (buildingHandler.CanConstructBuilding(constructParam, resultOnly: true))
                {
                    potentialBuildings.Add(allDefs[i]);
                    if (potentialBuildings.Count >= 10)
                    {
                        state.Stop();
                    }
                }
            });

            if (potentialBuildings.Count > 0)
            {
                BranchBuildingDef targetDef = potentialBuildings.RandomElement();
                AddNewReserve(targetDef);
                potentialBuildings = null;
                return true;
            }
            return false;
        }
    }
}