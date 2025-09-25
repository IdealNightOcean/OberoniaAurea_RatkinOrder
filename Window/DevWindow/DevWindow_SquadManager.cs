using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_SquadManager : DevWindowBase
{
    private readonly SquadManager squadManager;
    private readonly IReadOnlyList<Squad> allSquads;

    public DevWindow_SquadManager(SquadManager squadManager) : base()
    {
        this.squadManager = squadManager;
        optionalTitle = squadManager.RatkinOrder.Name;
        allSquads = squadManager.AllSquads.ToList();
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
            squadManager.RatkinOrder.OpenDevWindow();
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        listing_Rect.Label($"SquadCount: {allSquads.Count}");
        listing_Rect.Label($"TotalMemberCount: {squadManager.TotalMemberCount}");
        listing_Rect.Label($"LastSquadBeAttackedTick: {squadManager.lastSquadBeAttackedTick}");

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("All Squads");
        Text.Font = GameFont.Small;
        listing_Rect.Gap(6f);
        int selectIndex = -1;
        for (int i = 0; i < allSquads.Count; i++)
        {
            if (listing_Rect.ButtonText(allSquads[i].Name, null, 0.8f))
            {
                selectIndex = i;
            }
        }

        if (selectIndex >= 0)
        {
            Close();
            EndContents();
            allSquads[selectIndex].OpenDevWindow();
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