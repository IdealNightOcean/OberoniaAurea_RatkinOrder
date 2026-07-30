using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder.Utility;

public static class BranchUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this RatkinOrder ratkinOrder) => ratkinOrder is not null && !ratkinOrder.HasRemoved;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this Branch branch) => branch is not null && branch.BaseSite is not null && branch.RatkinOrder.IsValid();

    /// <summary>
    /// 地块是否在分部影响范围内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInAffectedRange(this Branch branch, PlanetTile tile)
    {
        if (tile.Layer != branch.BaseSite.Tile.Layer)
        {
            return false;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(branch.BaseSite.Tile, tile) <= branch.GetStatValue(BranchStatDefOf.OARO_AffectRadius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceTo(this Branch branch, PlanetTile tile)
    {
        if (branch is null || branch.BaseSite is null)
        {
            return float.MaxValue;
        }
        if (tile.Layer != branch.BaseSite.Tile.Layer)
        {
            return float.MaxValue;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(branch.BaseSite.Tile, tile);
    }

    public static string GetBranchSiteName(Branch branch)
    {
        if (branch is null || branch.BaseSite is null)
        {
            return KeyLibrary_Misc.ErrorTipWithColor;
        }

        if (branch.BaseSite is INameableWorldObject nameSite)
        {
            return nameSite.Name;
        }
        else
        {
            return branch.BaseSite.Label;
        }
    }

    /// <summary>
    /// 分部是否正在边境轮巡
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnJointPatrol(this Branch branch)
    {
        return branch.RatkinOrder.JointPatrolManager.CurState != JointPatrolManager.PatrolState.Invalid && branch.RatkinOrder.JointPatrolManager.IsParticipant(branch);
    }

    /// <summary>
    /// 分部能否参与边境轮巡
    /// </summary>
    public static AcceptanceReport CanParticipateInJointPatrol(this Branch branch, bool resultOnly)
    {
        JointPatrolManager jointPatrolManager = branch?.RatkinOrder.JointPatrolManager;
        if (jointPatrolManager is null || jointPatrolManager.CurState != JointPatrolManager.PatrolState.Prepare)
        {
            return resultOnly ? false : "OARO_JointPatrol_NotInPrepareStage".Translate();
        }

        if (branch.TaskHandler.CurTask?.Def.canInterruptedByJointPatrol ?? false)
        {
            return resultOnly ? false : "OARO_JointPatrol_BusyWithTasks".Translate();
        }
        if (jointPatrolManager.IsParticipant(branch))
        {
            return resultOnly ? false : "OARO_JointPatrol_AlreadyParticipantIn".Translate();
        }
        return true;
    }

    /// <summary>
    /// 分部能否退出边境轮巡
    /// </summary>
    public static AcceptanceReport CanQuitJointPatrol(this Branch branch, bool resultOnly)
    {
        JointPatrolManager jointPatrolManager = branch?.RatkinOrder.JointPatrolManager;
        if (jointPatrolManager is null || jointPatrolManager.CurState != JointPatrolManager.PatrolState.Prepare)
        {
            return resultOnly ? false : "OARO_JointPatrol_NotInPrepareStage".Translate();
        }
        if (jointPatrolManager.ParticipantsDict.Count <= 2)
        {
            return resultOnly ? false : "OARO_JointPatrol_AtLeastTwoBranches".Translate();
        }
        if (!jointPatrolManager.IsParticipant(branch))
        {
            return resultOnly ? false : "OARO_JointPatrol_NotParticipantIn".Translate();
        }

        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanParticipateInJointPatrolFast(this Branch branch)
    {
        return branch.TaskHandler.CurTask?.Def.canInterruptedByJointPatrol ?? true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Branch> GetAllAvailableBranchForOrder(this RatkinOrder ratkinOrder, Predicate<Branch> predicate)
    {
        return ratkinOrder.BranchManager.AllBranches.Where(b => predicate(b));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Branch> GetAllAffectedBranchForOrder(this RatkinOrder ratkinOrder, PlanetTile tile)
    {
        return ratkinOrder.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Branch> GetAllAffectedBranchForOrder(this RatkinOrder ratkinOrder, PlanetTile tile, Predicate<Branch> predicate)
    {
        return ratkinOrder.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile) && predicate(b));
    }

    public static List<Branch> GetAllAffectedBranch(PlanetTile tile)
    {
        ConcurrentBag<Branch> result = [];
        _ = BranchStatDefOf.OARO_AffectRadius.Worker;
        RatkinOrderManager.Instance.AllRatkinOrders
            .AsParallel()
            .ForAll(order =>
            {
                IEnumerable<Branch> affectedBranches = order.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile));
                foreach (Branch branch in affectedBranches)
                {
                    result.Add(branch);
                }
            });

        return result.ToList();
    }

    public static List<Branch> GetAllAffectedBranches(PlanetTile tile, Predicate<Branch> predicate)
    {
        ConcurrentBag<Branch> result = [];
        _ = BranchStatDefOf.OARO_AffectRadius.Worker;
        RatkinOrderManager.Instance.AllRatkinOrders
            .AsParallel()
            .ForAll(order =>
            {
                IEnumerable<Branch> affectedBranches = order.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile) && predicate(b));

                foreach (Branch branch in affectedBranches)
                {
                    result.Add(branch);
                }
            });

        return result.ToList();
    }

    public static List<Branch> GetAllAvailableBranches(Predicate<Branch> predicate)
    {
        ConcurrentBag<Branch> result = [];
        RatkinOrderManager.Instance.AllRatkinOrders
            .AsParallel()
            .ForAll(order =>
            {
                IEnumerable<Branch> affectedBranches = order.BranchManager.AllBranches.Where(b => predicate(b));

                foreach (Branch branch in affectedBranches)
                {
                    result.Add(branch);
                }
            });

        return result.ToList();
    }

    public static bool CanBeSiteForNewBranch(this WorldObject worldObject, RatkinOrder ratkinOrder)
    {
        if (!ratkinOrder.IsValid() || worldObject is null)
        {
            return false;
        }
        if (ratkinOrder.Faction != worldObject.Faction)
        {
            return false;
        }

        WorldObjectComp_BranchSite branchSiteComp = worldObject.GetComponent<WorldObjectComp_BranchSite>();
        if (branchSiteComp is null || branchSiteComp.IsActive)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 重新获取该分部某个 <see cref="BranchStatDef"/> 的对应 <see cref="StatTransformer"/>
    /// </summary>
    public static void RecacheBranchStat(this Branch branch, BranchStatDef statDef)
    {
        if (branch.TransformerHandler.RemoveStatTransformer(statDef))
        {
            bool hasTransformer = false;
            StatTransformer transformer = new();
            StatTransformer tempTransformer;
            if (branch.FacilityHandler.GetBranchStatTransformer(statDef, out tempTransformer))
            {
                hasTransformer = true;
                transformer.MergeWith(tempTransformer);
            }
            if (branch.BuildingHandler.GetBranchStatTransformer(statDef, out tempTransformer))
            {
                hasTransformer = true;
                transformer.MergeWith(tempTransformer);
            }

            if (hasTransformer)
            {
                branch.TransformerHandler.MergeStatTransformer(statDef, transformer);
            }
        }
    }

    /// <summary>
    /// 通知 <see cref="QuestManager"/> 某个分部已被销毁
    /// </summary>
    /// <param name="branch">被销毁的分部</param>
    public static void OnBranchDestroyed(this QuestManager questManager, Branch branch)
    {
        ConcurrentBag<IOnBranchDestroyed> ratkinOrderRelateds = [];
        questManager.ActiveQuestsListForReading
            .AsParallel()
            .ForAll(quest =>
            {
                IEnumerable<IOnBranchDestroyed> relatedParts = quest.PartsListForReading.OfType<IOnBranchDestroyed>();
                foreach (IOnBranchDestroyed relatedPartInner in relatedParts)
                {
                    ratkinOrderRelateds.Add(relatedPartInner);
                }
            });

        foreach (IOnBranchDestroyed relatedPart in ratkinOrderRelateds)
        {
            relatedPart.Notify_BranchDestroyed(branch);
        }
    }

    /// <summary>
    /// 友好分部默认持续天数
    /// </summary>
    public static int GetDefaultFriendlyDurationDays(Branch branch)
    {
        int durationDays = 40;
        durationDays += branch.RatkinOrder.Esteem * 2;
        return durationDays;
    }

    public static AcceptanceReport CanChangeFocusedTaskType(Branch branch, bool resultOnly)
    {
        if (!branch.IsValid())
        {
            return false;
        }

        int cooldownTicksLeft = branch.CooldownManager.GetCooldownTicksLeft(KeyLibrary_CDRecord.FocusedTaskTypeChanged);
        if (cooldownTicksLeft > 0)
        {
            return resultOnly ? false : "OARO_Cooling_ChangeFocusedTaskType".Translate()
                                        + ", "
                                        + "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
        }

        if (branch.RatkinOrder.Relationship < EsteemHandler.RelationshipKind.Soulmate)
        {
            if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            {
                return true;
            }
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(EsteemHandler.RelationshipKind.Soulmate.GetLabel());
        }

        return true;
    }

    public static AcceptanceReport CanChangeRadicalismDegree(Branch branch, bool resultOnly)
    {
        if (!branch.IsValid())
        {
            return false;
        }

        int cooldownTicksLeft = branch.CooldownManager.GetCooldownTicksLeft(KeyLibrary_CDRecord.RadicalismDegreeChanged);
        if (cooldownTicksLeft > 0)
        {
            return resultOnly ? false : "OARO_Cooling_ChangeRadicalismDegree".Translate()
                                        + ", "
                                        + "WaitTime".Translate(cooldownTicksLeft.ToStringTicksToPeriod());
        }

        if (branch.RatkinOrder.Relationship < EsteemHandler.RelationshipKind.Soulmate)
        {
            if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            {
                return true;
            }
            return resultOnly ? false : "OARO_Insufficient_Relationship".Translate(EsteemHandler.RelationshipKind.Soulmate.GetLabel());
        }

        return true;
    }

    public static bool GenerateBranchOnTile(RatkinOrder ratkinOrder, PlanetTile tile, WorldObject worldObject = null)
    {
        if (worldObject is null)
        {
            WorldObject_BranchUnderConstruction siteUnderConstruction = (WorldObject_BranchUnderConstruction)WorldObjectMaker.MakeWorldObject(OARO_WorldObjectDefOf.OARO_WO_BranchUnderConstruction);
            siteUnderConstruction.Tile = tile;
            siteUnderConstruction.StartConstruction(ratkinOrder, 15 * 60000);
            Find.WorldObjects.Add(siteUnderConstruction);
            return true;
        }
        else if (worldObject.CanBeSiteForNewBranch(ratkinOrder))
        {
            return Branch.GenerateBranchFor(ratkinOrder, worldObject, addToManager: true) is not null;
        }

        return false;
    }

    public static AcceptanceReport CanAssignStoreReserveByPlayer(Branch branch, bool resultOnly)
    {
        if (branch is null)
        {
            return false;
        }

        if (!branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            return resultOnly ? false : "OARO_NotFriendlyBranch".Translate();
        }

        return true;
    }

    public static bool CanStoreReserve(Branch branch, BranchConstructionDef def)
    {
        if (branch.StoresReserveHandler.HasReservesOf(def))
        {
            return false;
        }

        if (def is BranchFacilityDef facilityDef)
        {
            BranchFacilityHandler facilityHandler = branch.FacilityHandler;
            if (facilityHandler.UnderConstructionFacilities.ContainsKey(facilityDef))
            {
                return false;
            }
            return facilityHandler.GetFacilityLevel(facilityDef) < BranchFacilityLevel.Excellent;

        }
        else if (def is BranchBuildingDef buildingDef)
        {
            BranchBuildingHandler buildingHandler = branch.BuildingHandler;
            if (buildingDef.isSpecial && buildingHandler.SpecialBuildingDef.Value is not null)
            {
                return false;
            }
            if (buildingHandler.UnderConstructionBuildingDefs.Contains(buildingDef))
            {
                return false;
            }

            return !buildingHandler.HasBuilding(buildingDef);
        }
        else
        {
            return false;
        }
    }


    public static IEnumerable<BranchBuildingDef> GetAllStorableBuildingDefs(Branch branch)
    {
        BranchBuildingHandler buildingHandler = branch.BuildingHandler;
        if (buildingHandler.HasUnusedSlots)
        {
            foreach (BranchBuildingDef buildingDef in DefDatabase<BranchBuildingDef>.AllDefsListForReading)
            {
                if (CanStoreReserve(branch, buildingDef))
                {
                    yield return buildingDef;
                }
            }
        }
    }

    public static IEnumerable<BranchFacilityDef> GetAllStorableFacilityDefs(Branch branch)
    {
        if (!branch.FacilityHandler.IsFacilityFullyCompleted)
        {
            foreach (BranchFacilityDef facilityDef in DefDatabase<BranchFacilityDef>.AllDefsListForReading)
            {
                if (CanStoreReserve(branch, facilityDef))
                {
                    yield return facilityDef;
                }
            }
        }
    }
}