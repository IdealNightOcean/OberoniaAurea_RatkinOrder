using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class DevWindow_Squad : DevWindowBase
{
    private readonly Branch branch;
    public DevWindow_Squad(Branch branch) : base()
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

        if (listing_Rect.ButtonText("GoBack".Translate(), null, 0.8f))
        {
            Close();
            EndContents();
            Find.WindowStack.Add(new DevWindow_Branch(branch));
            return;
        }

        BranchSquad squad = branch.Squad;

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
        listing_Rect.Label($"MemberCount: {squad.MemberCountInt}");
        listing_Rect.Label($"CommanderCount: {squad.CommanderCountInt}");
        listing_Rect.Gap(6f);
        if (listing_Rect.ButtonText("Member +1", widthPct: 0.6f))
        {
            squad.MemberCount += 1f;
        }
        if (listing_Rect.ButtonText("Commander +1", widthPct: 0.6f))
        {
            squad.MemberCount += 1f;
        }
        listing_Rect.Gap(6f);
        listing_Rect.Label($"MemberCeiling: {squad.MemberCeiling:F2}");
        listing_Rect.Label($"CommanderCeiling: {squad.CommanderCeiling:F2}");

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