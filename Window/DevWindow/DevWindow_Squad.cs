using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_Squad : DevWindowBase
{
    private readonly Squad squad;
    public DevWindow_Squad(Squad squad) : base()
    {
        this.squad = squad;
        optionalTitle = squad.Branch.NameFull;
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
            Find.WindowStack.Add(new DevWindow_Branch(squad.Branch));
            return;
        }

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label($"Name: {squad.Name}");
        Text.Font = GameFont.Small;

        listing_Rect.Gap(6f);
        listing_Rect.Label("————————————————");
        Text.Font = GameFont.Medium;
        listing_Rect.Label("Stat:");
        Text.Font = GameFont.Small;
        squad.SquadStat.DrawDevWindow(listing_Rect);

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