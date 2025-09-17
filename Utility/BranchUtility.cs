using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

public static class BranchUtility
{
    public static bool InitBranchForNewOrder(this RatkinOrder order)
    {
        if (order is null || order.Faction is null || order.BranchManager is null)
        {
            return false;
        }

        BranchManager branchManager = order.BranchManager;
        bool atLeastOneSite = false;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (Rand.Chance(0.4f))
            {
                continue;
            }
            try
            {
                if (branchManager.GenerateBranchFor(order, settlement, addToManager: true))
                {
                    atLeastOneSite = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create a new branch for {order} at {settlement}: " + ex);
                continue;
            }
        }
        return atLeastOneSite;
    }

    public static IEnumerable<Branch> GetAllAffectedBranchSiteForOrder(this RatkinOrder order, PlanetTile tile)
    {
        return order.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile));
    }

    public static IEnumerable<Branch> GetAllAffectedBranchSiteForOrder(this RatkinOrder order, PlanetTile tile, Predicate<Branch> predicate)
    {
        return order.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile) && predicate(b));
    }

    public static List<Branch> GetAllAffectedBranchSite(PlanetTile tile)
    {
        ConcurrentBag<Branch> result = [];
        RatkinOrderManager.Instance.AllRatkinOrders
            .AsParallel()
            .ForAll(order =>
            {
                IEnumerable<Branch> affectedBranches = order.BranchManager.AllBranches
                    .Where(b => b.IsInAffectedRange(tile));

                foreach (Branch branch in affectedBranches)
                {
                    result.Add(branch);
                }
            });

        return result.ToList();
    }

    public static List<Branch> GetAllAffectedBranchSite(PlanetTile tile, Predicate<Branch> predicate)
    {
        ConcurrentBag<Branch> result = [];
        RatkinOrderManager.Instance.AllRatkinOrders
            .AsParallel()
            .ForAll(order =>
            {
                IEnumerable<Branch> affectedBranches = order.BranchManager.AllBranches
                    .Where(b => b.IsInAffectedRange(tile) && predicate(b));

                foreach (Branch branch in affectedBranches)
                {
                    result.Add(branch);
                }
            });

        return result.ToList();
    }

    public static bool CanBeSiteForNewBranch(this RatkinOrder order, WorldObject worldObject)
    {
        if (order is null || worldObject is null)
        {
            return false;
        }
        if (order.Faction != worldObject.Faction)
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

    public static AcceptanceReport CanInviteBranchCreation(this RatkinOrder order, Map map, PlanetTile tile, bool resultOnly)
    {
        if (map is null || order is null)
        {
            return false;
        }
        if (OAFrame_MapUtility.AmountSendableSilver(map) < order.BranchManager.SilverNeededForNextBranchCreation)
        {
            return resultOnly ? false : "NeedSilverLaunchable".Translate(order.BranchManager.SilverNeededForNextBranchCreation);
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
        return (curWO is null) || CanBeSiteForNewBranch(order, curWO);
    }

    public static bool InviteBranchCreation(this RatkinOrder order, Map map, WorldObject worldObject)
    {
        throw new NotImplementedException();
    }

    public static void InviteBranchCreationForNewWorldObject(this RatkinOrder order, Map map, WorldObjectDef worldObjectDef, PlanetTile tile)
    {
        WorldObject worldObject = WorldObjectMaker.MakeWorldObject(worldObjectDef);
        worldObject.Tile = tile;
        worldObject.SetFaction(order.Faction);

        if (InviteBranchCreation(order, map, worldObject))
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

    public static void OnBranchDestoryed(this QuestManager questManager, Branch branch)
    {
        ConcurrentBag<IOnBranchDestoryed> ratkinOrderRelateds = [];
        questManager.ActiveQuestsListForReading
            .AsParallel()
            .ForAll(quest =>
            {
                IEnumerable<IOnBranchDestoryed> relatedParts = quest.PartsListForReading.OfType<IOnBranchDestoryed>();
                foreach (IOnBranchDestoryed relatedPartInner in relatedParts)
                {
                    ratkinOrderRelateds.Add(relatedPartInner);
                }
            });

        foreach (IOnBranchDestoryed relatedPart in ratkinOrderRelateds)
        {
            relatedPart.Notify_BranchDestoryed(branch);
        }
    }
}
