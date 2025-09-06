using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStoresReserve : IExposable
{
    public BranchBuildingDef TargetBuilding;
    public bool InSpecialSlot;
    public BranchFacilityDef TargetFacility;

    public float costRate = 1f;


    public void SetReserve(BranchBuildingDef targetBuilding, bool inSpecialSlot, float costRate = 1f)
    {
        TargetBuilding = targetBuilding;
        InSpecialSlot = inSpecialSlot;
        TargetFacility = null;
        this.costRate = costRate;
    }

    public void SetReserve(BranchFacilityDef targetFacility, float costRate = 1f)
    {
        TargetBuilding = null;
        InSpecialSlot = false;
        TargetFacility = targetFacility;
        this.costRate = costRate;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref TargetBuilding, "TargetBuilding");
        Scribe_Values.Look(ref InSpecialSlot, "InSpecialSlot", defaultValue: false);
        Scribe_Defs.Look(ref TargetFacility, "TargetFacility");
        Scribe_Values.Look(ref costRate, "costRate", 1f);
    }
}

public class BranchStoresReserveHandler(Branch branch) : IExposable, ITickHourOfDay, IDrawDevWindow
{
    [Unsaved] public readonly Branch Branch = branch ?? throw new ArgumentNullException(nameof(branch));

    private static readonly float[] CostRateReduceArr = [0.02f, 0.005f, 0.0025f];
    private const int StoresReserveCeiling = 3;

    private List<BranchStoresReserve> storesReserves = new(StoresReserveCeiling);
    public bool ThoroughPreparation;

    public BranchStoresReserve PrimaryReserves => storesReserves.FirstOrFallback(null);
    public float PrimaryCostRate => PrimaryReserves?.costRate ?? 1f;

    public void ExposeData()
    {
        Scribe_Values.Look(ref ThoroughPreparation, "ThoroughPreparation", defaultValue: false);
        Scribe_Collections.Look(ref storesReserves, "storesReserves", LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"HasThoroughPreparation: {ThoroughPreparation}");
        listing_Rect.Label("StoresReserves:");
        if (storesReserves.Count > 0)
        {
            foreach (BranchStoresReserve reserve in storesReserves)
            {
                if (reserve.TargetBuilding is not null)
                {
                    listing_Rect.SubLabel($"{reserve.TargetBuilding.label} | {reserve.InSpecialSlot} | {reserve.costRate}", 0.8f);
                }
                else
                {
                    listing_Rect.SubLabel($"{reserve.TargetFacility.label} | {reserve.costRate}", 0.8f);
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
            storesReserves.Add(new BranchStoresReserve() { TargetBuilding = def, InSpecialSlot = inSpecialSlot });
            return;
        }

        if (storesReserves[0].TargetBuilding == def)
        {
            storesReserves[0].InSpecialSlot = inSpecialSlot;
        }
        else
        {
            bool exist = false;
            for (int i = 1; i < storesReserves.Count; i++)
            {
                if (storesReserves[i].TargetBuilding == def)
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
            storesReserves.Add(new BranchStoresReserve() { TargetFacility = def });
            return;
        }

        if (storesReserves[0].TargetFacility != def)
        {
            bool exist = false;
            for (int i = 1; i < storesReserves.Count; i++)
            {
                if (storesReserves[i].TargetFacility == def)
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
            if (storesReserves[i].TargetBuilding == def)
            {
                return;
            }
        }

        storesReserves.Add(new BranchStoresReserve() { TargetBuilding = def, InSpecialSlot = inSpecialSlot });
    }

    public void AddNewReserve(BranchFacilityDef def)
    {
        if (def is null || storesReserves.Count >= StoresReserveCeiling)
        {
            return;
        }

        for (int i = 0; i < storesReserves.Count; i++)
        {
            if (storesReserves[i].TargetFacility == def)
            {
                return;
            }
        }

        storesReserves.Add(new BranchStoresReserve() { TargetFacility = def });
    }

    /// <summary>
    /// 应该只在分部无任何建设时执行
    /// </summary>
    public void TickHour(int hourOfDay)
    {
        if (hourOfDay == 5)
        {
            float coseRateFloor = ThoroughPreparation ? 0.6f : 0.7f;
            float reduceMulti = ThoroughPreparation ? 2f : 1f;
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
            if (storesReserves[i].TargetBuilding == def)
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
            if (storesReserves[i].TargetFacility == def)
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
            if (storesReserves[i].TargetBuilding == def)
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
            if (storesReserves[i].TargetFacility == def)
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
                Branch = Branch,
                InSpecialSlot = false
            };

            for (int i = 0; i < allDefs.Count; i++)
            {
                buildingDef = allDefs[i];
                constructParam.BuildingDef = buildingDef;
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
}

