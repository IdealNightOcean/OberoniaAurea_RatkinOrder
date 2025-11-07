using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingHandler : IExposable, ITickHourOfDay, ITickDay
{
    [Unsaved] private readonly Branch branch;

    [Unsaved] private SimpleValueCache<int> buildingCeilingCache;
    public int BuildingCeiling => buildingCeilingCache.GetCachedResult();
    public bool HasUnusedNormalSlots => buildings.Count < BuildingCeiling;
    public bool IsNormalBuildingFullyCompleted => buildings.Count >= BranchStatDefOf.OARO_BuildingCeiling.maxValue;

    private List<BranchBuilding> buildings = [];
    private BranchBuilding specialBuilding;

    public IReadOnlyList<BranchBuilding> Buildings => buildings;
    public BranchBuilding SpecialBuilding => specialBuilding;

    [Unsaved] private List<ITickHour<Branch>> tickHourHandlers;
    [Unsaved] private List<ITickDay<Branch>> tickDayHandlers;
    [Unsaved] private List<BranchBuildingComp_Interaction> interactionComps;
    public List<BranchBuildingComp_Interaction> InteractionComps => interactionComps ??= [];

    private BranchBuildingConstructionRecord underConstructionBuilding;
    public BranchBuildingConstructionRecord UnderConstructionBuilding => underConstructionBuilding;
    public bool IsBusy => underConstructionBuilding is not null;

    internal BranchBuildingHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        buildingCeilingCache = new SimpleValueCache<int>(cacheInterval: 60000, defaultValue: (int)BranchStatDefOf.OARO_BuildingCeiling.baseValue, () => (int)BranchStatDefOf.OARO_BuildingCeiling.Worker.GetValue(this.branch, immediateUpdate: true));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref buildings, "buildings", LookMode.Deep);
        Scribe_Deep.Look(ref specialBuilding, "specialBuilding");

        Scribe_Deep.Look(ref underConstructionBuilding, "underConstructionBuilding");
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label("SpecialBuilding:");
        if (specialBuilding is null)
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(specialBuilding.Def.label, 0.8f);
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label($"NormalBuildings: {buildings.Count}");
        foreach (BranchBuilding building in buildings)
        {
            listing_Rect.SubLabel(building.Def.label, 0.8f);
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("UnderConstructionBuilding:");
        if (underConstructionBuilding is null)
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(underConstructionBuilding.BuildingDef.label, 0.8f);
            listing_Rect.Label($"BuildingTicksLeft: {underConstructionBuilding.DurationTicksLeft}");
        }
    }

    public void TickHour(int hourOfDay)
    {
        if (underConstructionBuilding is not null && (underConstructionBuilding.DurationTicksLeft -= 2500) <= 0)
        {
            try
            {
                AddBuilding(underConstructionBuilding.BuildingDef, underConstructionBuilding.InSpecialSlot);
            }
            finally
            {
                underConstructionBuilding = null;
            }
        }

        if (tickHourHandlers is not null)
        {
            for (int i = 0; i < tickHourHandlers.Count; i++)
            {
                tickHourHandlers[i].TickHour(branch);
            }
        }
    }

    public void TickDay()
    {
        if (CanUpgradeBuilding(specialBuilding))
        {
            specialBuilding.InitUpgraded();
            UpgradeBuilding(specialBuilding);
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            if (CanUpgradeBuilding(buildings[i]))
            {
                buildings[i].InitUpgraded();
                UpgradeBuilding(buildings[i]);
            }
        }

        if (tickDayHandlers is not null)
        {
            for (int i = 0; i < tickDayHandlers.Count; i++)
            {
                tickDayHandlers[i].TickDay(branch);
            }
        }
    }

    public bool HasBuilding(BranchBuildingDef buildingDef)
    {
        if (specialBuilding?.Def == buildingDef)
        {
            return true;
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].Def == buildingDef)
            {
                return true;
            }
        }

        return false;
    }

    public (BranchBuilding building, bool inSpecialSlot) GetBuilding(BranchBuildingDef buildingDef)
    {
        if (specialBuilding?.Def == buildingDef)
        {
            return (specialBuilding, true);
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].Def == buildingDef)
            {
                return (buildings[i], false);
            }
        }
        return (null, false);
    }

    public AcceptanceReport CanConstructBuilding(BranchBuildingConstructParameter constructParam, bool resultOnly = false)
    {
        if (constructParam.InSpecialSlot && specialBuilding is not null)
        {
            return resultOnly ? false : "OARO_AlreadyHasSpecialBuilding".Translate();
        }

        BranchBuildingDef buildingDef = constructParam.BuildingDef;

        if (!HasUnusedNormalSlots)
        {
            return resultOnly ? false : "OARO_AlreadyReachedBuildingCeiling".Translate();
        }

        if (HasBuilding(buildingDef))
        {
            return resultOnly ? false : "OARO_HasSameBuilding".Translate();
        }

        if (constructParam.ByPlayer)
        {
            int silverCost = branch.GetBuildingSilverCost(buildingDef);
            if (!CaravanInventoryUtility.HasThings(constructParam.caravan, ThingDefOf.Silver, silverCost))
            {
                return resultOnly ? false : "OARO_NotEnoughSilver".Translate(silverCost);
            }
        }

        return buildingDef.ConstructChecker.CanConstruct(constructParam, resultOnly);
    }

    public void StartBuildingConstruction(BranchBuildingConstructParameter constructParam)
    {
        if (constructParam.NeedDoubleConfirm)
        {
            constructParam.DoubleComfirm();
        }
        else
        {
            StartBuildingConstructionDirectly(constructParam);
        }
    }

    public void StartBuildingConstructionDirectly(BranchBuildingConstructParameter constructParam)
    {
        BranchBuildingDef buildingDef = constructParam.BuildingDef;
        underConstructionBuilding = new(
            def: buildingDef,
            inSpecialSlot: constructParam.InSpecialSlot,
            durationTicks: branch.GetBuildingTimeCost(underConstructionBuilding.BuildingDef));

        if (constructParam.ByPlayer)
        {
            int silverCost = branch.GetBuildingSilverCost(constructParam.BuildingDef);
            OAFrame_CaravanUtility.RemoveThingsOfDef(constructParam.caravan, ThingDefOf.Silver, silverCost);
        }
        branch.StoresReserveHandler.Notify_BranchConstructStarted(buildingDef);
    }

    private void AddBuilding(BranchBuildingDef buildingDef, bool inSpecialSlot)
    {
        inSpecialSlot = inSpecialSlot || buildingDef.isSpecial;
        if (inSpecialSlot && specialBuilding is not null)
        {
            Log.Error($"Attempted to add a new branch building to the special building slot of {branch}, but one already exists.");
            return;
        }
        BranchBuilding newBuilding;
        try
        {
            newBuilding = BranchBuilding.GenerateBranchBuilding(buildingDef, branch);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to generate building {buildingDef.defName} for {branch}: {e.Message}");
            return;
        }
        if (inSpecialSlot)
        {
            specialBuilding = newBuilding;
        }
        else
        {
            buildings.Add(newBuilding);
        }

        newBuilding.InitActive();
        ActiveBuilding(newBuilding, isSpecial: inSpecialSlot);

        if (CanUpgradeBuilding(newBuilding))
        {
            newBuilding.InitUpgraded();
            UpgradeBuilding(newBuilding);
        }
    }

    public void RemoveBuilding(BranchBuildingDef buildingDef)
    {
        (BranchBuilding building, bool inSpecialSlot) = GetBuilding(buildingDef);
        if (building is null)
        {
            return;
        }

        if (inSpecialSlot)
        {
            specialBuilding = null;
        }
        else
        {
            buildings.Remove(building);
        }

        if (building is ITickHour<Branch> ticksLong)
        {
            tickHourHandlers?.Remove(ticksLong);
        }
        if (building is ITickDay<Branch> newTickDay)
        {
            tickDayHandlers?.Remove(newTickDay);
        }
        if (building is IPostSquadCombatPawnGenerate postPawnGenerate)
        {
            branch.PostSquadCombatPawnGenerate.Add(postPawnGenerate);
        }

        branch.EffectTags.DecrementTagsValue(buildingDef.effectFlags);
        if (building.HasUpgraded)
        {
            branch.EffectTags.DecrementTagsValue(buildingDef.advancedProperties?.effectFlags);
        }

        //移除一般修正
        if (buildingDef.branchStatOffsets is not null)
        {
            branch.TransformerHandler.UnmergeStatsOffset(buildingDef.branchStatOffsets);
        }
        if (buildingDef.branchStatFactors is not null)
        {
            branch.TransformerHandler.UnmergeStatsFactor(buildingDef.branchStatFactors, doZeroUnmergedProcess: false);
        }
        //移除升级修正
        if (building.HasUpgraded && buildingDef.advancedProperties is not null)
        {
            if (buildingDef.advancedProperties.branchStatOffsets is not null)
            {
                branch.TransformerHandler.UnmergeStatsOffset(buildingDef.advancedProperties.branchStatOffsets);
            }
            if (buildingDef.advancedProperties.branchStatFactors is not null)
            {
                branch.TransformerHandler.UnmergeStatsFactor(buildingDef.advancedProperties.branchStatFactors, doZeroUnmergedProcess: false);
            }
        }
        branch.TransformerHandler.DoZeroFactorUnmergedProcess();
        building.PostDeactive();
    }

    public bool GetBranchStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        transformer = BranchStatTransformer.DefaultTransformer;
        BranchStatTransformer tempTransformer;
        bool hasTransformer = false;

        if (specialBuilding is not null && specialBuilding.TryGetStatTransformer(statDef, out tempTransformer))
        {
            hasTransformer = true;
            transformer.MergeWith(tempTransformer);
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].TryGetStatTransformer(statDef, out tempTransformer))
            {
                hasTransformer = true;
                transformer.MergeWith(tempTransformer);
            }
        }

        return hasTransformer;
    }

    internal void PostLoadInit()
    {
        if (specialBuilding is not null)
        {
            ActiveBuilding(specialBuilding, isSpecial: true);
        }

        if (buildings is null)
        {
            buildings = [];
            return;
        }

        if (buildings.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"{branch} has null buildings after loading, Removed.");
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            ActiveBuilding(buildings[i], isSpecial: false);
        }
    }

    private void ActiveBuilding(BranchBuilding building, bool isSpecial)
    {
        branch.EffectTags.IncrementTagsValue(building.Def.effectFlags, addIfMiss: true);
        branch.TransformerHandler.MergeStatOffsets(building.Def.branchStatOffsets, addIfMiss: true);
        branch.TransformerHandler.MergeStatFactors(building.Def.branchStatFactors, addIfMiss: true);
        if (building.HasUpgraded)
        {
            UpgradeBuilding(building);
        }

        if (isSpecial && building.Def.IsHonorSymbol)
        {
            branch.SetBranchType(Branch.BranchType.Honor, active: true);
            branch.HonorDef = building.Def.honorDef;
        }

        if (building is ITickHour<Branch> tickLong)
        {
            tickHourHandlers ??= [];
            tickHourHandlers.Add(tickLong);
        }
        if (building is ITickDay<Branch> tickDay)
        {
            tickDayHandlers ??= [];
            tickDayHandlers.Add(tickDay);
        }
        if (building is IPostSquadCombatPawnGenerate postPawnGenerate)
        {
            branch.PostSquadCombatPawnGenerate.Add(postPawnGenerate);
        }

        building.PostActive();
    }

    private bool CanUpgradeBuilding(BranchBuilding building)
    {
        if (building is null || !building.Def.IsUpgradable || building.HasUpgraded)
        {
            return false;
        }
        return branch.PopulationHandler.Population >= building.Def.advancedProperties.advancedPopulation;
    }

    private void UpgradeBuilding(BranchBuilding building)
    {
        building.HasUpgraded = true;
        branch.EffectTags.IncrementTagsValue(building.Def.advancedProperties.effectFlags, addIfMiss: true);
        branch.TransformerHandler.MergeStatOffsets(building.Def.advancedProperties.branchStatOffsets, addIfMiss: true);
        branch.TransformerHandler.MergeStatFactors(building.Def.advancedProperties.branchStatFactors, addIfMiss: true);
        building.PostUpgraded();
    }

    internal void PostBranchGenerated() { }
}
