using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityHandler : IExposable
{
    [Unsaved] private readonly Branch branch;

    private Dictionary<BranchFacilityDef, BranchFacilityLevel> facilities = [];
    public IReadOnlyDictionary<BranchFacilityDef, BranchFacilityLevel> Facilities => facilities;

    [Unsaved] private int totalFacilityLevel;
    [Unsaved] private bool facilityLevelDirty = true;
    public int TotalFacilityLevel
    {
        get
        {
            if (facilityLevelDirty)
            {
                totalFacilityLevel = facilities.Sum(kv => (int)kv.Value);
                facilityLevelDirty = false;
            }
            return totalFacilityLevel;
        }
    }

    public bool IsFacilityFullyCompleted { get; private set; }

    private BranchFacilityDef buildingFacility;
    private int buildingTicksLeft = -1;
    public bool IsBusy => buildingFacility is not null;

    internal BranchFacilityHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref facilities, "facilities", LookMode.Def, LookMode.Value);

        Scribe_Defs.Look(ref buildingFacility, "buildingFacility");
        Scribe_Values.Look(ref buildingTicksLeft, "buildingTicksLeft", -1);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"TotalFacilityLevel: {TotalFacilityLevel}");
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> facility in facilities)
        {
            listing_Rect.SubLabel($"{facility.Key.label}: {facility.Value}", 0.8f);
        }

        listing_Rect.Gap(6f);

        if (buildingFacility is null)
        {
            listing_Rect.Label("BuildingFacility: None");
        }
        else
        {
            listing_Rect.Label($"BuildingFacility: {buildingFacility.label} | {buildingTicksLeft}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BranchFacilityLevel GetFacilityLevel(BranchFacilityDef facilityDef) => facilities.TryGetValue(facilityDef, fallback: BranchFacilityLevel.None);

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
        BranchFacilityLevel targetLevel = oldLevel.FacilityLevelOffSetBy(1);

        if (byPlayer)
        {
            int silverCost = branch.GetFacilitySilverCost(facilityDef, targetLevel);
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
        BranchFacilityLevel targetLevel = oldLevel.FacilityLevelOffSetBy(1);
        buildingTicksLeft = branch.GetFacilityTimeCost(facilityDef, targetLevel);

        if (byPlayer)
        {
            int silverCost = branch.GetFacilitySilverCost(facilityDef, targetLevel);
            OAFrame_CaravanUtility.RemoveThingsOfDef(caravan, ThingDefOf.Silver, silverCost);
        }

        branch.StoresReserveHandler.Notify_BranchConstructStarted(facilityDef);
    }

    private void CompleteFacilityConstruction()
    {
        if (buildingFacility is null)
        {
            return;
        }
        TryActiveNewStage(buildingFacility, GetFacilityLevel(buildingFacility).FacilityLevelOffSetBy(1), addIfMiss: true);

        buildingTicksLeft = -1;
        buildingFacility = null;
    }

    public bool TryActiveNewStage(BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel, bool addIfMiss = false)
    {
        if (facilityDef is null || targetLevel == BranchFacilityLevel.None)
        {
            return false;
        }

        if (!facilities.TryGetValue(facilityDef, out BranchFacilityLevel oldLevel) && !addIfMiss)
        {
            return false;
        }

        if (oldLevel == BranchFacilityLevel.Excellent || oldLevel >= targetLevel)
        {
            return false;
        }

        if (oldLevel != BranchFacilityLevel.None)
        {
            BranchFacilityLevelStage oldStage = facilityDef.GetLevelStage(oldLevel);
            if (oldStage is not null)
            {
                branch.EffectTags.DecrementTagsValue(oldStage.effectFlags);
                branch.TransformerHandler.UnmergeStatsOffset(oldStage.branchStatOffsets);
                branch.TransformerHandler.UnmergeStatsFactor(oldStage.branchStatFactors, doZeroUnmergedProcess: false);
            }
        }

        ActiveStage(facilityDef, targetLevel);

        facilities[facilityDef] = targetLevel;
        facilityLevelDirty = true;
        if (targetLevel == BranchFacilityLevel.Excellent)
        {
            IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);
        }

        branch.TransformerHandler.DoZeroFactorUnmergedProcess();

        return true;
    }

    private void ActiveStage(BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        BranchFacilityLevelStage targetStage = facilityDef.GetLevelStage(targetLevel);
        if (targetStage is not null)
        {
            branch.EffectTags.DecrementTagsValue(targetStage.effectFlags);
            branch.TransformerHandler.MergeStatOffsets(targetStage.branchStatOffsets, addIfMiss: true);
            branch.TransformerHandler.MergeStatFactors(targetStage.branchStatFactors, addIfMiss: true);
        }
    }

    public bool GetBranchStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        transformer = BranchStatTransformer.DefaultTransformer;
        bool hasTransformer = false;

        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> facility in facilities)
        {
            if (facility.Value == BranchFacilityLevel.None)
            {
                continue;
            }

            BranchFacilityLevelStage stage = facility.Key.GetLevelStage(facility.Value);
            if (stage is null)
            {
                continue;
            }

            List<BranchStatModifier> statModifiers;
            if (stage.branchStatOffsets is not null)
            {
                statModifiers = stage.branchStatOffsets;
                for (int i = 0; i < statModifiers.Count; i++)
                {
                    if (statModifiers[i].statDef == statDef)
                    {
                        hasTransformer = true;
                        transformer.MergeOffset(statModifiers[i].value);
                        break;
                    }
                }
            }
            if (stage.branchStatFactors is not null)
            {
                statModifiers = stage.branchStatFactors;
                for (int i = 0; i < statModifiers.Count; i++)
                {
                    if (statModifiers[i].statDef == statDef)
                    {
                        hasTransformer = true;
                        transformer.MergeFactor(statModifiers[i].value);
                        break;
                    }
                }
            }
        }
        return hasTransformer;
    }

    internal void PostLoadInit()
    {
        if (facilities.RemoveAll(kv => kv.Key is null || kv.Value == BranchFacilityLevel.None) > 0)
        {
            Log.Error($"{branch} has null or None facilities after loading, Removed.");
        }

        int excellentFacilityCount = 0;
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> kv in facilities)
        {
            ActiveStage(kv.Key, kv.Value);
            if (kv.Value == BranchFacilityLevel.Excellent)
            {
                excellentFacilityCount++;
            }
        }

        IsFacilityFullyCompleted = facilities.Count == excellentFacilityCount;
    }

    internal void PostBranchGenerated()
    {
        List<BranchFacilityDef> allFacilities = DefDatabase<BranchFacilityDef>.AllDefsListForReading;
        for (int i = 0; i < allFacilities.Count; i++)
        {
            BranchFacilityLevel initLevel = Rand.Chance(0.3f) ? BranchFacilityLevel.Normal : BranchFacilityLevel.Poor;
            TryActiveNewStage(allFacilities[i], initLevel, addIfMiss: true);
        }
    }
}