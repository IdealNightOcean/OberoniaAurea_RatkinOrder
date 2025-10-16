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
    [Unsaved] private int totalFacilityLevel;

    public bool IsFacilityFullyCompleted { get; private set; }
    public Dictionary<BranchFacilityDef, BranchFacilityLevel> Facilities => facilities;
    public int TotalFacilityLevel => totalFacilityLevel;

    private BranchFacilityDef buildingFacility;
    private int buildingTicksLeft = -1;
    public bool IsBusy => buildingFacility is not null;

    public BranchFacilityHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

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
            int silverCost = BranchUtility.GetFacilitySilverCost(branch, facilityDef, newLevel);
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
            int silverCost = BranchUtility.GetFacilitySilverCost(branch, facilityDef, BranchUtility.BranchFacilityLevelOffSetBy(oldLevel, 1));
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
        TryActiveNewStage(buildingFacility, BranchUtility.BranchFacilityLevelOffSetBy(GetFacilityLevel(buildingFacility), 1));

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
        ActiveStage(facilityDef, BranchFacilityLevel.Poor);
    }

    public bool TryActiveNewStage(BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel, bool addIfMiss = false)
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

        HashSet<BranchStatDef> statNeedRecache = [];
        if (oldLevel != BranchFacilityLevel.None)
        {
            BranchFacilityLevelStage oldStage = facilityDef.GetLevelStage(oldLevel);
            if (oldStage is not null)
            {
                branch.EffectTags.DecrementTagsValue(oldStage.effectFlags);
                if (oldStage.statModifies is not null)
                {
                    List<BranchStatModifier> statModifies = oldStage.statModifies;
                    for (int i = 0; i < statModifies.Count; i++)
                    {
                        if (statModifies[i].Transformer.factor == 0f)
                        {
                            statNeedRecache.Add(statModifies[i].statDef);
                        }
                        else
                        {
                            branch.TransformerHandler.RemoveStatModifier(statModifies[i]);
                        }
                    }
                }
            }
        }

        ActiveStage(facilityDef, targetLevel);

        facilities[facilityDef] = targetLevel;
        IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);

        //需要在设施等级改变后再次重新获取 factor == 0f 的BranchStat
        if (statNeedRecache.Count > 0)
        {
            foreach (BranchStatDef statDef in statNeedRecache)
            {
                branch.RecacheBranchStat(statDef);
            }
        }

        return true;
    }

    private void ActiveStage(BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        BranchFacilityLevelStage targetStage = facilityDef.GetLevelStage(targetLevel);
        if (targetStage is not null)
        {
            branch.EffectTags.DecrementTagsValue(targetStage.effectFlags);
            branch.TransformerHandler.AddStatModifiers(targetStage.statModifies);
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
            if (stage is null || stage.statModifies.NullOrEmpty())
            {
                continue;
            }

            for (int i = 0; i < stage.statModifies.Count; i++)
            {
                if (stage.statModifies[i].statDef == statDef)
                {
                    hasTransformer = true;
                    transformer.MergeWith(stage.statModifies[i].Transformer);
                    break;
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

        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> item in facilities)
        {
            ActiveStage(item.Key, item.Value);
        }

        IsFacilityFullyCompleted = facilities.Count == facilities.Count(kv => kv.Value == BranchFacilityLevel.Excellent);
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