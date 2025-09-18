using OberoniaAurea_Frame;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchManager : IExposable, IPostLoadInit, ITickDay
{
    [Unsaved] public readonly RatkinOrder RatkinOrder;

    private List<Branch> allBranches = [];
    public IReadOnlyList<Branch> AllBranches => allBranches;

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

    private int normalDemandFulfillCount;
    private int criticalDemandFulfillCount;
    public int NormalDemandFulfillCount => normalDemandFulfillCount;
    public int CriticalDemandFulfillCount => criticalDemandFulfillCount;

    public BranchManager(RatkinOrder ratkinOrder)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        friendlyBranchesCountCache = new SimpleValueCache<int>(cacheInterval: 60000, () => FriendlyBranches.Count());
    }

    public void OpenDevWindow()
    {
        Find.WindowStack.Add(new DevWindow_BranchManager(this));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref invitedBranchCreationsCount, "invitedBranchCreationsCount", 0);

        Scribe_Collections.Look(ref allBranches, "branches", LookMode.Deep, ctorArgs: this);

        Scribe_Values.Look(ref normalDemandFulfillCount, "normalDemandFulfillCount", 0);
        Scribe_Values.Look(ref criticalDemandFulfillCount, "criticalDemandFulfillCount", 0);
    }

    public void Tick()
    {
        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].Tick();
        }
    }

    public void TickDay()
    {
        DailyConstructCheck();
        PeriodicCriticalDemandTrigger();
    }

    public bool GenerateBranchFor(RatkinOrder order, WorldObject worldObject, bool addToManager = true)
    {
        Branch branch = Branch.GenerateBranchFor(order, worldObject);

        if (branch is null)
        {
            return false;
        }

        if (addToManager)
        {
            allBranches.Add(branch);
        }
        return true;
    }

    public void Notify_MyOrderRemoved()
    {
        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].Destroy();
        }

        allBranches.Clear();
    }

    public void DestoryBranch(Branch branch)
    {
        if (!allBranches.Remove(branch))
        {
            Log.Error($"Attempted to destroy a branch that does not exist in {RatkinOrder.Name} ({RatkinOrder.GetUniqueLoadID()}). Branch ID: {branch.GetUniqueLoadID()}.");
            return;
        }

        branch.Destroy();

        // 理论上，只有单个分部销毁的时候使用下列通知
        // 骑士团移除（所有分部销毁）时应由骑士团进行骑士团移除通知 （Notify_RatkinOrderRemoved | OnRatkinOrderRemoved）
        if (branch == normalMobileBranch) { normalMobileBranch = null; }
        if (branch == honorMobileBranch) { honorMobileBranch = null; }
        GlobalOrderInteractionManager.Instance.Notify_BranchDestoryed(branch);
        MapComponent_RatkinOrder.OnBranchDestoryed(branch);
        Find.QuestManager.OnBranchDestoryed(branch);
    }

    public void Notify_DemandQuestCompleted(bool isCritical)
    {
        if (isCritical)
        {
            criticalDemandFulfillCount++;
        }
        else
        {
            normalDemandFulfillCount++;
        }
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
                if (reserve.TargetBuilding is not null)
                {
                    BranchBuildingConstructParameter constructParam = new(branch, reserve.TargetBuilding, reserve.InSpecialSlot);
                    if (branch.BuildingHandler.CanConstructBuilding(constructParam, resultOnly: true))
                    {
                        branch.BuildingHandler.StartBuildingConstruction(constructParam);
                        successConstruct = true;
                    }
                }
                else if (reserve.TargetFacility is not null)
                {
                    if (branch.FacilityHandler.CanConstructFacility(reserve.TargetFacility, byPlayer: false))
                    {
                        branch.FacilityHandler.StartFacilityConstruction(reserve.TargetFacility, byPlayer: false);
                        successConstruct = true;
                    }
                }

                if (successConstruct)
                {
                    RatkinOrder.FundHandler.AdjustFundsImmediately(-0.002f);
                }
            }

            if (RatkinOrder.Funds < 0.5f)
            {
                break;
            }
        }

    }

    /// <summary>
    /// 每5日统一更新骑士团所有分部的关键需求
    /// </summary>
    private void PeriodicCriticalDemandTrigger()
    {
        if (RatkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.CriticalDemandPeriodic))
        {
            return;
        }

        RatkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.CriticalDemandPeriodic, cdTicks: 5 * 60000);
        foreach (Branch branch in allBranches)
        {
            if (branch.DemandHandler.CanAddDemand(isCriticalDemand: true, ignoreCD: false, replaceCur: false))
            {
                if (Rand.Chance(BranchDemandUtility.GetCriticalDemandTriggerChance(branch, resultOnly: true, out _)))
                {
                    BranchDemandType demandType = Rand.Bool ? BranchDemandType.Important : BranchDemandType.Core;
                    BranchDemandDef demandDef = BranchDemandUtility.GetRandomBranchDemandOfType(branch, demandType);
                    if (demandDef is not null)
                    {
                        branch.DemandHandler.AddNewDemand(demandDef);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 重选机动分队
    /// </summary>
    private void ReselectMobileBranch()
    {
        Branch preHonorMobileBranch = honorMobileBranch;
        Branch preNormalMobileBranch = normalMobileBranch;
        honorMobileBranch?.SetBranchType(BranchType.Mobile, false);
        normalMobileBranch?.SetBranchType(BranchType.Mobile, false);

        List<Branch> priorityBranches = [];
        List<Branch> alternativeBranches = [];
        foreach (Branch branch in allBranches)
        {
            if (branch == preHonorMobileBranch || branch == preNormalMobileBranch)
            {
                continue;
            }

            if (branch.IsBranchOfType(BranchType.Honor))
            {
                priorityBranches.Add(branch);
            }
            else
            {
                alternativeBranches.Add(branch);
            }
        }

        if (priorityBranches.Count > 0)
        {
            honorMobileBranch = priorityBranches.RandomElement();
        }
        else if (alternativeBranches.Count > 0)
        {
            honorMobileBranch = alternativeBranches.RandomElement();
            alternativeBranches.Remove(honorMobileBranch);
        }
        else
        {
            honorMobileBranch = preHonorMobileBranch;
        }

        if (alternativeBranches.Count > 0)
        {
            normalMobileBranch = alternativeBranches.RandomElement();
        }
        else
        {
            normalMobileBranch = preNormalMobileBranch;
        }

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
}
