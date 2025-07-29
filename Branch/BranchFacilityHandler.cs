using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityHandler : IExposable, IPostLoadInit
{
    [Unsaved] public readonly Branch Branch;

    private Dictionary<BranchFacilityDef, BranchFacilityLevel> facilities = [];
    [Unsaved] private int totalFacilityLevel;

    public bool IsFacilityFullyCompleted { get; private set; }
    public Dictionary<BranchFacilityDef, BranchFacilityLevel> Facilities => facilities;
    public int TotalFacilityLevel => totalFacilityLevel;

    private BranchFacilityDef buildingFacility;
    private int buildingTicksLeft = -1;
    public bool IsBusy => buildingFacility is not null;

    public BranchFacilityHandler(Branch branch)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void TickHour()
    {
        if (buildingTicksLeft > 0 && (buildingTicksLeft -= 2500) <= 0)
        {
            CompleteFacilityConstruction();
        }
    }

    public int GetFacilitySilverCost(BranchFacilityDef facilityDef)
    {
        float result = BranchStatDefOf.OARO_BuildingCost.Worker.GetValue(Branch, facilityDef.silverCost);
        result *= Branch.StoresReserveHandler.GetFacilityCostReduce(facilityDef);

        return (int)result;
    }

    public AcceptanceReport CanConstructFacility(BranchFacilityDef facilityDef, bool byPlayer, Caravan caravan = null, bool resultOnly = false)
    {
        if (buildingTicksLeft > 0)
        {
            return resultOnly ? false : "OARO_FacilityAlreadyAssisting".Translate(facilityDef.LabelCap);
        }
        BranchFacilityLevel oldLevel = GetFacilityLevel(facilityDef);
        if (oldLevel == BranchFacilityLevel.Excellent)
        {
            return resultOnly ? false : "OARO_FacilityAlreadyAtMaxLevel".Translate();
        }
        BranchFacilityLevel newLevel = oldLevel + 1;

        if (byPlayer)
        {
            int silverCost = GetFacilitySilverCost(facilityDef);
            if (!CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, silverCost))
            {
                return resultOnly ? false : "OARO_NotEnoughSilver".Translate(silverCost);
            }
        }

        return true;
    }

    public void StartFacilityConstruction(BranchFacilityDef facilityDef, bool byPlayer, Caravan caravan = null)
    {
        buildingFacility = facilityDef;
        buildingTicksLeft = Find.TickManager.TicksGame;

        if (byPlayer)
        {
            int silverCost = GetFacilitySilverCost(facilityDef);
            OAFrame_CaravanUtility.RemoveThings(caravan, ThingDefOf.Silver, silverCost);
        }

        Branch.StoresReserveHandler.Notify_FacilityConstructionStarted(facilityDef);
    }

    private void CompleteFacilityConstruction()
    {
        if (buildingFacility is null)
        {
            return;
        }
        TryUpgradeFacility(buildingFacility);

        buildingTicksLeft = -1;
        buildingFacility = null;
    }

    public BranchFacilityLevel GetFacilityLevel(BranchFacilityDef facilityDef)
    {
        return facilities.TryGetValue(facilityDef, fallback: BranchFacilityLevel.None);
    }
    public void AddFacility(BranchFacilityDef facilityDef)
    {
        if (facilityDef == null || facilities.ContainsKey(facilityDef))
        {
            return;
        }
        StageUpgrade(facilityDef, oldLevelIndex: -1, newLevelIndex: 0, isPostInit: false);
    }

    public bool TryUpgradeFacility(BranchFacilityDef facilityDef)
    {
        if (facilityDef == null)
        {
            return false;
        }

        BranchFacilityLevel oldLevel = facilities.TryGetValue(facilityDef, fallback: BranchFacilityLevel.None);
        if (oldLevel == BranchFacilityLevel.None || oldLevel == BranchFacilityLevel.Excellent)
        {
            return false;
        }

        int oldLevelIndex = facilityDef.GetLevelStageIndex(oldLevel);
        if (oldLevelIndex < 0 || oldLevelIndex >= facilityDef.levelStages.Count - 1)
        {
            return false;
        }
        int newLevelIndex = oldLevelIndex + 1;

        if (StageUpgrade(facilityDef, oldLevelIndex, newLevelIndex, isPostInit: false))
        {
            facilities[facilityDef] = facilityDef.levelStages[newLevelIndex].level;
            IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool StageUpgrade(BranchFacilityDef facilityDef, int oldLevelIndex, int newLevelIndex, bool isPostInit = false)
    {
        if (oldLevelIndex < -1 || newLevelIndex <= oldLevelIndex)
        {
            return false;
        }

        for (int i = oldLevelIndex + 1; i <= newLevelIndex; i++)
        {
            BranchFacilityLevelStage stage = facilityDef.levelStages[i];
            Branch.EffectTags.IncrementTagsValue(stage.effectFlags, addIfMiss: true);
            Branch.TransformerHandler.AddStatModifiers(stage.statModifies);
            if (isPostInit)
            {
                stage.PostActive(Branch);
            }
            else
            {
                stage.PostLoadInit(Branch);
            }
        }
        totalFacilityLevel += (newLevelIndex - oldLevelIndex);
        return true;
    }

    public void PostLoadInit()
    {
        if (facilities.RemoveAll(kv => kv.Key is null || kv.Value == BranchFacilityLevel.None) > 0)
        {
            Log.Error($"{Branch} has null or None facilities after loading, Removed.");
        }

        foreach (var item in facilities)
        {
            StageUpgrade(item.Key, oldLevelIndex: -1, item.Key.GetLevelStageIndex(item.Value), isPostInit: true);
        }

        IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);
    }

    public void PostBranchGenerated()
    {

    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref facilities, "facilities", LookMode.Def, LookMode.Value);

        Scribe_Defs.Look(ref buildingFacility, "buildingFacility");
        Scribe_Values.Look(ref buildingTicksLeft, "buildingTicksLeft", -1);
    }
}