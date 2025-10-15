using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityHandler(Branch branch) : IExposable
{
    [Unsaved] public readonly Branch Branch = branch ?? throw new ArgumentNullException(nameof(branch));

    private Dictionary<BranchFacilityDef, BranchFacilityLevel> facilities = [];
    [Unsaved] private int totalFacilityLevel;

    public bool IsFacilityFullyCompleted { get; private set; }
    public Dictionary<BranchFacilityDef, BranchFacilityLevel> Facilities => facilities;
    public int TotalFacilityLevel => totalFacilityLevel;

    private BranchFacilityDef buildingFacility;
    private int buildingTicksLeft = -1;
    public bool IsBusy => buildingFacility is not null;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref facilities, "facilities", LookMode.Def, LookMode.Value);

        Scribe_Values.Look(ref totalFacilityLevel, "totalFacilityLevel", 0);
        Scribe_Defs.Look(ref buildingFacility, "buildingFacility");
        Scribe_Values.Look(ref buildingTicksLeft, "buildingTicksLeft", -1);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"TotalFacilityLevel: {totalFacilityLevel}");
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> facility in facilities)
        {
            listing_Rect.SubLabel($"{facility.Key.label}: {facility.Value}", 0.8f);
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("BuildingFacility");
        if (buildingFacility is null)
        {
            listing_Rect.SubLabel("None", 0.8f);
        }
        else
        {
            listing_Rect.SubLabel(buildingFacility.label, 0.8f);
        }
        listing_Rect.Label($"BuildingTicksLeft: {buildingTicksLeft}");
    }

    public void TickHour()
    {
        if (buildingTicksLeft > 0 && (buildingTicksLeft -= 2500) <= 0)
        {
            CompleteFacilityConstruction();
        }
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
        BranchFacilityLevel newLevel = BranchUtility.BranchFacilityLevelOffSetBy(oldLevel, 1);

        if (byPlayer)
        {
            int silverCost = BranchUtility.GetFacilitySilverCost(Branch, facilityDef, newLevel);
            if (!CaravanInventoryUtility.HasThings(caravan, ThingDefOf.Silver, silverCost))
            {
                return resultOnly ? false : "OARO_NotEnoughSilver".Translate(silverCost);
            }
        }

        return true;
    }

    public void StartFacilityConstruction(BranchFacilityDef facilityDef, bool byPlayer, Caravan caravan = null)
    {
        BranchFacilityLevel oldLevel = GetFacilityLevel(facilityDef);
        if (oldLevel == BranchFacilityLevel.Excellent)
        {
            return;
        }

        buildingFacility = facilityDef;
        buildingTicksLeft = Find.TickManager.TicksGame;

        if (byPlayer)
        {
            int silverCost = BranchUtility.GetFacilitySilverCost(Branch, facilityDef, BranchUtility.BranchFacilityLevelOffSetBy(oldLevel, 1));
            OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.Silver, silverCost);
        }

        Branch.StoresReserveHandler.Notify_BranchConstructStarted(facilityDef);
    }

    private void CompleteFacilityConstruction()
    {
        if (buildingFacility is null)
        {
            return;
        }
        TryUpgradeFacility(buildingFacility, BranchUtility.BranchFacilityLevelOffSetBy(GetFacilityLevel(buildingFacility), 1));

        buildingTicksLeft = -1;
        buildingFacility = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BranchFacilityLevel GetFacilityLevel(BranchFacilityDef facilityDef) => facilities.TryGetValue(facilityDef, fallback: BranchFacilityLevel.None);

    public void AddFacility(BranchFacilityDef facilityDef)
    {
        if (facilityDef is null || facilities.ContainsKey(facilityDef))
        {
            return;
        }
        ActiveStage(facilityDef, BranchFacilityLevel.None, BranchFacilityLevel.Poor, isPostInit: false);
    }

    public bool TryUpgradeFacility(BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel, bool addIfMiss = false)
    {
        if (facilityDef is null || targetLevel == BranchFacilityLevel.None)
        {
            return false;
        }

        BranchFacilityLevel oldLevel = facilities.TryGetValue(facilityDef, fallback: BranchFacilityLevel.None);
        if ((!addIfMiss && oldLevel == BranchFacilityLevel.None) || oldLevel == BranchFacilityLevel.Excellent || oldLevel >= targetLevel)
        {
            return false;
        }

        if (ActiveStage(facilityDef, oldLevel, targetLevel, isPostInit: false))
        {
            facilities[facilityDef] = targetLevel;
            IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool ActiveStage(BranchFacilityDef facilityDef, BranchFacilityLevel minLevelExclude, BranchFacilityLevel maxLevelInclude, bool isPostInit = false)
    {
        foreach (BranchFacilityLevelStage stage in facilityDef.GetAllUpgradeStages(minLevelExclude, maxLevelInclude))
        {
            Branch.EffectTags.IncrementTagsValue(stage.effectFlags, addIfMiss: true);
            Branch.TransformerHandler.AddStatModifiers(stage.statModifies);
            if (isPostInit)
            {
                stage.PostLoadInit(Branch);
            }
            else
            {
                stage.PostActive(Branch);
            }
        }

        if (!isPostInit)
        {
            totalFacilityLevel += (minLevelExclude - maxLevelInclude);
        }

        return true;
    }

    public BranchStatTransformer GetBranchStatTransformer(BranchStatDef statDef)
    {
        BranchStatTransformer transformer = BranchStatTransformer.DefaultTransformer;
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> facility in facilities)
        {
            if (facility.Value == BranchFacilityLevel.None)
            {
                continue;
            }

            foreach (BranchFacilityLevelStage stage in facility.Key.GetAllUpgradeStages(BranchFacilityLevel.None, facility.Value))
            {
                if (stage.statModifies.NullOrEmpty())
                {
                    continue;
                }

                foreach (BranchStatModifier statModifier in stage.statModifies)
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

    internal void PostLoadInit()
    {
        if (facilities.RemoveAll(kv => kv.Key is null || kv.Value == BranchFacilityLevel.None) > 0)
        {
            Log.Error($"{Branch} has null or None facilities after loading, Removed.");
        }

        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> item in facilities)
        {
            ActiveStage(item.Key, BranchFacilityLevel.None, item.Value, isPostInit: true);
        }

        IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);
    }

    internal void PostBranchGenerated()
    {
        List<BranchFacilityDef> allFacilities = DefDatabase<BranchFacilityDef>.AllDefsListForReading;
        for (int i = 0; i < allFacilities.Count; i++)
        {
            BranchFacilityLevel initLevel = Rand.Chance(0.3f) ? BranchFacilityLevel.Normal : BranchFacilityLevel.Poor;
            TryUpgradeFacility(allFacilities[i], initLevel, addIfMiss: true);
        }
    }
}