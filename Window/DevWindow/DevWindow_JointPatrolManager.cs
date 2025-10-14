using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_JointPatrolManager : DevWindowBase
{
    private readonly RatkinOrder ratkinOrder;
    private readonly JointPatrolManager jointPatrolManager;
    private readonly float needReconnaissanceValue;
    private readonly string patrolEndChances;
    private readonly string endResultText;

    public DevWindow_JointPatrolManager(RatkinOrder ratkinOrder) : base()
    {
        this.ratkinOrder = ratkinOrder;
        jointPatrolManager = ratkinOrder.BranchManager.JointPatrolManager;
        optionalTitle = ratkinOrder.Name;

        needReconnaissanceValue = jointPatrolManager.NeedReconnaissanceValue;
        patrolEndChances = GetPatrolEndChancesString(jointPatrolManager.PatrolEndChances);
        endResultText = jointPatrolManager.endResultText.Length > 0 ? jointPatrolManager.endResultText.ToString() : "None";
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
        listing_Rect.Label($"NeedReconnaissanceValue: {needReconnaissanceValue}");

        Text.Font = GameFont.Medium;
        listing_Rect.Label("Patrol End Chances:");
        Text.Font = GameFont.Small;
        listing_Rect.Label(patrolEndChances);

        Text.Font = GameFont.Medium;
        listing_Rect.Label("End Result Text:");
        Text.Font = GameFont.Small;
        listing_Rect.Label(endResultText);

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

    private static string GetPatrolEndChancesString(IEnumerable<(PatrolEndType, float)> patrolEndChances)
    {
        StringBuilder sb = new();
        foreach ((PatrolEndType, float) patrolEnd in patrolEndChances)
        {
            sb.AppendWithSeparator($"({patrolEnd.Item1}, {patrolEnd.Item2})", "  ");
        }
        return sb.ToString();
    }
}