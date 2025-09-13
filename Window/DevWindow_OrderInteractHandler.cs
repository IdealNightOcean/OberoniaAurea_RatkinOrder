using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class DevWindow_OrderInteractHandler : DevWindowBase
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
        if (OrderInteractionHandler.MainOrderCodePedestal is null)
        {
            listing_Rect.Label("None");
        }
        else
        {
            listing_Rect.Label($"Thing: {OrderInteractionHandler.MainOrderCodePedestal}");
            listing_Rect.Label($"Map: {OrderInteractionHandler.MainOrderCodePedestal.MapHeld}");
            listing_Rect.Label($"OrderHallLevel: {OrderInteractionHandler.OrderHallLevel}");
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("AroundKnightGroups:");
        Text.Font = GameFont.Small;
        OrderInteractionHandler.AroundKnightGroupsManager.DrawDevWindow(listing_Rect);

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