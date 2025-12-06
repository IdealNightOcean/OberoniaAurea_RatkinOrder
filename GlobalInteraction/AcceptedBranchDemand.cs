using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AcceptedBranchDemand : IExposable
{
    private Branch branch;
    private bool isCritical;
    private BranchDemand demand;

    public Branch Branch => branch;
    public bool IsCritical => isCritical;
    public BranchDemand Demand => demand ??= (IsCritical ? branch.DemandHandler.CriticalDemand : branch.DemandHandler.NormalDemand);

    private AcceptedBranchDemand() : base() { }
    public AcceptedBranchDemand(Branch branch, bool isCritical)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        this.isCritical = isCritical;
    }

    public void Notify_DemandQuestClean(QuestState questState)
    {
        branch.DemandHandler.RemoveDemand(isCritical);
        if (questState == QuestState.EndedSuccess)
        {
            GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.BranchDemandCompleted, 1, addIfMiss: true);
            if (isCritical)
            {
                GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.CriticalDemandCompleted, 1, addIfMiss: true);
            }
            else
            {
                GlobalInteractionManager.InteractionRecord.OffsetTagValueBy(KeyLibrary_InteractRecord.NormalDemandCompleted, 1, addIfMiss: true);
            }
            branch.BranchManager.Notify_DemandQuestCompleted(isCritical);
        }
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Values.Look(ref isCritical, nameof(isCritical), defaultValue: false);
    }
}