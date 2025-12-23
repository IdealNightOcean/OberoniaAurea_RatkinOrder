using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingHandler : IExposable, ITickHour, ITickDay
{
    [Unsaved] private readonly Branch branch;

    private SimpleValueCache<int> BuildingCeilingCache { get; }
    public int BuildingCeiling => BuildingCeilingCache.GetCachedResult();

    public int UnusedSlotsCount => BuildingCeiling - allBuildings.Count - underConstructionBuildings.Count;
    public bool HasUnusedSlots => UnusedSlotsCount > 0;

    private List<BranchBuilding> allBuildings = [];
    public IReadOnlyList<BranchBuilding> AllBuildings => allBuildings;
    public int AllBuildingsCount => allBuildings.Count;

    public BranchBuildingDef SpecialBuildingDef { get; private set; }
    public HashSet<BranchBuildingDef> AllBuildingDefsHash { get; } = [];

    [Unsaved] private List<ITickHour> tickHourHandlers;
    [Unsaved] private List<ITickDay> tickDayHandlers;
    [Unsaved] private List<BranchBuildingComp_Interaction> interactionComps;
    public List<BranchBuildingComp_Interaction> InteractionComps => interactionComps ??= [];

    private List<UnderConstructionRecord<BranchBuildingDef>> underConstructionBuildings = [];
    public HashSet<BranchBuildingDef> UnderConstructionBuildingDefs { get; private set; } = [];
    public IReadOnlyList<UnderConstructionRecord<BranchBuildingDef>> UnderConstructionBuildings => underConstructionBuildings;
    public bool IsBusy => underConstructionBuildings.Count > 0;

    public Action<BranchBuildingDef, bool> PostConstructionChanged { get; set; }

    internal BranchBuildingHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        BuildingCeilingCache = new SimpleValueCache<int>(
            cacheInterval: 2500,
            defaultValue: (int)BranchStatDefOf.OARO_BuildingCeiling.baseValue,
            checker: () => (int)BranchStatDefOf.OARO_BuildingCeiling.Worker.GetValue(this.branch, immediateUpdate: true));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref allBuildings, nameof(allBuildings), LookMode.Deep);
        Scribe_Collections.Look(ref underConstructionBuildings, nameof(underConstructionBuildings), LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label("特殊建筑:");
        if (SpecialBuildingDef is null)
        {
            listing_Rect.SubLabel("None".Translate(), 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(SpecialBuildingDef.label, 0.8f);
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label($"普通建筑: {allBuildings.Count}");
        foreach (BranchBuilding building in allBuildings)
        {
            listing_Rect.SubLabel(building.Def.label, 0.8f);
        }

        listing_Rect.Gap(6f);
        if (underConstructionBuildings.Count == 0)
        {
            listing_Rect.Label("在建建筑: 无");
        }
        else
        {
            // listing_Rect.Label($"在建设施: {underConstructionBuilding.TargetDef.label} | {underConstructionBuilding.DurationTicksLeft}");
        }
    }

    public void TickHour()
    {
        if (underConstructionBuildings.Count > 0)
        {
            int ticksGame = Find.TickManager.TicksGame;
            for (int i = underConstructionBuildings.Count - 1; i >= 0; i--)
            {
                if (underConstructionBuildings[i].CompletedTick <= ticksGame)
                {
                    BranchBuildingDef buildingDef = underConstructionBuildings[i].TargetDef;
                    try
                    {
                        AddBuilding(buildingDef);
                    }
                    catch (Exception ex)
                    {
                        ModUtility.LogExceptionError(ex,
                            errorDesc: "finish branch-building construction",
                            typeName: nameof(BranchBuildingHandler),
                            methodName: nameof(TickHour),
                            needStackTrace: true);
                    }
                    finally
                    {
                        underConstructionBuildings.RemoveAt(i);
                        UnderConstructionBuildingDefs.Remove(buildingDef);
                    }
                }
            }
        }

        if (tickHourHandlers is not null)
        {
            for (int i = 0; i < tickHourHandlers.Count; i++)
            {
                tickHourHandlers[i].TickHour();
            }
        }
    }

    public void TickDay()
    {
        foreach (BranchBuilding building in allBuildings)
        {
            if (CanUpgradeBuilding(building))
            {
                building.InitUpgraded();
                UpgradeBuilding(building);
            }
        }

        if (tickDayHandlers is not null)
        {
            for (int i = 0; i < tickDayHandlers.Count; i++)
            {
                tickDayHandlers[i].TickDay();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasBuilding(BranchBuildingDef buildingDef) => AllBuildingDefsHash.Contains(buildingDef);

    public BranchBuilding GetBuilding(BranchBuildingDef buildingDef)
    {
        for (int i = 0; i < allBuildings.Count; i++)
        {
            if (allBuildings[i].Def == buildingDef)
            {
                return allBuildings[i];
            }
        }
        return null;
    }

    public AcceptanceReport CanConstructBuilding(BranchBuildingConstructParms constructParam, bool resultOnly = false)
    {
        BranchBuildingDef buildingDef = constructParam.BuildingDef;
        if (buildingDef.isSpecial && SpecialBuildingDef is not null)
        {
            return resultOnly ? false : "OARO_AlreadyHasSpecialBuilding".Translate();
        }

        if (!HasUnusedSlots)
        {
            return resultOnly ? false : "OARO_AlreadyReachedBuildingCeiling".Translate();
        }

        if (HasBuilding(buildingDef))
        {
            return resultOnly ? false : "OARO_HasSameBuilding".Translate();
        }
        if (UnderConstructionBuildingDefs.Contains(buildingDef))
        {
            return resultOnly ? false : "OARO_BuildingOnConstruction".Translate();
        }

        if (constructParam.ByPlayer)
        {
            if (constructParam.Map is null)
            {
                return resultOnly ? false : "OARO_NoAvailablePlayerHomeMap".Translate();
            }
            int silverCost = branch.GetBuildingSilverCost(buildingDef, resultOnly: false, out _);
            if (!constructParam.Map.HasEnoughThingsOfDef(ThingDefOf.Silver, silverCost))
            {
                return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, silverCost.ToString());
            }
        }

        return buildingDef.ConstructChecker.CanConstruct(constructParam, resultOnly);
    }

    public void StartBuildingConstruction(BranchBuildingConstructParms constructParam)
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

    public void CancelBuildingConstruction(BranchBuildingDef buildingDef)
    {
        if (UnderConstructionBuildingDefs.Count == 0 || !UnderConstructionBuildingDefs.Contains(buildingDef))
        {
            return;
        }

        try
        {
            for (int i = 0; i < underConstructionBuildings.Count; i++)
            {
                if (underConstructionBuildings[i].TargetDef == buildingDef)
                {
                    underConstructionBuildings.RemoveAt(i);
                    break;
                }
            }
            UnderConstructionBuildingDefs.Remove(buildingDef);
            if (buildingDef == SpecialBuildingDef)
            {
                SpecialBuildingDef = null;
            }

            PostConstructionChanged?.Invoke(buildingDef, false);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, nameof(PostConstructionChanged), nameof(BranchBuildingHandler), nameof(CancelBuildingConstruction), needStackTrace: true);
        }
    }

    public void StartBuildingConstructionDirectly(BranchBuildingConstructParms constructParam)
    {
        BranchBuildingDef buildingDef = constructParam.BuildingDef;
        if (buildingDef.isSpecial && SpecialBuildingDef is not null)
        {
            return;
        }
        if (AllBuildingDefsHash.Contains(buildingDef) || UnderConstructionBuildingDefs.Contains(buildingDef))
        {
            return;
        }

        UnderConstructionRecord<BranchBuildingDef> underConstructionBuilding = new(
            targetDef: buildingDef,
            durationTicks: branch.GetBuildingTimeCost(buildingDef));

        underConstructionBuildings.Add(underConstructionBuilding);
        UnderConstructionBuildingDefs.Add(buildingDef);

        if (constructParam.ByPlayer && constructParam.Map is not null)
        {
            int silverCost = branch.GetBuildingSilverCost(buildingDef, resultOnly: false, out _);
            constructParam.Map.DestoryThingsOfDef(ThingDefOf.Silver, silverCost);
        }
        branch.StoresReserveHandler.Notify_BranchConstructStarted(buildingDef);
        try
        {
            PostConstructionChanged?.Invoke(buildingDef, true);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex, nameof(PostConstructionChanged), nameof(BranchBuildingHandler), nameof(StartBuildingConstructionDirectly), needStackTrace: true);
        }
    }

    public void AddBuilding(BranchBuildingDef buildingDef)
    {
        if (buildingDef.isSpecial && SpecialBuildingDef is not null)
        {
            Log.Error($"[OARO] Attempted to add a new branch building to the special building slot of {branch}, but one already exists.");
            return;
        }
        else if (HasBuilding(buildingDef))
        {
            Log.Error($"[OARO] Attempted to add a new branch building to {branch}, but one already exists.");
            return;
        }

        BranchBuilding newBuilding;
        try
        {
            newBuilding = BranchBuilding.GenerateBranchBuilding(buildingDef, branch);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"generating building {buildingDef.defName} for {branch}",
                typeName: nameof(BranchBuildingHandler),
                methodName: nameof(AddBuilding),
                needStackTrace: true);
            return;
        }
        allBuildings.Add(newBuilding);
        AllBuildingDefsHash.Add(buildingDef);

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
        BranchBuilding building = GetBuilding(buildingDef);
        if (building is null)
        {
            return;
        }

        allBuildings.Remove(building);
        if (buildingDef == SpecialBuildingDef)
        {
            SpecialBuildingDef = null;
        }
        AllBuildingDefsHash.Remove(buildingDef);

        if (building is ITickHour ticksLong)
        {
            tickHourHandlers?.Remove(ticksLong);
        }
        if (building is ITickDay newTickDay)
        {
            tickDayHandlers?.Remove(newTickDay);
        }
        if (building is IPostCombatantGenerate postPawnGenerate)
        {
            branch.IPostCombatantGenerate.Add(postPawnGenerate);
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

        for (int i = 0; i < allBuildings.Count; i++)
        {
            if (allBuildings[i].TryGetStatTransformer(statDef, out tempTransformer))
            {
                hasTransformer = true;
                transformer.MergeWith(tempTransformer);
            }
        }

        return hasTransformer;
    }

    internal void PostLoadInit()
    {
        if (underConstructionBuildings.RemoveAll(r => r is null) > 0)
        {
            Log.Error($"[OARO] {branch} has null under construction buildings after loading, Removed.");
        }

        foreach (UnderConstructionRecord<BranchBuildingDef> constructionBuilding in underConstructionBuildings)
        {
            if (constructionBuilding.TargetDef.isSpecial)
            {
                SpecialBuildingDef = constructionBuilding.TargetDef;
            }
            UnderConstructionBuildingDefs.Add(constructionBuilding.TargetDef);
        }

        if (allBuildings.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"[OARO] {branch} has null buildings after loading, Removed.");
        }

        foreach (BranchBuilding building in allBuildings)
        {
            AllBuildingDefsHash.Add(building.Def);
            ActiveBuilding(building);
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
            SpecialBuildingDef = building.Def;
            if (building.Def.IsHonorSymbol)
            {
                branch.SetHonorDef(building.Def.honorDef);
            }
        }

        if (building is ITickHour tickLong)
        {
            tickHourHandlers ??= [];
            tickHourHandlers.Add(tickLong);
        }
        if (building is ITickDay tickDay)
        {
            tickDayHandlers ??= [];
            tickDayHandlers.Add(tickDay);
        }
        if (building is IPostCombatantGenerate postPawnGenerate)
        {
            branch.IPostCombatantGenerate.Add(postPawnGenerate);
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