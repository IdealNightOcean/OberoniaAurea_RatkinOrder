using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchFacilityHandler : IExposable
{
    [Unsaved] private readonly Branch branch;

    private Dictionary<BranchFacilityDef, BranchFacilityLevel> facilities = [];
    public IReadOnlyDictionary<BranchFacilityDef, BranchFacilityLevel> Facilities => facilities;

    private readonly LazyMutable<int> totalFacilityLevel;
    public int TotalFacilityLevel => totalFacilityLevel.Value;

    public bool IsFacilityFullyCompleted { get; private set; }

    private Dictionary<BranchFacilityDef, UnderConstructionRecord<BranchFacilityDef>> underConstructionFacilities = [];
    public IReadOnlyDictionary<BranchFacilityDef, UnderConstructionRecord<BranchFacilityDef>> UnderConstructionFacilities => underConstructionFacilities;
    [Unsaved] private List<UnderConstructionRecord<BranchFacilityDef>> underConstructionFacilitiesList = [];

    public Action<BranchFacilityDef, bool> PostConstructionChanged { get; set; }

    public bool IsBusy => underConstructionFacilities.Count > 0;

    internal BranchFacilityHandler(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        totalFacilityLevel = new(refreshFunc: () => Mathf.Max(0, facilities.Sum(kv => (int)kv.Value)));
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref facilities, nameof(facilities), LookMode.Def, LookMode.Value);
        Scribe_Collections.Look(ref underConstructionFacilities, nameof(underConstructionFacilities), LookMode.Def, LookMode.Deep);
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"总设施等级: {TotalFacilityLevel}");
        listing_Rect.Label("所有设施等级");
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> facility in facilities)
        {
            listing_Rect.SubLabel($"{facility.Key.label}: {facility.Value}", 0.8f);
        }

        listing_Rect.Gap(6f);

        if (underConstructionFacilities.Count == 0)
        {
            listing_Rect.Label("在建设施: 无");
        }
        else
        {
            // listing_Rect.Label($"在建设施: {underConstructionFacility.TargetDef.label} | {underConstructionFacility.DurationTicksLeft}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BranchFacilityLevel GetFacilityLevel(BranchFacilityDef facilityDef) => facilities.TryGetValue(facilityDef, fallback: BranchFacilityLevel.None);

    public void TickHour()
    {
        if (underConstructionFacilitiesList.Count <= 0)
        {
            return;
        }

        int ticksGame = Find.TickManager.TicksGame;
        for (int i = underConstructionFacilitiesList.Count - 1; i >= 0; i--)
        {
            if (underConstructionFacilitiesList[i].CompletedTick <= ticksGame)
            {
                BranchFacilityDef facilityDef = underConstructionFacilitiesList[i].TargetDef;
                try
                {
                    TryAdjustFacilityStage(facilityDef, GetFacilityLevel(facilityDef).FacilityLevelOffSetBy(1), addIfMiss: true);
                }
                catch (Exception ex)
                {
                    ModUtility.LogExceptionError(ex,
                        errorDesc: "finish factity construction",
                        typeName: nameof(BranchFacilityHandler),
                        methodName: nameof(TickHour),
                        needStackTrace: true);
                }
                finally
                {
                    underConstructionFacilitiesList.RemoveAt(i);
                    underConstructionFacilities.Remove(facilityDef);
                }
            }
        }
    }

    public AcceptanceReport CanConstructFacility(BranchFacilityDef facilityDef, bool byPlayer, Map map = null, bool resultOnly = false)
    {
        if (underConstructionFacilities.Count > 0 && underConstructionFacilities.ContainsKey(facilityDef))
        {
            return resultOnly ? false : "OARO_OtherFacilityConstructing".Translate();
        }
        BranchFacilityLevel oldLevel = GetFacilityLevel(facilityDef);
        if (oldLevel == BranchFacilityLevel.Excellent)
        {
            return resultOnly ? false : "OARO_ReachMax_FacilityLevel".Translate();
        }
        BranchFacilityLevel targetLevel = oldLevel.FacilityLevelOffSetBy(1);

        if (byPlayer)
        {
            if (map is null)
            {
                return resultOnly ? false : "OARO_NoAvailablePlayerHomeMap".Translate();
            }
            int silverCost = branch.GetFacilitySilverCost(facilityDef, targetLevel, resultOnly: true, out _);
            if (!map.HasEnoughThingsOfDef(ThingDefOf.Silver, silverCost))
            {
                return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, silverCost.ToString());
            }
        }

        return true;
    }

    public void StartFacilityConstruction(BranchFacilityDef facilityDef, bool byPlayer, Map map = null)
    {

        BranchFacilityLevel oldLevel = GetFacilityLevel(facilityDef);
        if (oldLevel == BranchFacilityLevel.Excellent)
        {
            return;
        }
        if (underConstructionFacilities.ContainsKey(facilityDef))
        {
            return;
        }

        BranchFacilityLevel targetLevel = oldLevel.FacilityLevelOffSetBy(1);
        int buildingTicksCost = branch.GetFacilityTimeCost(facilityDef, targetLevel);
        UnderConstructionRecord<BranchFacilityDef> underConstructionFacility = new(facilityDef, buildingTicksCost);
        underConstructionFacilities.Add(facilityDef, underConstructionFacility);
        underConstructionFacilitiesList.Add(underConstructionFacility);

        if (byPlayer && map is not null)
        {
            int silverCost = branch.GetFacilitySilverCost(facilityDef, targetLevel, resultOnly: true, out _);
            map.DestroyThingsOfDef(ThingDefOf.Silver, silverCost);
        }

        branch.StoresReserveHandler.Notify_BranchConstructStarted(facilityDef);

        try
        {
            PostConstructionChanged?.Invoke(facilityDef, true);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"call-back: {nameof(PostConstructionChanged)}",
                typeName: nameof(BranchFacilityHandler),
                methodName: nameof(StartFacilityConstruction),
                needStackTrace: true);
        }
    }

    public void CancelFacilityConstruction(BranchFacilityDef facilityDef)
    {
        if (underConstructionFacilities.Count == 0)
        {
            return;
        }

        try
        {
            if (underConstructionFacilities.TryGetValue(facilityDef, out UnderConstructionRecord<BranchFacilityDef> record))
            {
                underConstructionFacilitiesList.Remove(record);
                PostConstructionChanged?.Invoke(facilityDef, false);
            }
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"call-back: {nameof(PostConstructionChanged)}",
                typeName: nameof(BranchFacilityHandler),
                methodName: nameof(CancelFacilityConstruction),
                needStackTrace: true);
        }
    }

    public bool TryAdjustFacilityStage(BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel, bool addIfMiss = false)
    {
        if (facilityDef is null)
        {
            return false;
        }

        if (!facilities.TryGetValue(facilityDef, out BranchFacilityLevel oldLevel) && !addIfMiss)
        {
            return false;
        }

        if (oldLevel == targetLevel)
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

        if (targetLevel == BranchFacilityLevel.None)
        {
            facilities.Remove(facilityDef);
        }
        else
        {
            ActiveStage(facilityDef, targetLevel);
            facilities[facilityDef] = targetLevel;
        }

        totalFacilityLevel.MarkDirty();
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

    public bool GetBranchStatTransformer(BranchStatDef statDef, out StatTransformer transformer)
    {
        transformer = new();
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

            List<OberoniaAurea.RatkinOrder.StatModifier<BranchStatDef>> statModifiers;
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
        if (facilities.RemoveAll(kv => kv.Value == BranchFacilityLevel.None) > 0)
        {
            Log.Error($"[OARO] {branch} 在加载后有空或级别为{BranchFacilityLevel.None}的设施，已移除。");
        }
        if (underConstructionFacilities.RemoveAll(kv => kv.Value is null || kv.Value.TargetDef is null) > 0)
        {
            Log.Error($"[OARO] {branch} 在加载后有空或无效的在建设施，已移除。");
        }

        underConstructionFacilitiesList = underConstructionFacilities.Values.ToList();

        int excellentFacilityCount = 0;
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> kv in facilities)
        {
            ActiveStage(kv.Key, kv.Value);
            if (kv.Value == BranchFacilityLevel.Excellent)
            {
                excellentFacilityCount++;
            }
        }

        totalFacilityLevel.MarkDirty();
        IsFacilityFullyCompleted = facilities.Count == excellentFacilityCount;
    }

    internal void PostBranchGenerated()
    {
        List<BranchFacilityDef> allFacilities = DefDatabase<BranchFacilityDef>.AllDefsListForReading;
        for (int i = 0; i < allFacilities.Count; i++)
        {
            BranchFacilityLevel initLevel = Rand.Chance(0.3f) ? BranchFacilityLevel.Normal : BranchFacilityLevel.Poor;
            TryAdjustFacilityStage(allFacilities[i], initLevel, addIfMiss: true);
        }
    }
}