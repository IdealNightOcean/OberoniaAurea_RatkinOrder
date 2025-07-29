using OberoniaAurea_Frame;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchManager : IExposable, IPostLoadInit, ITickHour, ITickDay
{
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private List<Branch> allBranches = [];
    public IReadOnlyCollection<Branch> AllBranches => allBranches;

    public IEnumerable<Branch> HonorBranches
    {
        get
        {
            foreach (Branch branch in allBranches)
            {
                if (branch.IsBranchOfType(BranchType.Honor))
                {
                    yield return branch;
                }
            }
        }
    }
    public IEnumerable<Branch> FriendlyBranches
    {
        get
        {
            foreach (Branch branch in allBranches)
            {
                if (branch.IsBranchOfType(BranchType.Friendly))
                {
                    yield return branch;
                }
            }
        }
    }

    [Unsaved] private SimpleValueCache<int> friendlyBranchesCountCache;
    public int FriendlyBranchesCount => friendlyBranchesCountCache.GetCachedResult();


    private Branch honorMobileBranch;
    private Branch normalMobileBranch;
    public IEnumerable<Branch> MobileBranches
    {
        get
        {
            if (honorMobileBranch is not null) { yield return honorMobileBranch; }
            if (normalMobileBranch is not null) { yield return normalMobileBranch; }
        }
    }

    public IEnumerable<(Branch Branch, BranchStoresReserve reserve)> AllPrimaryReserves
    {
        get
        {
            foreach (Branch branch in allBranches)
            {
                if (branch.StoresReserveHandler.PrimaryReserves is not null)
                {
                    yield return (branch, branch.StoresReserveHandler.PrimaryReserves);
                }
            }
        }
    }

    public int invitedBranchCreationsCount;
    public int SilverNeededForNextBranchCreation => 7500 + 5000 * invitedBranchCreationsCount;

    public BranchManager(RatkinOrder ratkinOrder)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        friendlyBranchesCountCache = new SimpleValueCache<int>(cacheInterval: 60000, () => FriendlyBranches.Count());
    }

    public void TickHour()
    {
        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].TickHour();
        }
    }

    public void TickDay()
    {
        DailyConstructCheck();
    }

    public Branch GenerateBranchFor(RatkinOrder order, WorldObject worldObject, bool addToManager = true)
    {
        Branch branch = Branch.GenerateBranchFor(order, worldObject);
        if (addToManager && branch is not null)
        {
            allBranches.Add(branch);
        }
        return branch;
    }

    private void DailyConstructCheck()
    {
        if (RatkinOrder.Funds < 0.7f)
        {
            return;
        }

        List<(Branch branch, BranchStoresReserve reserve)> potentialReserve = AllPrimaryReserves.Where(pr => pr.reserve.costRate <= 0.7f).ToList();

        for (int i = potentialReserve.Count - 1; i >= 0; i--)
        {
            (Branch branch, BranchStoresReserve reserve) = potentialReserve[i];

            if (Rand.Chance(0.05f))
            {
                bool successConstruct = false;
                if (reserve.targetBuilding is not null)
                {
                    if (branch.BuildingHandler.CanConstructBuilding(reserve.targetBuilding, reserve.inSpecialSlot, byPlayer: false, resultOnly: true))
                    {
                        branch.BuildingHandler.StartBuildingConstruction(reserve.targetBuilding, reserve.inSpecialSlot, byPlayer: false);
                        successConstruct = true;
                    }
                }
                else if (reserve.targetFacility is not null)
                {
                    if (branch.FacilityHandler.CanConstructFacility(reserve.targetFacility, byPlayer: false))
                    {
                        branch.FacilityHandler.StartFacilityConstruction(reserve.targetFacility, byPlayer: false);
                        successConstruct = true;
                    }
                }

                if (successConstruct)
                {
                    //消耗资金
                }
            }

            if (RatkinOrder.Funds < 0.5f)
            {
                break;
            }
        }

    }

    private void ReselectMobileBranch()
    {
        Branch preHonorMobileBranch = honorMobileBranch;
        Branch preNormalMobileBranch = normalMobileBranch;
        honorMobileBranch?.SetBranchType(BranchType.Mobile, false);
        normalMobileBranch?.SetBranchType(BranchType.Mobile, false);

        honorMobileBranch = HonorBranches?.Where(b => b != preHonorMobileBranch && b != preNormalMobileBranch)
                                         .RandomElementWithFallback(null)
                          ?? allBranches.Where(b => b != preHonorMobileBranch && b != preNormalMobileBranch)
                                        .RandomElementWithFallback(null)
                          ?? preHonorMobileBranch;

        normalMobileBranch = allBranches.Where(b => b != honorMobileBranch && b != preNormalMobileBranch)
                                        .RandomElementWithFallback(null)
                           ?? preNormalMobileBranch;

        honorMobileBranch?.SetBranchType(BranchType.Mobile, true);
        normalMobileBranch?.SetBranchType(BranchType.Mobile, true);
    }

    public void PostLoadInit()
    {
        if (allBranches.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"Some branches of {RatkinOrder} were null after loading and have been removed.");
        }

        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].PostLoadInit();
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref invitedBranchCreationsCount, "invitedBranchCreationsCount", 0);

        Scribe_Collections.Look(ref allBranches, "branches", LookMode.Deep, ctorArgs: this);
    }
}
