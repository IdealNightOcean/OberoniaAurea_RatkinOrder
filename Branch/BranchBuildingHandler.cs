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

    protected List<BranchBuilding> buildings = [];
    protected BranchBuilding specialBuilding;

    public BranchBuilding SpecialBuilding => specialBuilding;

    [Unsaved] private List<ITickHour<Branch>> TickLongHandlers;
    [Unsaved] private List<ITickDay<Branch>> TickDayHandlers;

    private BranchBuildingDef underConstructionBuilding;
    private bool inSpecialSlot;
    private int buildingTicksLeft = -1;

    public bool IsBusy => underConstructionBuilding is not null;

    public BranchBuildingHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        buildingCeilingCache = new SimpleValueCache<int>(cacheInterval: 60000, defaultValue: 1, () => (int)BranchStatDefOf.OARO_BuildingCeiling.Worker.GetValue(this.branch, immediateUpdate: true));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref buildings, "buildings", LookMode.Deep);
        Scribe_Deep.Look(ref specialBuilding, "specialBuilding");

        Scribe_Defs.Look(ref underConstructionBuilding, "underConstructionBuilding");
        Scribe_Values.Look(ref inSpecialSlot, "inSpecialSlot", defaultValue: false);
        Scribe_Values.Look(ref buildingTicksLeft, "buildingTicksLeft", -1);
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
            listing_Rect.SubLabel(underConstructionBuilding.label, 0.8f);
        }
        listing_Rect.Label($"BuildingTicksLeft: {buildingTicksLeft}");
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
                TickLongHandlers[i].TickHour(branch);
            }
        }
    }
    public void TickDay()
    {
        if (TickDayHandlers is not null)
        {
            for (int i = 0; i < TickDayHandlers.Count; i++)
            {
                TickDayHandlers[i].TickDay(branch);
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

    public int GetBuildingSilverCost(BranchBuildingDef buildingDef)
    {
        float result = branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCost, baseValueOverride: buildingDef.silverCost);
        result *= (1f - branch.StoresReserveHandler.GetReserveCostReduce(buildingDef));

        return (int)result;
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
            int silverCost = GetBuildingSilverCost(buildingDef);
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
        underConstructionBuilding = constructParam.BuildingDef;

        buildingTicksLeft = (int)(underConstructionBuilding.constructionDays * 60000);
        if (constructParam.ByPlayer)
        {
            int silverCost = GetBuildingSilverCost(constructParam.BuildingDef);
            OAFrame_CaravanUtility.RemoveThingsOfDef(constructParam.caravan, ThingDefOf.Silver, silverCost);
        }
        branch.StoresReserveHandler.Notify_BranchConstructStarted(underConstructionBuilding);
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
            TickLongHandlers?.Remove(ticksLong);
        }
        if (building is ITickDay<Branch> newTickDay)
        {
            TickDayHandlers?.Remove(newTickDay);
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

        HashSet<BranchStatDef> statNeedRecache = [];
        List<BranchStatModifier> branchStatModifies;
        //移除一般修正
        if (buildingDef.branchStatModifies is not null)
        {
            branchStatModifies = buildingDef.branchStatModifies;
            for (int i = 0; i < branchStatModifies.Count; i++)
            {
                if (branchStatModifies[i].Transformer.factor == 0f)
                {
                    statNeedRecache.Add(branchStatModifies[i].statDef);
                }
                else
                {
                    branch.TransformerHandler.RemoveStatModifier(branchStatModifies[i]);
                }
            }
        }
        //移除升级修正
        if (building.HasUpgraded && buildingDef.advancedProperties.branchStatModifies is not null)
        {
            branchStatModifies = buildingDef.advancedProperties.branchStatModifies;
            for (int i = 0; i < branchStatModifies.Count; i++)
            {
                if (branchStatModifies[i].Transformer.factor == 0f)
                {
                    statNeedRecache.Add(branchStatModifies[i].statDef);
                }
                else
                {
                    branch.TransformerHandler.RemoveStatModifier(branchStatModifies[i]);
                }
            }
        }
        //重新获得 factor == 0f 的修正
        if (statNeedRecache.Count > 0)
        {
            foreach (BranchStatDef statDef in statNeedRecache)
            {
                branch.RecacheBranchStat(statDef);
            }
        }

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

        foreach (BranchBuilding building in buildings)
        {
            ActiveBuilding(building, isSpecial: false);
        }
    }

    private void ActiveBuilding(BranchBuilding building, bool isSpecial)
    {
        branch.EffectTags.IncrementTagsValue(building.Def.effectFlags, addIfMiss: true);
        branch.TransformerHandler.AddStatModifiers(building.Def.branchStatModifies);
        if (isSpecial && building.Def.IsHonorSymbol)
        {
            branch.SetBranchType(Branch.BranchType.Honor, active: true);
            branch.HonorProperties = building.Def.honorProperties;
        }

        if (CanUpgradeBuilding(building))
        {
            UpgradeBuilding(building);
        }

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
        branch.TransformerHandler.AddStatModifiers(building.Def.advancedProperties.branchStatModifies);

        building.PostUpgraded();
    }

    internal void PostBranchGenerated() { }
}
