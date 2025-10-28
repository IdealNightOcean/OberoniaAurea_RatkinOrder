using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemandHandler : BranchRelatedRecordsHandler<AcceptedBranchDemand>
{
    public static AcceptedBranchDemandHandler Instance { get; private set; }
    public static void ClearStaticCache() => Instance = null;
    public AcceptedBranchDemandHandler()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }

    public bool CanAcceptDemand(Branch branch, BranchDemand demand)
    {
        if (records.Count >= 2)
        {
            return false;
        }

        return BranchDemandUtility.CanAcceptDemand(branch, demand);
    }

    public void AcceptDemand(Branch branch, BranchDemand demand)
    {
        AcceptedBranchDemand acceptedDemand = new(branch, demand);
        demand.OnAccepted(branch);

        if (demand.IsOngoing)
        {
            records.Add(acceptedDemand);
        }
    }

    public void Notify_DemandQuestClean(Quest quest)
    {
        AcceptedBranchDemand toRmove = null;
        foreach (AcceptedBranchDemand acceptedDemand in records)
        {
            if (quest == acceptedDemand.Demand.RelatedQuest)
            {
                toRmove = acceptedDemand;
                acceptedDemand.Branch.DemandHandler.Notify_DemandQuestClean(acceptedDemand.IsCritical);
                if (quest?.State == QuestState.EndedSuccess)
                {
                    GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchDemandCompleted, 1, addIfMiss: true);
                    if (acceptedDemand.IsCritical)
                    {
                        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.CriticalDemandCompleted, 1, addIfMiss: true);
                    }
                    else
                    {
                        GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.NormalDemandCompleted, 1, addIfMiss: true);
                    }
                    acceptedDemand.Branch.BranchManager.Notify_DemandQuestCompleted(acceptedDemand.IsCritical);
                }

                break;
            }
        }

        if (toRmove is not null)
        {
            records.Remove(toRmove);
        }
    }
}