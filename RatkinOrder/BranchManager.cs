using OberoniaAurea_Frame;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;
using static OberoniaAurea.RatkinOrder.BranchDemand;

namespace OberoniaAurea.RatkinOrder;

public class BranchManager : IExposable, ITickDay
{
    [Unsaved] private readonly RatkinOrder ratkinOrder;

    private List<Branch> allBranches = [];
    public IReadOnlyList<Branch> AllBranches => allBranches;

    private JointPatrolManager jointPatrolManager;
    public JointPatrolManager JointPatrolManager => jointPatrolManager;
    public bool IsJointPatrolActived => jointPatrolManager is not null;

    [Unsaved] private readonly SimpleValueCache<int> totalKnightsCache;
    public int TotalKnights => totalKnightsCache.GetCachedResult();

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

    public IEnumerable<(Branch Branch, BranchStoresReserveHandler.ReserveRecord reserve)> AllPrimaryReserves
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
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        friendlyBranchesCountCache = new SimpleValueCache<int>(cacheInterval: 60000, () => FriendlyBranches.Count());
        totalKnightsCache = new SimpleValueCache<int>(cacheInterval: 60000, () => allBranches.Sum(b => b.Squad.AllCrewCountInt));
    }

    public void OpenDevWindow()
    {
        Find.WindowStack.Add(new DevWindow_BranchManager(ratkinOrder));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref invitedBranchCreationsCount, "invitedBranchCreationsCount", 0);

        Scribe_Collections.Look(ref allBranches, "branches", LookMode.Deep, ctorArgs: [ratkinOrder, false]);
        Scribe_Deep.Look(ref jointPatrolManager, "jointPatrolManager", ctorArgs: [ratkinOrder]);

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

    public void TickLong()
    {
        jointPatrolManager?.TickLong();
    }

    public void TickDay()
    {
        DailyConstructCheck();

        jointPatrolManager?.TickDay();

        PeriodicCriticalDemandTrigger();
    }

    public void AddBranch(Branch branch)
    {
        if (branch is null)
        {
            Log.Error("Attempted to add a null branch.");
            return;
        }
        if (branch.RatkinOrder != ratkinOrder)
        {
            Log.Error("Attempted to add a branch belonging to another RatkinOrder.");
            return;
        }

        if (!allBranches.Contains(branch))
        {
            allBranches.Add(branch);
        }
    }

    internal void Notify_MyOrderRemoved()
    {
        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].Destroy();
        }

        allBranches.Clear();
    }

    /// <summary>
    /// 只有单个分部销毁的时候使用
    /// 骑士团移除（所有分部销毁）时应由骑士团进行骑士团移除通知 （Notify_RatkinOrderRemoved | OnRatkinOrderRemoved）
    /// </summary>
    public void DestoryBranch(Branch branch)
    {
        if (!allBranches.Remove(branch))
        {
            Log.Error($"Attempted to destroy a branch that does not exist in {ratkinOrder.Name} ({ratkinOrder.GetUniqueLoadID()}). Branch ID: {branch.GetUniqueLoadID()}.");
            return;
        }

        branch.Destroy();

        if (branch == normalMobileBranch) { normalMobileBranch = null; }
        if (branch == honorMobileBranch) { honorMobileBranch = null; }
        GlobalInteractionManager.Instance.Notify_BranchDestroyed(branch);
        MapComponent_RatkinOrder.OnBranchDestroyed(branch);
        Find.QuestManager.OnBranchDestroyed(branch);
    }

    internal void Notify_JointPatrolEnd()
    {
        jointPatrolManager = null;
    }

    internal void Notify_DemandQuestCompleted(bool isCritical)
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
        if (ratkinOrder.Funds < 0.7f)
        {
            return;
        }

        List<(Branch branch, BranchStoresReserveHandler.ReserveRecord reserve)> potentialReserve = AllPrimaryReserves.Where(pr => pr.reserve.CostRateReduce >= 0.3f).ToList();

        for (int i = potentialReserve.Count - 1; i >= 0; i--)
        {
            (Branch branch, BranchStoresReserveHandler.ReserveRecord reserve) = potentialReserve[i];

            if (Rand.Chance(0.05f))
            {
                bool successConstruct = false;
                if (reserve.Target is BranchBuildingDef reserveBuilding)
                {
                    BranchBuildingConstructParameter constructParam = new(branch, reserveBuilding);
                    if (branch.BuildingHandler.CanConstructBuilding(constructParam, resultOnly: true))
                    {
                        branch.BuildingHandler.StartBuildingConstruction(constructParam);
                        successConstruct = true;
                    }
                }
                else if (reserve.Target is BranchFacilityDef reserveFacility)
                {
                    if (branch.FacilityHandler.CanConstructFacility(reserveFacility, byPlayer: false))
                    {
                        branch.FacilityHandler.StartFacilityConstruction(reserveFacility, byPlayer: false);
                        successConstruct = true;
                    }
                }

                if (successConstruct)
                {
                    ratkinOrder.FundHandler.AdjustFundsImmediately(-0.002f, "OARO_Fund_BranchConstruct".Translate());
                }
            }

            if (ratkinOrder.Funds < 0.2f)
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
        if (ratkinOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.CriticalDemandPeriodic))
        {
            return;
        }

        ratkinOrder.CooldownManager.RegisterRecord(KeyLibrary_CDRecord.CriticalDemandPeriodic, cdTicks: 5 * 60000);
        foreach (Branch branch in allBranches)
        {
            if (branch.DemandHandler.CanAddDemand(isCriticalDemand: true, ignoreCD: false, replaceCur: false))
            {
                if (Rand.Chance(BranchDemandUtility.GetCriticalDemandTriggerChance(branch, resultOnly: true, out _)))
                {
                    BranchDemandDef demandDef = BranchDemandUtility.GetRandomBranchDemandOfType(branch, DemandType.Critical);
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

    internal void PostLoadInit()
    {
        if (allBranches.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"Some branches of {ratkinOrder} were null after loading and have been removed.");
        }

        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].PostLoadInit();
        }
    }
}
