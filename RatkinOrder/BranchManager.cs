using NightOcean;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using static OberoniaAurea.RatkinOrder.Branch;
using static OberoniaAurea.RatkinOrder.BranchDemand;

/// <summary>
/// 分部管理
/// </summary>
public class BranchManager : IExposable, ITickDay
{
    [Unsaved] private readonly RatkinOrder ratkinOrder;

    private List<Branch> allBranches = [];
    public IReadOnlyList<Branch> AllBranches => allBranches;

    public LazyMutable<int> TotalKnightsCount { get; }

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

    public LazyMutable<int> FriendlyBranchesCount { get; }

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

    public IEnumerable<KeyValuePair<Branch, BranchStoresReserveHandler.ReserveRecord>> AllPrimaryReserves
    {
        get
        {
            foreach (Branch branch in allBranches)
            {
                if (branch.StoresReserveHandler.PrimaryReserves is not null)
                {
                    yield return new(branch, branch.StoresReserveHandler.PrimaryReserves);
                }
            }
        }
    }

    private List<Branch> followedBranches = [];
    public IReadOnlyList<Branch> FollowedBranches => followedBranches;


    private int invitedBranchCreationsCount;
    public int InvitedBranchCreationsCount => invitedBranchCreationsCount;
    public int SilverNeededForNextBranchCreation => 2500 + 5000 * invitedBranchCreationsCount;

    private int normalDemandFulfillCount;
    private int criticalDemandFulfillCount;
    public int NormalDemandFulfillCount => normalDemandFulfillCount;
    public int CriticalDemandFulfillCount => criticalDemandFulfillCount;

    internal BranchManager(RatkinOrder ratkinOrder)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        FriendlyBranchesCount = new LazyMutable<int>(refreshFunc: () => FriendlyBranches.Count());
        TotalKnightsCount = new LazyMutable<int>(refreshFunc: () => allBranches.Sum(b => b.Squad.AllCrewCountInt));
    }

    public void OpenDevWindow() => Find.WindowStack.Add(new DevWindow_BranchManager(ratkinOrder));

    public void ExposeData()
    {
        Scribe_Values.Look(ref invitedBranchCreationsCount, nameof(invitedBranchCreationsCount), 0);

        Scribe_Values.Look(ref normalDemandFulfillCount, nameof(normalDemandFulfillCount), 0);
        Scribe_Values.Look(ref criticalDemandFulfillCount, nameof(criticalDemandFulfillCount), 0);

        Scribe_Collections.Look(ref allBranches, nameof(allBranches), LookMode.Deep, ctorArgs: [ratkinOrder, false]);
        Scribe_Collections.Look(ref followedBranches, nameof(followedBranches), LookMode.Reference);
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

    public void AddBranch(Branch branch)
    {
        if (branch is null)
        {
            Log.Error("[OARO] Attempted to add a null branch.");
            return;
        }
        if (branch.RatkinOrder != ratkinOrder)
        {
            Log.Error("[OARO] Attempted to add a branch belonging to another RatkinOrder.");
            return;
        }

        if (!allBranches.Contains(branch))
        {
            allBranches.Add(branch);
            TotalKnightsCount.MarkDirty();
            if (branch.IsBranchOfType(BranchType.Friendly))
            {
                FriendlyBranchesCount.MarkDirty();
            }
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
            Log.Error($"[OARO] Attempted to destroy a branch that does not exist in {ratkinOrder}. Branch: {branch}.");
            return;
        }
        followedBranches.Remove(branch);
        if (branch == normalMobileBranch) { normalMobileBranch = null; }
        if (branch == honorMobileBranch) { honorMobileBranch = null; }

        branch.Destroy();

        TotalKnightsCount.MarkDirty();
        if (branch.IsBranchOfType(BranchType.Friendly))
        {
            FriendlyBranchesCount.MarkDirty();
        }

        ratkinOrder.JointPatrolManager.Notify_BranchDestroyed(branch);
        GlobalInteractionManager.Instance.Notify_BranchDestroyed(branch);
        MapComponent_RatkinOrder.OnBranchDestroyed(branch);
        Find.QuestManager.OnBranchDestroyed(branch);
    }

    public void ChangeFollowedBranches(IEnumerable<Branch> branches)
    {
        followedBranches.Clear();
        if (branches is null)
        {
            return;
        }
        foreach (Branch branch in branches)
        {
            if (branch.RatkinOrder == ratkinOrder)
            {
                followedBranches.Add(branch);
            }
        }
    }

    public void Notify_DemandQuestCompleted(bool isCritical)
    {
        if (isCritical)
            criticalDemandFulfillCount++;
        else
            normalDemandFulfillCount++;
    }

    public void Notify_NewBranchInviteCreated() => invitedBranchCreationsCount++;

    private void DailyConstructCheck()
    {
        if (ratkinOrder.Funds < 0.7f)
        {
            return;
        }

        List<KeyValuePair<Branch, BranchStoresReserveHandler.ReserveRecord>> potentialReserves = AllPrimaryReserves.Where(pr => pr.Value.CostRateReduce >= 0.3f).ToList();

        for (int i = potentialReserves.Count - 1; i >= 0; i--)
        {
            if (Rand.Chance(0.95f))
            {
                continue;
            }

            (Branch branch, BranchStoresReserveHandler.ReserveRecord reserve) = potentialReserves[i];

            bool successConstruct = false;
            if (reserve.Target is BranchBuildingDef reserveBuilding)
            {
                BranchBuildingConstructParms constructParam = new(branch, reserveBuilding);
                if (branch.BuildingHandler.CanConstructBuilding(constructParam, resultOnly: true))
                {
                    branch.BuildingHandler.StartBuildingConstruction(constructParam);
                    successConstruct = true;
                }
            }
            else if (reserve.Target is BranchFacilityDef reserveFacility)
            {
                if (branch.FacilityHandler.CanConstructFacility(reserveFacility, byPlayer: false, resultOnly: true))
                {
                    branch.FacilityHandler.StartFacilityConstruction(reserveFacility, byPlayer: false);
                    successConstruct = true;
                }
            }

            if (successConstruct)
            {
                ratkinOrder.FundHandler.AdjustFundsImmediately(-0.002f, "OARO_Fund_BranchConstruct".Translate());
                if (ratkinOrder.Funds < 0.2f)
                {
                    break;
                }
            }
        }
    }

    private void DailyRandomUnlockKnightCommanderVisit()
    {
        if (Rand.Chance(0.99f))
        {
            return;
        }

        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: false);
        if (map is null)
        {
            return;
        }

        PlanetTile tile = map.Tile;
        Branch targetBranch = ratkinOrder.GetAllAvailableBranchForOrder(b => b.DistanceTo(tile) <= 20f).RandomElementWithFallback(fallback: null);
        if (targetBranch is null)
        {
            return;
        }

        targetBranch.CommanderVisitable = true;
        ChoiceLetter_RatkinOrder letter = (ChoiceLetter_RatkinOrder)LetterMaker.MakeLetter(
            label: "OARO_AutoUnlockCommanderVisitLabel".Translate(),
            text: "OARO_AutoUnlockCommanderVisitText".Translate(ratkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName), targetBranch.NameColored.Named(KeyLibrary_FormatArgName.BranchName)),
            def: OARO_LetterDefOf.OARO_Order_PositiveLetter,
            lookTargets: targetBranch.BaseSite,
            relatedFaction: ratkinOrder.Faction);
        letter.RelatedOrder = ratkinOrder;
        Find.LetterStack.ReceiveLetter(letter);
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
            if (!branch.DemandHandler.CanAddDemand(isCriticalDemand: true, ignoreCD: false, replaceCur: false))
            {
                continue;
            }

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
            Log.Error($"[OARO] Some branches of {ratkinOrder} were null after loading and have been removed.");
        }
        if (followedBranches.RemoveAll(b => b is null) > 0)
        {
            Log.Error($"[OARO] Some followed branches of {ratkinOrder} were null after loading and have been removed.");
        }

        for (int i = 0; i < allBranches.Count; i++)
        {
            allBranches[i].PostLoadInit();
        }
    }
}
