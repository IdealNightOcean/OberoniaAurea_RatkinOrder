using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStoresReserve : IExposable
{
    public BranchBuildingDef targetBuilding;
    public bool inSpecialSlot;
    public BranchFacilityDef targetFacility;

    public float costRate = 1f;


    public void SetReserve(BranchBuildingDef targetBuilding, bool inSpecialSlot, float costRate = 1f)
    {
        this.targetBuilding = targetBuilding;
        this.inSpecialSlot = inSpecialSlot;
        targetFacility = null;
        this.costRate = costRate;
    }

    public void SetReserve(BranchFacilityDef targetFacility, float costRate = 1f)
    {
        targetBuilding = null;
        inSpecialSlot = false;
        this.targetFacility = targetFacility;
        this.costRate = costRate;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref targetBuilding, "targetBuilding");
        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);
        Scribe_Defs.Look(ref targetFacility, "targetFacility");
        Scribe_Values.Look(ref costRate, "costRate", 1f);
    }
}

public class BranchStoresReserveHandler(Branch branch) : IExposable, ITickHourOfDay, IDrawDevWindow
{
    [Unsaved] public readonly Branch Branch = branch ?? throw new ArgumentNullException(nameof(branch));

    private static readonly float[] CostRateReduceArr = [0.02f, 0.005f, 0.0025f];
    private const int StoresReserveCeiling = 3;

    private List<BranchStoresReserve> storesReserves = new(StoresReserveCeiling);
    public BranchStoresReserve PrimaryReserves => storesReserves.FirstOrFallback(null);
    public float PrimaryCostRate => PrimaryReserves?.costRate ?? 1f;

    public bool thoroughPreparation;

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"HasThoroughPreparation: {thoroughPreparation}");
        listing_Rect.Label("StoresReserves:");
        if (storesReserves.Count > 0)
        {
            foreach (BranchStoresReserve reserve in storesReserves)
            {
                if (reserve.targetBuilding is not null)
                {
                    listing_Rect.SubLabel($"{reserve.targetBuilding.label} | {reserve.inSpecialSlot} | {reserve.costRate}", 0.8f);
                }
                else
                {
                    listing_Rect.SubLabel($"{reserve.targetFacility.label} | {reserve.costRate}", 0.8f);
                }
            }
        }
        else
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
    }

    public void SetPrimaryReserves(BranchBuildingDef def, bool inSpecialSlot)
    {
        if (storesReserves.NullOrEmpty())
        {
            storesReserves.Add(new BranchStoresReserve() { targetBuilding = def, inSpecialSlot = inSpecialSlot });
            return;
        }

        if (storesReserves[0].targetBuilding == def)
        {
            storesReserves[0].inSpecialSlot = inSpecialSlot;
        }
        else
        {
            bool exist = false;
            for (int i = 1; i < storesReserves.Count; i++)
            {
                if (storesReserves[i].targetBuilding == def)
                {
                    storesReserves[0].SetReserve(def, inSpecialSlot, storesReserves[i].costRate);
                    storesReserves.RemoveAt(i);
                    exist = true;
                    break;
                }
            }

            if (!exist)
            {
                storesReserves[0].SetReserve(def, inSpecialSlot);
            }
        }
    }

    public void SetPrimaryReserves(BranchFacilityDef def)
    {
        if (storesReserves.NullOrEmpty())
        {
            storesReserves.Add(new BranchStoresReserve() { targetFacility = def });
            return;
        }

        if (storesReserves[0].targetFacility != def)
        {
            bool exist = false;
            for (int i = 1; i < storesReserves.Count; i++)
            {
                if (storesReserves[i].targetFacility == def)
                {
                    storesReserves[0].SetReserve(def, storesReserves[i].costRate);
                    storesReserves.RemoveAt(i);
                    exist = true;
                    break;
                }
            }

            if (!exist)
            {
                storesReserves[0].SetReserve(def);
            }
        }
    }

    public void AddNewReserve(BranchBuildingDef def, bool inSpecialSlot)
    {
        if (def is null || storesReserves.Count >= StoresReserveCeiling)
        {
            return;
        }

        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].targetBuilding == def)
            {
                return;
            }
        }

        storesReserves.Add(new BranchStoresReserve() { targetBuilding = def, inSpecialSlot = inSpecialSlot });
    }

    public void AddNewReserve(BranchFacilityDef def)
    {
        if (def is null || storesReserves.Count >= StoresReserveCeiling)
        {
            return;
        }

        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].targetFacility == def)
            {
                return;
            }
        }

        storesReserves.Add(new BranchStoresReserve() { targetFacility = def });
    }

    /// <summary>
    /// 应该只在分部无任何建设时执行
    /// </summary>
    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == 5)
        {
            float coseRateFloor = thoroughPreparation ? 0.6f : 0.7f;
            float reduceMulti = thoroughPreparation ? 2f : 1f;
            for (int i = 0; i < storesReserves.Count; i++)
            {
                storesReserves[i].costRate = Mathf.Max(storesReserves[i].costRate - (CostRateReduceArr[Mathf.Min(i, 2)] * reduceMulti), coseRateFloor);
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

    public void Notify_BuildingConstructStarted(BranchBuildingDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].targetBuilding == def)
            {
                storesReserves.RemoveAt(i);
                break;
            }
        }
    }

    public void Notify_FacilityConstructionStarted(BranchFacilityDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].targetFacility == def)
            {
                storesReserves.RemoveAt(i);
                break;
            }
        }
    }

    public float GetBuildingCostReduce(BranchBuildingDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].targetBuilding == def)
            {
                return storesReserves[i].costRate;
            }
        }
        return 1f;
    }

    public float GetFacilityCostReduce(BranchFacilityDef def)
    {
        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].targetFacility == def)
            {
                return storesReserves[i].costRate;
            }
        }
        return 1f;
    }

    private void TryCreateNewReserves()
    {
        BranchBuildingHandler buildingHandler = Branch.BuildingHandler;
        if (!buildingHandler.IsNormalBuildingFullyCompleted && buildingHandler.HasUnusedNormalSlots)
        {
            List<BranchBuildingDef> potentialDefs = [];
            List<BranchBuildingDef> allDefs = DefDatabase<BranchBuildingDef>.AllDefsListForReading;
            int maxPotential = 10;
            int curPotential = 0;

            BranchBuildingDef buildingDef;
            BranchBuildingConstructParameter constructParam = new()
            {
                branch = Branch,
                InSpecialSlot = false
            };

            for (int i = 0; i < allDefs.Count; i++)
            {
                buildingDef = allDefs[i];
                constructParam.buildingDef = buildingDef;
                if (buildingHandler.CanConstructBuilding(constructParam, resultOnly: true))
                {
                    potentialDefs.Add(buildingDef);
                    curPotential++;
                    if (curPotential >= maxPotential)
                    {
                        break;
                    }
                }
            }

            if (curPotential != 0)
            {
                BranchBuildingDef targetDef = potentialDefs.RandomElement();
                AddNewReserve(targetDef, inSpecialSlot: false);
                return;
            }
        }

        BranchFacilityHandler facilityHandler = Branch.FacilityHandler;
        if (!facilityHandler.IsFacilityFullyCompleted)
        {
            (BranchFacilityDef fDef, BranchFacilityLevel fLevel) = facilityHandler.Facilities.MinBy(kv => kv.Value);
            if (fDef is not null && fLevel < BranchFacilityLevel.Excellent)
            {
                AddNewReserve(fDef);
            }
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref thoroughPreparation, "thoroughPreparation", defaultValue: false);

        Scribe_Collections.Look(ref storesReserves, "storesReserves", LookMode.Deep);
    }
}

