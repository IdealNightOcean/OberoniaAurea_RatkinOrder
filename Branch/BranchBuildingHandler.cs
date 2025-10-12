using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingHandler : IExposable, IPostLoadInit, ITickHourOfDay, ITickDay, IDrawDevWindow
{
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
                TickLongHandlers[i].TickHour(Branch);
            }
        }
    }
    public void TickDay()
    {
        if (TickDayHandlers is not null)
        {
            for (int i = 0; i < TickDayHandlers.Count; i++)
            {
                TickDayHandlers[i].TickDay(Branch);
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
        float result = Branch.GetStatValue(BranchStatDefOf.OARO_BuildingCost, baseValueOverride: buildingDef.silverCost);
        result *= Branch.StoresReserveHandler.GetBuildingCostReduce(buildingDef);

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
        Branch.StoresReserveHandler.Notify_BuildingConstructStarted(underConstructionBuilding);
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
            newBuilding = BranchBuilding.MakeBranchBuilding(buildingDef, Branch);
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

        Branch.EffectTags.DecrementTagsValue(buildingDef.effectFlags);
        if (buildingDef.branchStatModifies is not null)
        {
            foreach (BranchStatModifier statModifier in buildingDef.branchStatModifies)
            {
                if (statModifier.Transformer.factor == 0f)
                {
                    Branch.RecacheBranchStat(statModifier.statDef);
                }
                else
                {
                    Branch.TransformerHandler.RemoveStatModifier(statModifier);
                }
            }
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
            Branch.PostSquadCombatPawnGenerate.Add(postPawnGenerate);
        }

        building.PostDeactive();
    }

    public BranchStatTransformer GetBranchStatTransformer(BranchStatDef statDef)
    {
        BranchStatTransformer transformer = BranchStatTransformer.DefaultTransformer;
        if (specialBuilding is not null && specialBuilding.Def.branchStatModifies is not null)
        {
            foreach (BranchStatModifier statModifier in specialBuilding.Def.branchStatModifies)
            {
                if (statModifier.statDef == statDef)
                {
                    transformer.MergeWith(statModifier.Transformer);
                    break;
                }
            }
        }

        foreach (BranchBuilding building in buildings)
        {
            if (building.Def.branchStatModifies is not null)
            {
                foreach (BranchStatModifier statModifier in specialBuilding.Def.branchStatModifies)
                {
                    if (statModifier.statDef == statDef)
                    {
                        transformer.MergeWith(statModifier.Transformer);
                        break;
                    }
                }
            }
        }

        return transformer;
    }

    public void PostLoadInit()
    {
        if (specialBuilding is not null)
        {
            ActiveBuilding(specialBuilding, isSpecial: true);
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
            ActiveBuilding(building, isSpecial: false);
        }
        IsNormalBuildingFullyCompleted = buildings.Count >= BranchStatDefOf.OARO_BuildingCeiling.maxValue;
    }

    private void ActiveBuilding(BranchBuilding building, bool isSpecial)
    {
        Branch.EffectTags.IncrementTagsValue(building.Def.effectFlags, addIfMiss: true);
        Branch.TransformerHandler.AddStatModifiers(building.Def.branchStatModifies);
        if (isSpecial && building.Def.isHonorSymbol)
        {
            Branch.SetBranchType(Branch.BranchType.Honor, active: true);
            Branch.HonorProperties = building.Def.honorProperties;
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
            Branch.PostSquadCombatPawnGenerate.Add(postPawnGenerate);
        }

        building.PostActive();
    }

    public void PostBranchGenerated()
    {

    }
}
