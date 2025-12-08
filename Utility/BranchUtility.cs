using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public static class BranchUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this RatkinOrder ratkinOrder) => ratkinOrder is not null && !ratkinOrder.HasRemoved;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this Branch branch) => branch is not null && branch.RatkinOrder is not null && !branch.RatkinOrder.HasRemoved;

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
        return Find.WorldGrid.ApproxDistanceInTiles(branch.BaseSite.Tile, tile) <= BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_AffectRadius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceTo(this Branch branch, PlanetTile tile)
    {
        if (tile.Layer != branch.BaseSite.Tile.Layer)
        {
            return 999999f;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(branch.BaseSite.Tile, tile);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanParticipateInJointPatrolFast(this Branch branch)
    {
        return branch.TaskHandler.CurTask?.Def.canInterruptedByJointPatrol ?? true;
    }

    public static AcceptanceReport CanUnlockSupportAuthority(Branch branch, Map map, bool resultOnly)
    {
        if (branch.HasSupportAuthority)
        {
            return resultOnly ? false : "OARO_AlreadyHasSupportAuthority".Translate();
        }

        RatkinOrder ratkinOrder = branch.RatkinOrder;
        if (ratkinOrder.Faction.HostileTo(Faction.OfPlayer))
        {
            return resultOnly ? false : "OARO_OrderFaction_Hostile".Translate();
        }
        if (RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map) < 1)
        {
            return resultOnly ? false : "OARO_Insufficient_CurRecommendation".Translate(1, ratkinOrder.Name);
        }

        return true;
    }

    public static void UnlockSupportAuthority(Branch branch, Map map)
    {
        RecommendationUtility.UseRecommendationOfMap(branch.RatkinOrder, map, 1);
        branch.HasSupportAuthority = true;
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

    public static List<Branch> GetAllAffectedBranch(PlanetTile tile, Predicate<Branch> predicate)
    {
        ConcurrentBag<Branch> result = [];
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

    public static AcceptanceReport IsValidTileForInviteBranchCreation(RatkinOrder ratkinOrder, Map map, PlanetTile tile, bool resultOnly)
    {
        if (map is null || !ratkinOrder.IsValid() || !tile.Valid)
        {
            return false;
        }

        if (tile.LayerDef != PlanetLayerDefOf.Surface)
        {
            return resultOnly ? false : "OARO_SurfaceOnly".Translate();
        }

        List<WorldObject> allWorldObjects = Find.WorldObjects.AllWorldObjects;

        WorldObject curWO = allWorldObjects.Where(w => w.Tile == tile).FirstOrFallback(fallback: null);
        if (curWO is null)
        {
            if (allWorldObjects.Any(w => w.Tile.Layer == tile.Layer && Find.WorldGrid.ApproxDistanceInTiles(w.Tile, tile) <= 3f))
            {
                return resultOnly ? false : "OARO_TooCloseToOtherWorldObjects".Translate(3.ToString());
            }
            return true;
        }

        return curWO.CanBeSiteForNewBranch(ratkinOrder);
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

    public static string GenerateBranchNameCore(RatkinOrder ratkinOrder)
    {
        GrammarRequest grammarRequest = new()
        {
            Includes = { ratkinOrder.Def.branchNameCoreSelecter }
        };

        return NameGenerator.GenerateName(grammarRequest, IsUniqueName, false, rootKeyword: "r_name");

        bool IsUniqueName(string name)
        {
            return !ratkinOrder.BranchManager.AllBranches.Select(b => b.NameCore).Contains(name);
        }
    }

    /// <summary>
    /// 分部的名称序号生成器
    /// </summary>
    /// <returns>1~999的尽量不重复的随机数</returns>
    public static int GetBranchOrdinal(Branch branch)
    {
        int m = 999;
        int a = 445;
        int c = 700001;
        unchecked
        {
            int ordinal = 31 * branch.LoadID + branch.RatkinOrder.LoadID;
            ordinal ^= (ordinal >> 16);
            ordinal = (a * ordinal + c) % m + 1;
            return ordinal > 0 ? ordinal : ordinal + m;
        }
    }

    /// <summary>
    /// 重新获取该分部某个 <see cref="BranchStatDef"/> 的对应 <see cref="BranchStatTransformer"/>
    /// </summary>
    public static void RecacheBranchStat(this Branch branch, BranchStatDef statDef)
    {
        if (branch.TransformerHandler.RemoveStatTransformer(statDef))
        {
            bool hasTransformer = false;
            BranchStatTransformer transformer = new();
            BranchStatTransformer tempTransformer;
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
}