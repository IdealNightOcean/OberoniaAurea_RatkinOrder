using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class Window_QuestClique : OrderWindowBase
{
    private enum BottomType
    {
        Clique,
        Success,
        Fail
    }

    public override Vector2 InitialSize => new(1339f, 909f);

    private Quest Quest { get; }
    private BranchDemandDef DemandDef { get; }
    private QuestPart_CliquesManager CliquesManager { get; }
    private QuestPart_BranchDemandWatcher DemandWatcher { get; }
    private List<TabRecord> Tabs { get; } = new(2);
    private bool ShowBranchClique { get; set; }
    private BottomType BottomShowType { get; set; }

    private Branch MainBranch { get; }
    private Texture2D DemandTexture { get; }

    private IEnumerable<QuestClique> AllCliques => CliquesManager.AllCliques.Values;

    private Vector2 scrollPosition_ActiveCliques;
    private Vector2 scrollPosition_InactiveCliques;

    public Window_QuestClique(Quest quest, BranchDemandDef demandDef)
    {
        Quest = quest ?? throw new ArgumentNullException(nameof(quest));
        DemandDef = demandDef ?? throw new ArgumentNullException(nameof(demandDef));

        if (!quest.TryGetCliquesManager(addPartIfMiss: false, out QuestPart_CliquesManager cliquesManager))
        {
            throw new NullReferenceException(nameof(CliquesManager));
        }
        CliquesManager = cliquesManager;
        if (!quest.TryGetBranchDemandWatcher(out QuestPart_BranchDemandWatcher demandWatcher))
        {
            throw new NullReferenceException(nameof(DemandWatcher));
        }
        DemandWatcher = demandWatcher;
        MainBranch = DemandWatcher.Branch;
        DemandTexture = DemandDef.BackgroundTexture ?? IconLibrary.TransTex;
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);

        Rect mainInnerRect = inRect.ContractedBy(3f);
        float mainInnerX = mainInnerRect.xMin;
        float mainInnerY = mainInnerRect.yMin;

        if (OARO_WindowUtility.DrawCloseX(mainInnerRect))
        {
            Close();
            return;
        }

        Rect topRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerY + 168f, 1021f, 249f);
        DrawTopRect(topRect);

        Rect bottomRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerY + 464f, 1021f, 249f);
        DrawBottomRect(bottomRect);
    }

    private void DrawTopRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, cliqueRectBackground);

        Rect innerRect = inRect.ContractedBy(2f);
        Rect textRect = innerRect;
        textRect.height = 32f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = textRect;
        reusedRect.width = 180f;
        Widgets.Label(reusedRect, "OARO_CliqueWin_CliqueName".Translate());

        // reusedRect = new(reusedRect.xMax + 4f, textRect.yMin, 32f, textRect.height);

        reusedRect = new(reusedRect.xMax + 124f, textRect.yMin, 470f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_ActiveDesc".Translate());

        reusedRect = new(reusedRect.xMax - 110f, textRect.yMin, 110f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_CliquePotency".Translate());

        Rect outRect = innerRect;
        outRect.yMin += 32f;
        Rect viewRect = outRect;
        viewRect.width -= 18f;

        float entryX = viewRect.xMin;
        float entryY = viewRect.yMin;
        float entryWidth = viewRect.width;
        float entryHeight = 40f;

        IEnumerable<QuestClique> ActiveCliques = AllCliques.Where(c => c.IsActive);
        viewRect.height = entryHeight * ActiveCliques.Count();

        Widgets.BeginScrollView(outRect, ref scrollPosition_ActiveCliques, viewRect);
        foreach (QuestClique clique in ActiveCliques)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DrawActiveClique(entryRect, clique);
        }
        Widgets.EndScrollView();
        OARO_WindowUtility.ResetText();
    }

    private void DrawBottomRect(Rect inRect)
    {
        Tabs.Clear();
        Tabs.Add(new TabRecord("OARO_CliqueWin_Normal".Translate().CapitalizeFirst(), delegate
        {
            ShowBranchClique = false;
        }, !ShowBranchClique));
        Tabs.Add(new TabRecord("OARO_CliqueWin_Branch".Translate().CapitalizeFirst(), delegate
        {
            ShowBranchClique = true;
        }, ShowBranchClique));
        TabDrawer.DrawTabs(inRect, Tabs, maxTabWidth: 140f);

        GUI.DrawTexture(inRect, cliqueRectBackground);

        Rect mainRect = inRect.ContractedBy(2f);
        if (ShowBranchClique)
        {
            DrawBottomRect_Branch(mainRect);
        }
        else
        {
            DrawBottomRect_Normal(mainRect);
        }
    }

    private void DrawBottomRect_Normal(Rect inRect)
    {
        Rect textRect = inRect;
        textRect.height = 32f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = textRect;
        reusedRect.width = 220f;
        Widgets.Label(reusedRect, "OARO_CliqueWin_CliqueName".Translate());

        reusedRect = new(textRect.xMin + 360f, textRect.yMin, 220f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_NonActiveDesc".Translate());

        reusedRect = new(textRect.xMin + 600f, textRect.yMin, 60f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_CliquePotency".Translate());

        reusedRect = new(textRect.xMin + 725f, textRect.yMin, 90f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_PreferBuilding".Translate());

        reusedRect = new(textRect.xMax - 180f, textRect.yMin, 180f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_Willingness".Translate());

        Rect outRect = inRect;
        outRect.yMin += 32f;
        Rect viewRect = outRect;
        viewRect.width -= 18f;

        float entryX = viewRect.xMin;
        float entryY = viewRect.yMin;
        float entryWidth = viewRect.width;
        float entryHeight = 60f;

        IEnumerable<QuestClique> showCliques = AllCliques.Where(c => !c.IsActive && !c.IsBranchClique);
        viewRect.height = entryHeight * showCliques.Count();

        Widgets.BeginScrollView(outRect, ref scrollPosition_InactiveCliques, viewRect);
        foreach (QuestClique clique in showCliques)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DrawInactiveClique_Normal(entryRect, clique);
        }
        Widgets.EndScrollView();
        OARO_WindowUtility.ResetText();
    }

    private void DrawBottomRect_Branch(Rect inRect)
    {
        Rect textRect = inRect;
        textRect.height = 32f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = textRect;
        reusedRect.width = 220f;
        Widgets.Label(reusedRect, "OARO_CliqueWin_CliqueName".Translate());

        reusedRect = new(textRect.xMin + 305f, textRect.yMin, 90f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_CurState".Translate());

        reusedRect = new(textRect.xMin + 450f, textRect.yMin, 90f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_Supply".Translate());

        reusedRect = new(textRect.xMin + 600f, textRect.yMin, 60f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_CliquePotency".Translate());

        reusedRect = new(textRect.xMax - 180f, textRect.yMin, 180f, textRect.height);
        Widgets.Label(reusedRect, "OARO_CliqueWin_Willingness".Translate());

        Rect outRect = inRect;
        outRect.yMin += 32f;
        Rect viewRect = outRect;
        viewRect.width -= 18f;

        float entryX = viewRect.xMin;
        float entryY = viewRect.yMin;
        float entryWidth = viewRect.width;
        float entryHeight = 65f;

        IEnumerable<QuestClique> showCliques = AllCliques.Where(c => !c.IsActive && c.IsBranchClique);
        viewRect.height = entryHeight * showCliques.Count();

        Widgets.BeginScrollView(outRect, ref scrollPosition_InactiveCliques, viewRect);
        foreach (QuestClique clique in showCliques)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DrawInactiveClique_Clique(entryRect, clique);
        }
        Widgets.EndScrollView();
        OARO_WindowUtility.ResetText();
    }

    private void DrawActiveClique(Rect inRect, QuestClique clique)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin;
        float inRectHeight = inRect.height;

        Rect reusedRect = new(inRect.xMax - 274f, inRectY, 274f, inRectHeight);
        GUI.DrawTexture(reusedRect, potencyLace);

        reusedRect = new(inRectX, inRectY, 45f, inRectHeight);
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, 24f, 24f);
        if (clique.IsBranchClique)
        {
            OARO_WindowUtility.DrawBranchIcon(reusedRect, clique.RelatedBranch, expand: false);
        }
        else
        {
            GUI.DrawTexture(inRect, IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX + 45f, inRectY, 180f - 45f, inRectHeight);
        Widgets.Label(reusedRect, clique.Name);

        reusedRect = new(reusedRect.xMax + 4f, inRectY, 32f, inRectHeight);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: string.Empty,
            baseTex: IconLibrary.ellipsisButton,
            downTex: IconLibrary.ellipsisButton_Down))
        {

        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + 124f, inRectY, 470f, inRectHeight);
        Widgets.Label(reusedRect, clique.ActiveDesc);

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRect.xMax - 110f, inRectY, 110f, inRectHeight);
        Widgets.Label(reusedRect, clique.Potency.ToStringPercent());

        OARO_WindowUtility.ResetText();
    }

    private void DrawInactiveClique_Normal(Rect inRect, QuestClique clique)
    {
        Rect topRect = inRect;
        float topRectX = inRect.xMin;
        float topRectY = inRect.yMin;
        topRect.height = 40f;
        float topRectHeight = topRect.height;

        Rect reusedRect = new(topRectX, topRectY, 45f, topRectHeight);
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, 20f, 20f);
        GUI.DrawTexture(reusedRect, IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(topRectX + 45f, topRectY, 180f - 45f, topRectHeight);
        Widgets.Label(reusedRect, clique.Name);

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(topRectX + 360f, topRectY, 220f, topRectHeight);
        Widgets.Label(reusedRect, clique.InactiveDesc);

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(topRectX + 600f, topRectY, 60f, topRectHeight);
        Widgets.Label(reusedRect, clique.Potency.ToStringPercent());

        reusedRect = new(topRectX + 725f, topRectY, 90f, topRectHeight);
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, 20f, 20f);
        if (clique.PreferredBuilding is not null)
        {
            if (MainBranch?.BuildingHandler.HasBuilding(clique.PreferredBuilding) ?? false)
            {
                GUI.DrawTexture(reusedRect, IconLibrary.StarWhite);
            }
            else
            {
                GUI.DrawTexture(reusedRect, IconLibrary.StarBlack);
            }
        }

        reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, topRectX + 800f, 135f, 24f);
        Widgets.FillableBar(reusedRect, clique.Willingness, BaseContent.GreyTex);

        reusedRect = new(topRectX + 940f, topRectY, 60f, topRectHeight);
        Widgets.Label(reusedRect, clique.Willingness.ToStringPercent());

        OARO_WindowUtility.ResetText();
    }

    private void DrawInactiveClique_Clique(Rect inRect, QuestClique clique)
    {
        Branch branch = clique.RelatedBranch;

        Rect topRect = inRect;
        float topRectX = inRect.xMin;
        float topRectY = inRect.yMin;
        topRect.height = 40f;
        float topRectHeight = topRect.height;

        Rect reusedRect = new(topRectX, topRectY, 45f, topRectHeight);
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, 24f, 24f);
        OARO_WindowUtility.DrawBranchIcon(reusedRect, clique.RelatedBranch, expand: false);

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(topRectX + 45f, topRectY, 180f - 45f, topRectHeight);
        Widgets.Label(reusedRect, clique.Name);

        reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, reusedRect.xMax + 4f, 20f, 32f);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: string.Empty,
            baseTex: IconLibrary.ellipsisButton,
            downTex: IconLibrary.ellipsisButton_Down))
        {

        }

        reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, reusedRect.xMax + 4f, 32f, 32f);
        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            GUI.DrawTexture(reusedRect, friendlyHeart);
        }
        else
        {
            GUI.DrawTexture(reusedRect, nearClock);
        }

        reusedRect = new(reusedRect.xMax + 4f, topRectY, 96f, topRectHeight);
        GUI.DrawTexture(reusedRect, DemandTexture, ScaleMode.ScaleToFit);

        reusedRect = new(topRectX + 305f, topRectY, 90f, topRectHeight);
        Widgets.Label(reusedRect, branch.CurWorkStateDesc);

        reusedRect = new(topRectX + 450f, topRectY, 90f, topRectHeight);
        //

        reusedRect = new(topRectX + 600f, topRectY, 60f, topRectHeight);
        Widgets.Label(reusedRect, clique.Potency.ToStringPercent());

        reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, topRectX + 800f, 135f, 24f);
        Widgets.FillableBar(reusedRect, clique.Willingness, BaseContent.GreyTex);

        reusedRect = new(topRectX + 940f, topRectY, 60f, topRectHeight);
        Widgets.Label(reusedRect, clique.Willingness.ToStringPercent());

        OARO_WindowUtility.ResetText();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_MainBackground");
    private static readonly Texture2D cliqueRectBackground = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_CliqueRectBackground");

    private static readonly Texture2D potencyLace = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_PotencyLace");

    private static readonly Texture2D friendlyHeart = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_FriendlyHeart");
    private static readonly Texture2D nearClock = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_NearClock");
}
