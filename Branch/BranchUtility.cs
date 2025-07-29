using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class BranchUtility
{
    public static bool InitBranchForNewOrder(RatkinOrder order)
    {
        if (order is null || order.Faction is null || order.BranchManager is null)
        {
            return false;
        }

        BranchManager branchManager = order.BranchManager;
        bool atLeastOneSite = false;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            try
            {
                if (branchManager.GenerateBranchFor(order, settlement, addToManager: true) is not null)
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

    public static IEnumerable<Branch> GetAllAffectedBranchSiteForOrder(RatkinOrder order, int tile)
    {
        return order.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile));
    }

    public static IEnumerable<Branch> GetAllAffectedBranchSiteForOrder(RatkinOrder order, int tile, Predicate<Branch> predicate)
    {
        return order.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile) && predicate(b));
    }

    public static IEnumerable<Branch> GetAllAffectedBranchSite(int tile)
    {
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            foreach (Branch branch in order.BranchManager.AllBranches
                                           .Where(b => b.IsInAffectedRange(tile))
                    )
            {
                yield return branch;
            }
        }
    }
    public static IEnumerable<Branch> GetAllAffectedBranchSite(int tile, Predicate<Branch> predicate)
    {
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            foreach (Branch branch in order.BranchManager.AllBranches
                                           .Where(b => b.IsInAffectedRange(tile)
                                                       && predicate(b))
                    )
            {
                yield return branch;
            }
        }
    }

    public static bool CanBeSiteForNewBranch(RatkinOrder order, WorldObject worldObject)
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

    public static AcceptanceReport CanInviteBranchCreation(Map map, RatkinOrder order, int tile)
    {
        if (map is null || order is null)
        {
            return false;
        }
        if (OAFrame_MapUtility.AmountSendableSilver(map) < order.BranchManager.SilverNeededForNextBranchCreation)
        {
            return "NeedSilverLaunchable".Translate(order.BranchManager.SilverNeededForNextBranchCreation);
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

    public static bool InviteBranchCreation(Map map, RatkinOrder order, WorldObject worldObject)
    {
        return false;
    }

    public static void InviteBranchCreationForNewWorldObject(Map map, RatkinOrder order, WorldObjectDef worldObjectDef, int tile)
    {
        WorldObject worldObject = WorldObjectMaker.MakeWorldObject(worldObjectDef);
        worldObject.Tile = tile;
        worldObject.SetFaction(order.Faction);

        if (InviteBranchCreation(map, order, worldObject))
        {
            Find.WorldObjects.Add(worldObject);
        }
        else
        {
            worldObject.Destroy();
        }
    }
}
