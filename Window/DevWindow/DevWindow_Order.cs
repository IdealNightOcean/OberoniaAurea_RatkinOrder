using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_Order : DevWindowBase
{

    private readonly RatkinOrder ratkinOrder;

    public DevWindow_Order(RatkinOrder ratkinOrder) : base()
    {
        this.ratkinOrder = ratkinOrder;
        optionalTitle = ratkinOrder.Name;
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
            RatkinOrderManager.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"ID: {ratkinOrder.LoadID}");
        listing_Rect.Label($"Name: {ratkinOrder.Name}");
        listing_Rect.Label($"Faction: {ratkinOrder.Faction.Name}");
        Text.Font = GameFont.Small;

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Esteem:");
        Text.Font = GameFont.Small;
        ratkinOrder.EsteemHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Fund:");
        Text.Font = GameFont.Small;
        ratkinOrder.FundHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Reformation:");
        Text.Font = GameFont.Small;
        ratkinOrder.ReformationManager.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        if (listing_Rect.ButtonText("EffectTags", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(ratkinOrder.EffectTags.GetDetailString()));
        }
        if (listing_Rect.ButtonText("StatTransformers", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(ratkinOrder.TransformerHandler.GetDetailString()));
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Branch Manager:");
        Text.Font = GameFont.Small;
        if (listing_Rect.ButtonText("BranchManager DevWin", null, 0.8f))
        {
            Close();
            EndContents();
            ratkinOrder.BranchManager.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Squad Group Patrol Manager:");
        Text.Font = GameFont.Small;
        if (listing_Rect.ButtonText("GroupPatrolManager DevWin", null, 0.8f))
        {
            Close();
            EndContents();
            ratkinOrder.JointPatrolManager.OpenDevWindow();
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