using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class DevWindow_GlobalOrderInteractHandler : DevWindowBase
{
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

        Text.Font = GameFont.Medium;
        listing_Rect.Label("Main Order Code Pedestal:");
        Text.Font = GameFont.Small;
        if (OrderHallHandler.MainOrderCodePedestal is null)
        {
            listing_Rect.Label("None");
        }
        else
        {
            listing_Rect.Label($"Thing: {OrderHallHandler.MainOrderCodePedestal}");
            listing_Rect.Label($"Map: {OrderHallHandler.MainOrderCodePedestal.MapHeld}");
            listing_Rect.Label($"OrderHallLevel: {OrderHallHandler.OrderHallLevel}");
            if (listing_Rect.ButtonText("Jump to", widthPct: 0.4f))
            {
                CameraJumper.TryJumpAndSelect(OrderHallHandler.MainOrderCodePedestal);
            }
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Resident Knights:");
        Text.Font = GameFont.Small;
        ResidentKnightsManager.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Around Knight Groups:");
        Text.Font = GameFont.Small;
        AroundKnightGroupsManager.DrawDevWindow(listing_Rect);

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