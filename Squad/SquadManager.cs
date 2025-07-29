using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class SquadManager : IExposable, IPostLoadInit, ITickHour
{

    [Unsaved] public readonly RatkinOrder RatkinOrder;
    [Unsaved] public readonly BranchManager BranchManager;

    public IEnumerable<Squad> AllSquads
    {
        get
        {
            foreach (Branch branch in BranchManager.AllBranches)
            {
                yield return branch.Squad;
            }
        }
    }
    public int AllSquadsCount => BranchManager.AllBranches.Count;

    public IEnumerable<Squad> FirendlySquads
    {
        get
        {
            foreach (Branch branch in BranchManager.FriendlyBranches)
            {
                yield return branch.Squad;
            }
        }
    }

    public IEnumerable<Squad> HonorSquads
    {
        get
        {
            foreach (Branch branch in BranchManager.HonorBranches)
            {
                yield return branch.Squad;
            }
        }
    }

    public int lastSquadBeAttackedTick = -1; //上次分队被攻击的Tick

    private SquadGroupPatrolManager groupPatrolManager;
    public SquadGroupPatrolManager GroupPatrolManager => groupPatrolManager;

    public int TotalMemberCount { get; private set; }

    public SquadManager(RatkinOrder ratkinOrder, bool initConstruct)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        BranchManager = ratkinOrder.BranchManager ?? throw new ArgumentNullException(nameof(ratkinOrder.BranchManager));
        if (initConstruct)
        {
            EnsureComponentsInit();
        }
    }

    public void TickHour()
    {

        if (groupPatrolManager.IsPatrolActived)
        {
            groupPatrolManager.TickHour();
        }
    }

    public void RecacheMemberCount()
    {
        TotalMemberCount = 0;
        foreach (Squad squad in AllSquads)
        {
            TotalMemberCount += squad.SquadStat.MemberCountInt;
        }
    }

    public void PostLoadInit()
    {
        groupPatrolManager ??= new(this);
        RecacheMemberCount();
    }

    private void EnsureComponentsInit()
    {
        groupPatrolManager ??= new(this);
    }

    public void ExposeData()
    {

    }
}
