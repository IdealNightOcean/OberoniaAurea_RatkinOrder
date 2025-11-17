using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingHandler : IExposable, ITickHourOfDay, ITickDay
{
    [Unsaved] private readonly Branch branch;

    [Unsaved] private SimpleValueCache<int> buildingCeilingCache;
    public int BuildingCeiling => buildingCeilingCache.GetCachedResult();
    public bool HasUnusedNormalSlots => noramlBuildings.Count < BuildingCeiling;
    public bool IsNormalBuildingFullyCompleted => noramlBuildings.Count >= BranchStatDefOf.OARO_BuildingCeiling.maxValue;

    private List<BranchBuilding> noramlBuildings = [];
    private BranchBuilding specialBuilding;

    public IReadOnlyList<BranchBuilding> NormalBuildings => noramlBuildings;
    public BranchBuilding SpecialBuilding => specialBuilding;
    public IEnumerable<BranchBuilding> AllBuldings
    {
        get
        {
            if (specialBuilding is not null)
            {
                yield return specialBuilding;
            }
            foreach (BranchBuilding building in noramlBuildings)
            {
                yield return building;
            }
        }
    }


    [Unsaved] public readonly LazyMutableCollection<HashSet<BranchBuildingDef>, BranchBuildingDef> AllBuildingDefsHash;

    [Unsaved] private List<ITickHour<Branch>> tickHourHandlers;
    [Unsaved] private List<ITickDay<Branch>> tickDayHandlers;
    [Unsaved] private List<BranchBuildingComp_Interaction> interactionComps;
    public List<BranchBuildingComp_Interaction> InteractionComps => interactionComps ??= [];

    private UnderConstructionBranchBuilding underConstructionBuilding;
    public UnderConstructionBranchBuilding UnderConstructionBuilding => underConstructionBuilding;
    [Unsaved] public Action<BranchBuildingDef, bool> OnBuildingConstructionChanged;
    public bool IsBusy => underConstructionBuilding is not null;

    internal BranchBuildingHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        buildingCeilingCache = new SimpleValueCache<int>(cacheInterval: 60000, defaultValue: (int)BranchStatDefOf.OARO_BuildingCeiling.baseValue, () => (int)BranchStatDefOf.OARO_BuildingCeiling.Worker.GetValue(this.branch, immediateUpdate: true));
        AllBuildingDefsHash = new LazyMutableCollection<HashSet<BranchBuildingDef>, BranchBuildingDef>(refreshFunc: () => AllBuldings.Select(b => b.Def));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref noramlBuildings, "noramlBuildings", LookMode.Deep);
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
        listing_Rect.Label($"NormalBuildings: {noramlBuildings.Count}");
        foreach (BranchBuilding building in noramlBuildings)
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
        if (underConstructionBuilding is not null && Find.TickManager.TicksGame >= underConstructionBuilding.CompletedTick)
        {
            try
            {
                AddBuilding(underConstructionBuilding.BuildingDef);
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

        for (int i = 0; i < noramlBuildings.Count; i++)
        {
            if (CanUpgradeBuilding(noramlBuildings[i]))
            {
                noramlBuildings[i].InitUpgraded();
                UpgradeBuilding(noramlBuildings[i]);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasBuilding(BranchBuildingDef buildingDef) => AllBuildingDefsHash.Value.Contains(buildingDef);

    public BranchBuilding GetBuilding(BranchBuildingDef buildingDef, bool strictMatch = false)
    {
        if (buildingDef.isSpecial)
        {
            if (specialBuilding?.Def == buildingDef)
            {
                return specialBuilding;
            }
            else if (!strictMatch)
            {
                return null;
            }
        }

        for (int i = 0; i < noramlBuildings.Count; i++)
        {
            if (noramlBuildings[i].Def == buildingDef)
            {
                return noramlBuildings[i];
            }
        }
        return null;
    }

    public AcceptanceReport CanConstructBuilding(BranchBuildingConstructParameter constructParam, bool resultOnly = false)
    {
        if (constructParam.BuildingDef.isSpecial && specialBuilding is not null)
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
            if (constructParam.Caravan is null)
            {
                return false;
            }
            int silverCost = branch.GetBuildingSilverCost(buildingDef);
            if (!CaravanInventoryUtility.HasThings(constructParam.Caravan, ThingDefOf.Silver, silverCost))
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

    public void CancelBuildingConstruction()
    {
        if (underConstructionBuilding is null)
        {
            return;
        }

        try
        {
            OnBuildingConstructionChanged?.Invoke(underConstructionBuilding.BuildingDef, false);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, nameof(OnBuildingConstructionChanged), nameof(BranchBuildingHandler), nameof(CancelBuildingConstruction), needStackTrace: true);
        }
        finally
        {
            underConstructionBuilding = null;
        }
    }

    public void StartBuildingConstructionDirectly(BranchBuildingConstructParameter constructParam)
    {
        BranchBuildingDef buildingDef = constructParam.BuildingDef;
        underConstructionBuilding = new(
            def: buildingDef,
            durationTicks: branch.GetBuildingTimeCost(buildingDef));
        if (constructParam.ByPlayer)
        {
            int silverCost = branch.GetBuildingSilverCost(buildingDef);
            OAFrame_CaravanUtility.RemoveThingsOfDef(constructParam.Caravan, ThingDefOf.Silver, silverCost);
        }
        branch.StoresReserveHandler.Notify_BranchConstructStarted(buildingDef);
        try
        {
            OnBuildingConstructionChanged?.Invoke(buildingDef, true);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, nameof(OnBuildingConstructionChanged), nameof(BranchBuildingHandler), nameof(StartBuildingConstructionDirectly), needStackTrace: true);
        }
    }

    private void AddBuilding(BranchBuildingDef buildingDef)
    {
        if (buildingDef.isSpecial && specialBuilding is not null)
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
        if (buildingDef.isSpecial)
        {
            specialBuilding = newBuilding;
        }
        else
        {
            noramlBuildings.Add(newBuilding);
        }
        AllBuildingDefsHash.MarkDirty();

        newBuilding.InitActive();
        ActiveBuilding(newBuilding);

        if (CanUpgradeBuilding(newBuilding))
        {
            newBuilding.InitUpgraded();
            UpgradeBuilding(newBuilding);
        }
    }

    public void RemoveBuilding(BranchBuildingDef buildingDef)
    {
        BranchBuilding building = GetBuilding(buildingDef, strictMatch: true);
        if (building is null)
        {
            return;
        }

        if (buildingDef.isSpecial)
        {
            specialBuilding = null;
        }
        else
        {
            noramlBuildings.Remove(building);
        }
        AllBuildingDefsHash.MarkDirty();

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
        transformer = new();
        BranchStatTransformer tempTransformer;
        bool hasTransformer = false;

        if (specialBuilding is not null && specialBuilding.TryGetStatTransformer(statDef, out tempTransformer))
        {
            hasTransformer = true;
            transformer.MergeWith(tempTransformer);
        }

        for (int i = 0; i < noramlBuildings.Count; i++)
        {
            if (noramlBuildings[i].TryGetStatTransformer(statDef, out tempTransformer))
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
            ActiveBuilding(specialBuilding);
        }

        if (noramlBuildings is null)
        {
            noramlBuildings = [];
            return;
        }

        if (noramlBuildings.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"{branch} has null buildings after loading, Removed.");
        }

        for (int i = 0; i < noramlBuildings.Count; i++)
        {
            ActiveBuilding(noramlBuildings[i]);
        }
    }

    private void ActiveBuilding(BranchBuilding building)
    {
        branch.EffectTags.IncrementTagsValue(building.Def.effectFlags, addIfMiss: true);
        branch.TransformerHandler.MergeStatOffsets(building.Def.branchStatOffsets, addIfMiss: true);
        branch.TransformerHandler.MergeStatFactors(building.Def.branchStatFactors, addIfMiss: true);
        if (building.HasUpgraded)
        {
            UpgradeBuilding(building);
        }

        if (building.Def.isSpecial)
        {
            if (building.Def.IsHonorSymbol)
            {
                branch.SetBranchType(Branch.BranchType.Honor, active: true);
                branch.HonorDef = building.Def.honorDef;
            }
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