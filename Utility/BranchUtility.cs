using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public static class BranchUtility
{
    public static readonly BranchMedalRecord.BranchMedalType[] BranchMedalsArr = (BranchMedalRecord.BranchMedalType[])Enum.GetValues(typeof(BranchMedalRecord.BranchMedalType));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInAffectedRange(this Branch branch, PlanetTile tile)
    {
        if (tile.Layer != branch.WorldObject.Tile.Layer)
        {
            return false;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(branch.WorldObject.Tile, tile) <= BranchStatUtility.GetStatValue(branch, BranchStatDefOf.OARO_AffectRadius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceTo(this Branch branch, PlanetTile tile)
    {
        if (tile.Layer != branch.WorldObject.Tile.Layer)
        {
            return 999999f;
        }
        return Find.WorldGrid.ApproxDistanceInTiles(branch.WorldObject.Tile, tile);
    }

    /// <summary>
    /// 该分部是否正在边境轮巡
    /// </summary>
    public static bool IsOnJointPatrol(this Branch branch)
    {
        return branch.BranchManager.IsJointPatrolActived && branch.BranchManager.JointPatrolManager.IsParticipant(branch);
    }

    /// <summary>
    /// 该分部能否参与边境轮巡
    /// </summary>
    public static bool CanParticipateInJointPatrol(this Branch branch)
    {
        if (!branch.TaskHandler.HasTask)
        {
            return true;
        }
        if (branch.TaskHandler.CurTask.Def.canInterruptedByJointPatrol)
        {
            return true;
        }
        return false;
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

    public static bool CanBeSiteForNewBranch(this RatkinOrder ratkinOrder, WorldObject worldObject)
    {
        if (ratkinOrder is null || worldObject is null)
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

    public static AcceptanceReport CanInviteBranchCreation(this RatkinOrder ratkinOrder, Map map, PlanetTile tile, bool resultOnly)
    {
        if (map is null || ratkinOrder is null)
        {
            return false;
        }
        if (OAFrame_MapUtility.AmountSendableSilver(map) < ratkinOrder.BranchManager.SilverNeededForNextBranchCreation)
        {
            return resultOnly ? false : "NeedSilverLaunchable".Translate(ratkinOrder.BranchManager.SilverNeededForNextBranchCreation);
        }

        WorldObject curWO = null;
        List<WorldObject> worldObjects = Find.WorldObjects.AllWorldObjects;
        for (int i = 0; i < worldObjects.Count; i++)
        {
            if (worldObjects[i].Tile == tile)
            {
                curWO = worldObjects[i];
                break;
            }
        }
        return (curWO is null) || CanBeSiteForNewBranch(ratkinOrder, curWO);
    }

    public static bool InviteBranchCreation(this RatkinOrder ratkinOrder, Map map, WorldObject worldObject)
    {
        throw new NotImplementedException();
    }

    public static void InviteBranchCreationForNewWorldObject(this RatkinOrder ratkinOrder, Map map, WorldObjectDef worldObjectDef, PlanetTile tile)
    {
        WorldObject worldObject = WorldObjectMaker.MakeWorldObject(worldObjectDef);
        worldObject.Tile = tile;
        worldObject.SetFaction(ratkinOrder.Faction);

        if (InviteBranchCreation(ratkinOrder, map, worldObject))
        {
            Find.WorldObjects.Add(worldObject);
        }
        else
        {
            worldObject.Destroy();
        }
    }

    public static string GenerateBranchName(RatkinOrder ratkinOrder)
    {
        int ordinal = Rand.Range(1, 999);
        int unitsDigit = ordinal % 10;

        GrammarRequest grammarRequest = new()
        {
            Includes = { ratkinOrder.Def.branchNameMaker }
        };
        grammarRequest.Constants.Add("unitsDigit", unitsDigit.ToString());
        grammarRequest.Rules.Add(new Rule_String("ordinal", ordinal.ToString()));

        return NameGenerator.GenerateName(grammarRequest, IsUniqueName, false, rootKeyword: "r_name");

        bool IsUniqueName(string name)
        {
            return !ratkinOrder.BranchManager.AllBranches.Select(b => b.Name).Contains(name);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BranchFacilityLevel BranchFacilityLevelOffSetBy(BranchFacilityLevel level, int offset)
    {
        return (BranchFacilityLevel)Mathf.Clamp((int)level + offset, 0, 4);
    }

    /// <summary>
    /// 重新获取该分部某个BranchStatDef的对应BranchStatTransformer
    /// </summary>
    public static void RecacheBranchStat(this Branch branch, BranchStatDef statDef)
    {
        if (branch.TransformerHandler.RemoveStatRecord(statDef))
        {
            bool hasTransformer = false;
            BranchStatTransformer transformer = BranchStatTransformer.DefaultTransformer;
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
                branch.TransformerHandler.AddStatTransformer(statDef, transformer);
            }
        }
    }

    /// <summary>
    /// 获取升级设施所需白银花费
    /// </summary>
    public static int GetFacilitySilverCost(Branch branch, BranchFacilityDef facilityDef, BranchFacilityLevel targetLevel)
    {
        float baseCost = facilityDef.GetLevelStage(targetLevel)?.silverCost ?? BranchStatDefOf.OARO_ConstructionCost.baseValue;
        float result = branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCost, baseValueOverride: baseCost);
        result *= (1f - branch.StoresReserveHandler.GetReserveCostReduce(facilityDef));

        return (int)result;
    }

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
}
