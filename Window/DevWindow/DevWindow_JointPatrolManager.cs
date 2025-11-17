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
        jointPatrolManager = ratkinOrder.BranchManager.JointPatrolManager;
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
        listing_Rect.Label($"All Participants: {jointPatrolManager.Participants.Count}");
        Text.Font = GameFont.Small;
        listing_Rect.Gap(6f);

        int selectIndex = -1;
        for (int i = 0; i < jointPatrolManager.Participants.Count; i++)
        {
            if (listing_Rect.ButtonText(jointPatrolManager.Participants[i].Branch.Name, null, 0.8f))
            {
                selectIndex = i;
                break;
            }
        }

        if (selectIndex >= 0)
        {
            Close();
            EndContents();
            jointPatrolManager.Participants[selectIndex].Branch.OpenDevWindow();
            selectIndex = -1;
            return;
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