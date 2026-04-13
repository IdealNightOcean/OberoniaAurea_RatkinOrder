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
        listing_Rect.Label("主团规台:");
        Text.Font = GameFont.Small;
        if (OrderStationHandler.Instance.MainOrderCodePedestal is null)
        {
            listing_Rect.Label("None".Translate());
        }
        else
        {
            listing_Rect.Label($"物品 Thing: {OrderStationHandler.Instance.MainOrderCodePedestal}");
            listing_Rect.Label($"地图 Map: {OrderStationHandler.Instance.MainOrderCodePedestal.MapHeld}");
            listing_Rect.Label($"骑士大厅等级: {OrderStationHandler.Instance.OrderHallLevel}");
            if (listing_Rect.ButtonText("JumpTo".Translate(), widthPct: 0.4f))
            {
                CameraJumper.TryJumpAndSelect(OrderStationHandler.Instance.MainOrderCodePedestal);
            }
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("常驻骑士:");
        Text.Font = GameFont.Small;
        ResidentPawnsManager.Instance.DrawDevWindow(listing_Rect);

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("附近骑士小组:");
        Text.Font = GameFont.Small;
        AroundKnightGroupsManager.Instance.DrawDevWindow(listing_Rect);

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