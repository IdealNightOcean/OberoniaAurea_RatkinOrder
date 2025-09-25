using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_AllOrders : DevWindowBase
{
    private readonly IReadOnlyList<RatkinOrder> allRatkinOrders;
    public DevWindow_AllOrders() : base()
    {
        allRatkinOrders = RatkinOrderManager.Instance.AllRatkinOrders;
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
        listing_Rect.Label("All Ratkin Orders:");

        int selectIndex = -1;
        for (int i = 0; i < allRatkinOrders.Count; i++)
        {
            if (listing_Rect.ButtonText(allRatkinOrders[i].Name))
            {
                selectIndex = i;
            }
        }

        if (selectIndex >= 0)
        {
            Close();
            EndContents();
            allRatkinOrders[selectIndex].OpenDevWindow();
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