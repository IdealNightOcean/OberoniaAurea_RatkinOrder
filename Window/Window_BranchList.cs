using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Window_BranchList : OrderWindowBase
{
    public override Vector2 InitialSize => new(713f, 685f);

    private Vector2 scrollPosition_Branch;

    private RatkinOrder RatkinOrder { get; }
    private Map Map { get; }
    private bool ConstructTab { get; set; }
    private List<TabRecord> Tabs { get; } = new(2);
    private List<BranchSummaryUICache> BranchSummaryUICaches { get; }

    public Window_BranchList(RatkinOrder ratkinOrder, Map map, bool initWithConstructTab)
    {
        RatkinOrder = ratkinOrder;
        Map = map;
        ConstructTab = initWithConstructTab;
        BranchSummaryUICaches = new(RatkinOrder.BranchManager.AllBranches.Count);
        foreach (Branch branch in RatkinOrder.BranchManager.AllBranches)
        {
            BranchSummaryUICaches.Add(new BranchSummaryUICache(branch, Map));
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = inRect.BottomPartPixels(653f);
        GUI.DrawTexture(mainRect, mainBackground);

        Tabs.Clear();
        Tabs.Add(new TabRecord("OARO_BranchListWin_Info".Translate().CapitalizeFirst(), delegate
        {
            ConstructTab = false;
        }, !ConstructTab));
        Tabs.Add(new TabRecord("OARO_BranchListWin_Construct".Translate().CapitalizeFirst(), delegate
        {
            ConstructTab = true;
        }, ConstructTab));
        TabDrawer.DrawTabs(mainRect, Tabs, maxTabWidth: 140f);


        Rect mainInnerRect = mainRect.ContractedBy(2f);

        if (OARO_WindowUtility.DrawCloseX(mainInnerRect))
        {
            Close();
            return;
        }

        Rect listViewRect = OARO_WindowUtility.CenterRect(mainInnerRect, 641f, 585f);
        Rect listOutRect = listViewRect;
        listOutRect.xMax += 16f;

        float entryX = listViewRect.xMin;
        float entryY = listViewRect.yMin;
        float entryWidth = listViewRect.width;
        float entryHeight = 117f;

        listViewRect.height = RatkinOrder.BranchManager.AllBranches.Count * entryHeight + 10f;

        Widgets.BeginScrollView(listOutRect, ref scrollPosition_Branch, listViewRect);
        foreach (BranchSummaryUICache branchSummary in BranchSummaryUICaches)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DarwBranchEntry(entryRect, branchSummary);
        }
        Widgets.EndScrollView();

        OARO_WindowUtility.ResetText();
    }

    private void DarwBranchEntry(Rect inRect, BranchSummaryUICache branchSummary)
    {
        GUI.DrawTexture(inRect, entryBackground);
        Rect innerRect = inRect.ContractedBy(2f);
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;

        Rect reusedRect = new(innerX, innerY, 224f, 86f);
        Branch branch = branchSummary.Branch;
        if (branch.HonorDef is not null)
        {
            GUI.DrawTexture(reusedRect, branch.HonorDef.backgroundTexture.Texture);
        }

        reusedRect.xMin += 12f;
        reusedRect.xMax -= 12f;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Rect reusedRectII = reusedRect.TopHalf();
        Widgets.Label(reusedRectII, branch.NameColored);
        reusedRectII = OARO_WindowUtility.CenterRectOnY(reusedRectII, reusedRect.xMax - 30f, 30f, 25f);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRectII,
            label: string.Empty,
            baseTex: IconLibrary.ellipsisButton,
            downTex: IconLibrary.ellipsisButton_Down,
            doMouseoverSound: true))
        {
            Window_Branch branchWin = new(branch, map: Map);
            Find.WindowStack.Add(branchWin);
            Close();
            return;
        }

        Text.Font = GameFont.Small;
        reusedRectII = reusedRect.BottomHalf();
        Widgets.Label(reusedRectII, "OARO_BranchListWin_Population".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRectII, $"{branch.PopulationHandler.Population}/{branch.PopulationHandler.PopulationCeiling}");

        Rect bottomRect = new(innerX, innerRect.yMax - 25f, innerRect.width, 25f);
        reusedRect = bottomRect;
        reusedRect.width = 4f;
        GUI.DrawTexture(reusedRect, branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = bottomRect;
        reusedRect.xMin += 12f;
        reusedRect.xMax -= 12f;
        Widgets.Label(reusedRect, branchSummary.SquadName);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, branch.PopulationHandler.PublicSecurityLabel);

        reusedRect = new(bottomRect.xMax - 137f, bottomRect.y, 137f, bottomRect.height);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: "OARO_BranchListWin_OpenSquadWin".Translate(),
            baseTex: squadButton,
            downTex: squadButton_Down,
            doMouseoverSound: true))
        {
            Window_BranchSquad squadWin = new(RatkinOrder, Map);
            squadWin.SelectSquad(branch);
            Find.WindowStack.Add(squadWin);
            Close();
            return;
        }

        reusedRect = new(innerX + 226f, innerY, 410f, 86f);
        if (ConstructTab)
        {
            DrawBranchConstruct(reusedRect, branchSummary);
        }
        else
        {
            DrawBranchInfo(reusedRect, branchSummary);
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawBranchInfo(Rect inRect, BranchSummaryUICache branchSummary)
    {
        Branch branch = branchSummary.Branch;
        float thirdWidth = inRect.width / 3f;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(inRect.x, inRect.y, thirdWidth, inRect.height);
        Widgets.Label(reusedRect.TopHalf(), "OARO_BranchWin_TotalFacilitiesLevel".Translate());
        Widgets.Label(reusedRect.BottomHalf(), branch.FacilityHandler.TotalFacilityLevel.ToString());

        reusedRect = new(inRect.x + thirdWidth, inRect.y, thirdWidth, inRect.height);
        Widgets.Label(reusedRect.TopHalf(), "OARO_BranchListWin_BuildingSlot".Translate());
        Widgets.Label(reusedRect.BottomHalf(), $"{branch.BuildingHandler.AllBuildingsCount}/{branch.BuildingHandler.BuildingCeiling}");

        reusedRect = new(inRect.xMax - thirdWidth, inRect.y, thirdWidth, inRect.height);
        Widgets.Label(reusedRect.TopHalf(), "OARO_BranchListWin_Distance".Translate());
        Widgets.Label(reusedRect.BottomHalf(), branchSummary.Distance.ToString("F0").Colorize(branchSummary.IsInAffectedRange ? Color.green : Color.white));

        OARO_WindowUtility.ResetText();
    }

    private void DrawBranchConstruct(Rect inRect, BranchSummaryUICache branchSummary)
    {
        Branch branch = branchSummary.Branch;
        BranchStoresReserveHandler.ReserveRecord primaryReserve = branch.StoresReserveHandler.PrimaryReserve;
        float thirdWidth = inRect.width / 3f;

        Rect reusedRect = new(inRect.x, inRect.y, thirdWidth, inRect.height);
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(reusedRect, "OARO_BranchListWin_PrimaryReserve".Translate());
        if (primaryReserve is null)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, "OARO_BranchListWin_NoReserve".Translate());

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(reusedRect, "--");

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRect.x + thirdWidth, inRect.y, thirdWidth, inRect.height);
            Widgets.Label(reusedRect.TopHalf(), "OARO_BranchListWin_ReserveReduce".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(reusedRect.BottomHalf(), "--%");
        }
        else
        {
            Rect iconRect = OARO_WindowUtility.CenterRect(reusedRect, 55f, 50f);
            GUI.DrawTexture(iconRect, primaryReserve.Target.iconTexture.Texture);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(reusedRect, primaryReserve.Target.LabelCap);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRect.x + thirdWidth, inRect.y, thirdWidth, inRect.height);
            Widgets.Label(reusedRect.TopHalf(), "OARO_BranchListWin_ReserveReduce".Translate());
            Widgets.Label(reusedRect.BottomHalf(), (-primaryReserve.CostRateReduce).ToStringPercentSigned("0.#"));
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(inRect.xMax - thirdWidth, inRect.y, thirdWidth, inRect.height);
        Widgets.Label(reusedRect.TopHalf(), "No.2");
        Text.Font = GameFont.Small;
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect.BottomHalf(),
            label: string.Empty,
            baseTex: reserveButton,
            downTex: reserveButton_Down,
            doMouseoverSound: true))
        {


        }

        OARO_WindowUtility.ResetText();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/RatkinOrder/BranchList/OARO_MainBackground");
    private static readonly Texture2D entryBackground = ContentFinder<Texture2D>.Get("UI/RatkinOrder/BranchList/OARO_EntryBackground");

    private static readonly Texture2D squadButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/BranchList/OARO_SquadButton");
    private static readonly Texture2D squadButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/BranchList/OARO_SquadButton_Down");

    private static readonly Texture2D reserveButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/BranchList/OARO_ReserveButton");
    private static readonly Texture2D reserveButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/BranchList/OARO_ReserveButton_Down");
}
