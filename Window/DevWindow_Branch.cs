using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_Branch : DevWindowBase
{
    private readonly Branch branch;
    public DevWindow_Branch(Branch branch) : base()
    {
        this.branch = branch;
        optionalTitle = branch.NameFull;
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

        if (listing_Rect.ButtonText("BranchManager DevWin", null, 0.8f))
        {
            Close();
            EndContents();
            branch.BranchManager.OpenDevWindow();
            return;
        }
        if (listing_Rect.ButtonText("Order DevWin", null, 0.8f))
        {
            Close();
            EndContents();
            branch.RatkinOrder.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"ID: {branch.LoadID}");
        listing_Rect.Label($"Name: {branch.Name}");
        Text.Font = GameFont.Small;

        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("EffectTags", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(branch.EffectTags.GetDetailString()));
        }
        if (listing_Rect.ButtonText("StatTransformers", null, 0.8f))
        {
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(branch.TransformerHandler.GetDetailString()));
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"Squad:");
        Text.Font = GameFont.Small;
        if (listing_Rect.ButtonText("Squad", null, 0.8f))
        {
            Close();
            EndContents();
            branch.Squad.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Facility:");
        Text.Font = GameFont.Small;
        branch.FacilityHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Building:");
        Text.Font = GameFont.Small;
        branch.BuildingHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Demand:");
        Text.Font = GameFont.Small;
        branch.DemandHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Resident:");
        Text.Font = GameFont.Small;
        branch.ResidentHandler.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("StoresReserve:");
        Text.Font = GameFont.Small;
        branch.StoresReserveHandler.DrawDevWindow(listing_Rect);

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