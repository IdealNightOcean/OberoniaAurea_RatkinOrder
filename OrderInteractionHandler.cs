using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemand : IExposable
{
    public Branch branch;
    public bool isCritical;

    public BranchDemand Demand => isCritical ? branch.DemandHandler.CriticalDemand : branch.DemandHandler.NormalDemand;

    private AcceptedBranchDemand() { }
    public AcceptedBranchDemand(Branch branch, BranchDemand demand)
    {
        this.branch = branch;
        isCritical = demand.Def.IsCriticalDemand;
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref isCritical, "isCritical", defaultValue: false);
    }
}

public class OrderInteractionHandler : IExposable, IRatkinOrderRelated, IBranchRelated
{
    public List<AcceptedBranchDemand> acceptedBranchDemands = new(2);

    public static OrderInteractionHandler Instance { get; private set; }

    public OrderInteractionHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }

    public static void ClearStaticCache() => Instance = null;

    public void Notify_RatkinOrderRemoved(RatkinOrder order)
    {
        acceptedBranchDemands.RemoveAll(ac => ac.branch?.RatkinOrder == order);
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        acceptedBranchDemands.RemoveAll(ac => ac.branch == branch);
    }

    public bool CanAcceptDemand(Branch branch, BranchDemand demand)
    {
        if (acceptedBranchDemands.Count >= 2)
        {
            return false;
        }

        return BranchDemandUtility.CanAcceptDemand(branch, demand);
    }

    public void AcceptDemand(Branch branch, BranchDemand demand)
    {
        AcceptedBranchDemand acceptedDemand = new(branch, demand);
        demand.Notify_Accepted(branch);

        if (demand.CurState == BranchDemand.DemandState.Ongoing)
        {
            acceptedBranchDemands.Add(acceptedDemand);
        }
    }

    public void Notify_DemandQuestClean(Quest quest)
    {
        AcceptedBranchDemand toRmove = null;
        foreach (AcceptedBranchDemand acceptedDemand in acceptedBranchDemands)
        {
            if (quest == acceptedDemand.Demand.RelatedQuest)
            {
                toRmove = acceptedDemand;
                acceptedDemand.branch.DemandHandler.Notify_DemandQuestClean(acceptedDemand.isCritical);
                if (quest?.State == QuestState.EndedSuccess)
                {
                    acceptedDemand.branch.BranchManager.Notify_DemandQuestCompleted(acceptedDemand.isCritical);
                }

                break;
            }
        }

        if (toRmove is not null)
        {
            acceptedBranchDemands.Remove(toRmove);
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref acceptedBranchDemands, "acceptedBranchDemands", LookMode.Deep);
    }

    public void PostLoadInit()
    {
        if (acceptedBranchDemands.RemoveAll(ac => ac is null || ac.branch is null) > 0)
        {
            Log.Error($"Some AcceptedBranchDemand were null or invalided after loading and have been removed.");
        }
    }
}
