using RimWorld;
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

    public bool HasReservesOf(BranchConstructionDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                return true;
            }
        }
        return false;
    }

    public void RemoveReserves(int index) => storesReserves.RemoveAt(index);
    public int RemoveReserves(BranchConstructionDef def) => storesReserves.RemoveAll(r => r.Target == def);

    public void SetReserves(BranchConstructionDef def, int index)
    {
        if (index < 0 || index >= storesReserves.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (def == storesReserves[index].Target)
        {
            return;
        }

        int existDefIndex = -1;
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].Target == def)
            {
                existDefIndex = i;
                break;
            }
        }

        if (existDefIndex >= 0)
        {
            storesReserves.Swap(existDefIndex, index);
        }
        else
        {
            storesReserves[index] = ReserveRecord.GenrateNewRecord(def);
        }
    }

    public void AddNewReserve(BranchConstructionDef def)
    {
        if (!HasReservesOf(def))
        {
            storesReserves.Add(ReserveRecord.GenrateNewRecord(def));
        }
    }

    /// <summary>
    /// 应该只在分部无任何建设时执行
    /// </summary>
    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == 5)
        {
            float maxReduce = branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder) ? 0.4f : 0.3f;
            float reduceMulti = branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder) ? 2f : 1f;
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
                TryCreateAutoStartReserves();
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

    private void TryCreateAutoStartReserves()
    {
        if (!branch.FacilityHandler.IsFacilityFullyCompleted)
        {
            List<BranchFacilityDef> potentialFacilities = [];
            foreach (BranchFacilityDef facilityDef in DefDatabase<BranchFacilityDef>.AllDefsListForReading)
            {
                if (BranchUtility.CanStoreReserve(branch, facilityDef))
                {
                    potentialFacilities.Add(facilityDef);
                }
            }

            if (potentialFacilities.Count > 0)
            {
                BranchFacilityDef facilityDef = potentialFacilities.RandomElement();
                AddNewReserve(facilityDef);
                return;
            }
        }


        BranchBuildingHandler buildingHandler = branch.BuildingHandler;
        if (!buildingHandler.IsNormalBuildingFullyCompleted && buildingHandler.HasUnusedNormalSlots)
        {
            ConcurrentBag<BranchBuildingDef> potentialBuildings = [];

            List<BranchBuildingDef> allBuildingDefs = DefDatabase<BranchBuildingDef>.AllDefsListForReading;
            ParallelOptions options = new() { MaxDegreeOfParallelism = 8 }; // 最多使用8个线程
            Parallel.For(0, allBuildingDefs.Count, options, (i, state) =>
            {
                if (potentialBuildings.Count >= 10)
                {
                    state.Stop();
                    return;
                }

                if (BranchUtility.CanStoreReserve(branch, allBuildingDefs[i]))
                {
                    potentialBuildings.Add(allBuildingDefs[i]);
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
            }
        }
    }
}