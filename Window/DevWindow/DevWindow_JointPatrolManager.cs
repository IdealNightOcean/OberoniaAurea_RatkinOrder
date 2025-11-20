using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_JointPatrolManager : DevWindowBase
{
    private readonly RatkinOrder ratkinOrder;
    private readonly JointPatrolManager jointPatrolManager;
    private readonly float neededTaskPotency;

    public DevWindow_JointPatrolManager(RatkinOrder ratkinOrder) : base()
    {
        this.ratkinOrder = ratkinOrder;
        jointPatrolManager = ratkinOrder.JointPatrolManager;
        optionalTitle = ratkinOrder.Name;

        neededTaskPotency = jointPatrolManager.NeededTaskPotency;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect viewRect = inRect.ContractedBy(8f);
        viewRect.height = viewRectHeight;
        Listing_Standard listing_Rect = new(inRect, () => scrollPosition)
        {
            ColumnWidth = viewRect.width
        };
        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
        listing_Rect.Begin(viewRect);

        if (listing_Rect.ButtonText("GoBack".Translate(), null, 0.8f))
        {
            Close();
            EndContents();
            ratkinOrder.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        jointPatrolManager.DrawDevWindow(listing_Rect);
        listing_Rect.Label($"NeedReconnaissanceValue: {neededTaskPotency}");

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"All Participants: {jointPatrolManager.ParticipantsDict.Count}");
        Text.Font = GameFont.Small;
        listing_Rect.Gap(6f);

        foreach (JointBranchRecord record in jointPatrolManager.ParticipantsDict.Values)
        {
            if (listing_Rect.ButtonText(record.Branch.Name, null, 0.8f))
            {
                record.Branch.OpenDevWindow();
                Close();
                EndContents();
                return;
            }
        }

        if (Event.current.type == EventType.Layout)
        {
            viewRectHeight = listing_Rect.MaxColumnHeightSeen + 50f;
        }
        EndContents();

        void EndContents()
        {
            listing_Rect.End();
            Widgets.EndScrollView();
        }
    }
}