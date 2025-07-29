using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingHandler : IExposable, IPostLoadInit, ITickHourOfDay, ITickDay
{
    public static readonly BranchBuildingDef[] Memorials =
    [
        BranchBuildingDefOf.OARO_GuardMemorial,
        BranchBuildingDefOf.OARO_PioneerMemorial,
        BranchBuildingDefOf.OARO_InterveneMemorial,
        BranchBuildingDefOf.OARO_LoyalMemorial,
        BranchBuildingDefOf.OARO_GloryMemorial
    ];

    [Unsaved] public readonly Branch Branch;

    [Unsaved] private SimpleValueCache<int> buildingCeilingCache;
    public int BuildingCeiling => buildingCeilingCache.GetCachedResult();
    public bool HasUnusedNormalSlots => buildings.Count < BuildingCeiling;
    public bool IsNormalBuildingFullyCompleted { get; private set; }
    public bool IsBuildingFullyCompleted => specialBuilding is not null && IsNormalBuildingFullyCompleted;

    protected List<BranchBuilding> buildings = [];
    protected BranchBuilding specialBuilding;

    [Unsaved] private List<ITickHour<Branch>> TickLongHandlers;
    [Unsaved] private List<ITickDay<Branch>> TickDayHandlers;

    private BranchBuildingDef underConstructionBuilding;
    private bool inSpecialSlot;
    private int buildingTicksLeft = -1;

    public bool IsBusy => underConstructionBuilding is not null;

    public BranchBuildingHandler(Branch branch)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        buildingCeilingCache = new SimpleValueCache<int>(cacheInterval: 60000, defaultValue: 1, () => (int)BranchStatDefOf.OARO_BuildingCeiling.Worker.GetValue(Branch, immediateUpdate: true));
    }

    public void TickHour(int hourOfDay)
    {
        if (buildingTicksLeft > 0 && (buildingTicksLeft -= 2500) <= 0)
        {
            try
            {
                AddBuilding(underConstructionBuilding, inSpecialSlot);
            }
            finally
            {
                underConstructionBuilding = null;
                inSpecialSlot = false;
                buildingTicksLeft = -1;
            }
        }

        if (TickLongHandlers is not null)
        {
            for (int i = 0; i < TickLongHandlers.Count; i++)
            {
                TickLongHandlers[i].TickHour(Branch);
            }
        }
    }
    public void TickDay()
    {
        if (TickDayHandlers is null)
        {
            return;
        }

        for (int i = 0; i < TickDayHandlers.Count; i++)
        {
            TickDayHandlers[i].TickDay(Branch);
        }
    }

    public bool HasBuilding(BranchBuildingDef buildingDef)
    {
        if (specialBuilding?.def == buildingDef)
        {
            return true;
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].def == buildingDef)
            {
                return true;
            }
        }

        return false;
    }

    public (BranchBuilding building, bool inSpecialSlot) GetBuilding(BranchBuildingDef buildingDef)
    {
        if (specialBuilding?.def == buildingDef)
        {
            return (specialBuilding, true);
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].def == buildingDef)
            {
                return (buildings[i], false);
            }
        }
        return (null, false);
    }

    public int GetBuildingSilverCost(BranchBuildingDef buildingDef)
    {
        float result = BranchStatDefOf.OARO_BuildingCost.Worker.GetValue(Branch, buildingDef.silverCost);
        result *= Branch.StoresReserveHandler.GetBuildingCostReduce(buildingDef);

        return (int)result;
    }

    public AcceptanceReport CanConstructBuilding(BranchBuildingDef buildingDef, bool inSpecialSlot, bool byPlayer, Caravan caravan = null, bool resultOnly = false)
    {
        if ((inSpecialSlot || buildingDef.isSpecial) && specialBuilding is not null)
        {
            return resultOnly ? false : "OARO_AlreadyHasSpecialBuilding".Translate();
        }
        if (buildingDef.isSpecial && !inSpecialSlot)
        {
            return resultOnly ? false : "OARO_SpecialBuildingOnlyInSpecialSlot".Translate();
        }
        else
        {
            if (buildings.Count >= BuildingCeiling)
            {
                return resultOnly ? false : "OARO_AlreadyReachedBuildingCeiling".Translate();
            }

            if (HasBuilding(buildingDef))
            {
                return resultOnly ? false : "OARO_HasSameBuilding".Translate();
            }
        }

        if (byPlayer)
        {
            int silverCost = GetBuildingSilverCost(buildingDef);
            if (!CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, silverCost))
            {
                return resultOnly ? false : "OARO_NotEnoughSilver".Translate(silverCost);
            }
        }

        return buildingDef.ConstructChecker.CanConstruct(Branch, buildingDef, inSpecialSlot, byPlayer, caravan, resultOnly);
    }

    public void StartBuildingConstruction(BranchBuildingDef buildingDef, bool inSpecialSlot, bool byPlayer, Caravan caravan = null)
    {
        if (byPlayer && buildingDef.ConstructChecker.DoubleComfirm)
        {
            buildingDef.ConstructChecker.DoubleComfirmAction(Branch, buildingDef, inSpecialSlot, caravan);
        }
        else
        {
            StartBuildingConstructionDirectly(buildingDef, inSpecialSlot, byPlayer, caravan);
        }
    }

    public void StartBuildingConstructionDirectly(BranchBuildingDef buildingDef, bool inSpecialSlot, bool byPlayer, Caravan caravan = null)
    {
        underConstructionBuilding = buildingDef;
        this.inSpecialSlot = inSpecialSlot || buildingDef.isSpecial;
        buildingTicksLeft = (int)(buildingDef.constructionDays * 60000);
        if (byPlayer)
        {
            int silverCost = GetBuildingSilverCost(buildingDef);
            OAFrame_CaravanUtility.RemoveThings(caravan, ThingDefOf.Silver, silverCost);
        }
        Branch.StoresReserveHandler.Notify_BuildingConstructStarted(buildingDef);
    }

    private void AddBuilding(BranchBuildingDef buildingDef, bool inSpecialSlot)
    {
        inSpecialSlot = inSpecialSlot || buildingDef.isSpecial;
        if (inSpecialSlot && specialBuilding is not null)
        {
            return;
        }
        BranchBuilding newBuilding;
        try
        {
            newBuilding = BranchBuildingMaker.MakeBranchBuilding(buildingDef);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to add building {buildingDef.defName} to {Branch}: {e.Message}");
            return;
        }
        if (inSpecialSlot)
        {
            specialBuilding = newBuilding;
        }
        else
        {
            buildings.Add(newBuilding);
            IsNormalBuildingFullyCompleted = buildings.Count >= BranchStatDefOf.OARO_BuildingCeiling.maxValue;
        }

        AddOrPostLoadBuilding(buildingDef);
        newBuilding.PostAddBuilding(Branch);

    }

    public void RemoveBuilding(BranchBuildingDef buildingDef)
    {
        (BranchBuilding building, bool inSpecialSlot) = GetBuilding(buildingDef);
        if (building is null)
        {
            return;
        }

        Branch.EffectTags.DecrementTagsValue(buildingDef.effectFlags);
        Branch.TransformerHandler.RemoveStatModifies(buildingDef.branchStatModifies);
        if (building is ITickHour<Branch> ticksLong)
        {
            TickLongHandlers?.Remove(ticksLong);
        }
        if (building is ITickDay<Branch> newTickDay)
        {
            TickDayHandlers?.Remove(newTickDay);
        }
        if (building is IPostSquadCombatPawnGenerate postPawnGenerate)
        {
            Branch.PostSquadCombatPawnGenerate.Add(postPawnGenerate);
        }

        if (inSpecialSlot)
        {
            specialBuilding = null;
        }
        else
        {
            buildings.Remove(building);
        }


        building.PostRemoveBuilding(Branch);
    }

    public void PostLoadInit()
    {

        if (specialBuilding is not null)
        {
            AddOrPostLoadBuilding(specialBuilding.def);
            specialBuilding.PostLoadInit(Branch);
        }

        if (buildings is null)
        {
            buildings = [];
            IsNormalBuildingFullyCompleted = false;
            return;
        }

        if (buildings.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"{Branch} has null buildings after loading, Removed.");
        }

        foreach (BranchBuilding building in buildings)
        {
            AddOrPostLoadBuilding(building.def);
            building.PostLoadInit(Branch);
        }
        IsNormalBuildingFullyCompleted = buildings.Count >= BranchStatDefOf.OARO_BuildingCeiling.maxValue;
    }

    private void AddOrPostLoadBuilding(BranchBuildingDef building)
    {
        Branch.EffectTags.IncrementTagsValue(building.effectFlags, addIfMiss: true);
        Branch.TransformerHandler.AddStatModifiers(building.branchStatModifies);

        if (building is ITickHour<Branch> tickLong)
        {
            TickLongHandlers ??= [];
            TickLongHandlers.Add(tickLong);
        }
        if (building is ITickDay<Branch> tickDay)
        {
            TickDayHandlers ??= [];
            TickDayHandlers.Add(tickDay);
        }
        if (building is IPostSquadCombatPawnGenerate postPawnGenerate)
        {
            Branch.PostSquadCombatPawnGenerate.Add(postPawnGenerate);
        }
    }

    public void PostBranchGenerated()
    {
        if (Rand.Chance(0.08f))
        {
            BranchBuildingDef specialBuildingDef = Memorials[Rand.Range(0, Memorials.Length)];
            AddBuilding(specialBuildingDef, inSpecialSlot: true);
            specialBuildingDef.GetModExtension<BranchBuilding_MemorialExtension>()?.CompleteRequirements(Branch);
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref buildings, "buildings", LookMode.Deep);
        Scribe_Deep.Look(ref specialBuilding, "specialBuilding");

        Scribe_Defs.Look(ref underConstructionBuilding, "underConstructionBuilding");
        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);
        Scribe_Values.Look(ref buildingTicksLeft, "buildingTicksLeft", -1);
    }
}
