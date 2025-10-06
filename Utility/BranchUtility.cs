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
    public static readonly BranchMedalRecord.BranchMedalType[] BranchMedalsArr = (BranchMedalRecord.BranchMedalType[])Enum.GetValues(typeof(BranchMedalRecord.BranchMedalType));

    public static bool InitBranchForNewOrder(this RatkinOrder ratkinOrder)
    {
        if (ratkinOrder is null || ratkinOrder.Faction is null || ratkinOrder.BranchManager is null)
        {
            return false;
        }

        bool atLeastOneSite = false;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (Rand.Chance(0.4f))
            {
                continue;
            }
            try
            {
                if (Branch.GenerateBranchFor(ratkinOrder, settlement, addToManager: true) is not null)
                {
                    atLeastOneSite = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create a new branch for {ratkinOrder} at {settlement}: " + ex);
                continue;
            }
        }
        return atLeastOneSite;
    }

    public static IEnumerable<Branch> GetAllAvailableBranchForOrder(this RatkinOrder ratkinOrder, Predicate<Branch> predicate)
    {
        return ratkinOrder.BranchManager.AllBranches.Where(b => predicate(b));
    }

    public static IEnumerable<Branch> GetAllAffectedBranchForOrder(this RatkinOrder ratkinOrder, PlanetTile tile)
    {
        return ratkinOrder.BranchManager.AllBranches.Where(b => b.IsInAffectedRange(tile));
    }

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
