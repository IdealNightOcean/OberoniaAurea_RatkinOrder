using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_QuestClique : OrderWindowBase
{
    private enum CornerType
    {
        Clique,
        Success,
        Fail
    }

    public override Vector2 InitialSize => new(1339f, 909f);

    private BranchDemand Demand { get; }
    private BranchDemandDef DemandDef => Demand.Def;
    private Map Map { get; }
    private QuestPart_CliquesManager CliquesManager { get; }
    private QuestPart_BranchDemandWatcher DemandWatcher { get; }
    private List<TabRecord> Tabs { get; } = new(2);
    private bool ShowBranchClique { get; set; }
    private CornerType CornerShowType { get; set; }

    private Branch MainBranch { get; }
    private Texture2D DemandTexture { get; }

    private LazyMutable<int> MapRecommendationCount { get; }
    private IEnumerable<QuestClique> AllCliques => CliquesManager.AllCliques.Values;

    private Vector2 scrollPosition_ActiveCliques;
    private Vector2 scrollPosition_InactiveCliques;
    private Vector2 scrollPosition_Medals;

    public Window_QuestClique(BranchDemand demand, Map map)
    {
        Demand = demand ?? throw new ArgumentNullException(nameof(demand));
        Quest quest = demand.RelatedQuest ?? throw new ArgumentNullException(nameof(quest));

        Map = map ?? OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: false) ?? Find.CurrentMap ?? throw new ArgumentNullException(nameof(map));
        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationCount(Map));

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

        if (OARO_WindowUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }

        Rect reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerY + 2f, 473f, 164f);
        GUI.DrawTexture(reusedRect, DemandTexture, ScaleMode.ScaleToFit);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(mainInnerRect.x, mainInnerY + 65f, mainInnerRect.width, 40f);
        Widgets.Label(reusedRect, DemandDef.LabelCap);

        Text.Font = GameFont.Small;
        reusedRect = new(mainInnerRect.x, reusedRect.yMax + 16f, mainInnerRect.width, 24f);
        Widgets.Label(reusedRect, MainBranch.NameColored);

        reusedRect = new(mainInnerRect.x + 770f, reusedRect.y, 92f, 24f);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: "OARO_CliqueWin_OpenSquadWin".Translate(),
            baseTex: smallButton,
            downTex: smallButton_Down,
            doMouseoverSound: true
            ))
        {

            Window_BranchSquad squadWin = new(MainBranch.RatkinOrder, OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: false));
            squadWin.SelectSquad(MainBranch);
            Find.WindowStack.Add(squadWin);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(mainInnerRect.x + 770f, mainInnerY + 85f, 400f, 20f);
        if (MainBranch.RatkinOrder.JointPatrolManager.CurState != JointPatrolManager.PatrolState.Invalid)
        {
            Widgets.Label(reusedRect, "OARO_CliqueWin_OnJointPatrol".Translate().Colorize(Color.cyan));
        }


        Rect topRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerY + 168f, 1021f, 249f);
        DrawTopRect(topRect);

        Rect bottomRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerY + 464f, 1021f, 249f);
        DrawBottomRect(bottomRect);

        Rect cornerRect = new(bottomRect.x, mainInnerY + 725f, 432f, 89f + 24f);
        DrawCornerRect(cornerRect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(mainInnerX + 765f, mainInnerY + 725f, 130f, 24f);
        Widgets.Label(reusedRect, "OARO_CliqueWin_TotalPotency".Translate());

        Text.Font = GameFont.Medium;
        reusedRect = new(mainInnerX + 765f, reusedRect.yMax + 32f, 130f, 40f);
        float totalPotency = CliquesManager.TotalPotency.Value;
        Widgets.Label(reusedRect, totalPotency.ToStringPercent().Colorize(totalPotency < 0f ? ColorLibrary.RedReadable : Color.green));

        if (Demand is BranchDemand_Critical criticalDemand)
        {
            Text.Font = GameFont.Small;
            reusedRect = new(mainInnerX + 1015f, mainInnerY + 725f, 150f, 24f);
            Widgets.Label(reusedRect, "OARO_CliqueWin_PotentialMedals".Translate());

            Rect medalOutRect = new(reusedRect.x, reusedRect.yMax + 32f, 150f, 40f);
            Rect medalViewRect = medalOutRect;
            float medalEntryX = medalOutRect.x;
            float medalEntryY = medalOutRect.y;
            float medalEntryWidth = 40f;
            float medalEntryHeight = 40f;
            float medalEntryXInterval = 15f;
            Widgets.BeginScrollView(medalOutRect, ref scrollPosition_Medals, medalViewRect, showScrollbars: false);
            foreach (BranchMedalDef medal in criticalDemand.PotentialMedals)
            {
                Rect medalEntryRect = new(medalEntryX, medalEntryY, medalEntryWidth, medalEntryHeight);
                medalEntryX += (medalEntryWidth + medalEntryXInterval);
                GUI.DrawTexture(medalEntryRect, medal.iconTexture.Texture);
            }
            Widgets.EndScrollView();
        }

        OARO_WindowUtility.ResetText();
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

        reusedRect = new(innerRect.xMax - 110f, textRect.yMin, 110f, textRect.height);
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
        int column = 0;
        foreach (QuestClique clique in ActiveCliques)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if (((++column) & 1) == 0)
            {
                GUI.DrawTexture(entryRect, IconLibrary.DarkTex);
            }
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
            DrawInactiveClique_Branch(entryRect, clique);
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

        reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 12f, 24f, 24f);
        if (clique.IsBranchClique)
        {
            OARO_WindowUtility.DrawBranchIcon(reusedRect, clique.RelatedBranch, expand: false);
        }
        else
        {
            GUI.DrawTexture(reusedRect, IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 45f, inRectY, 180f - 45f, inRectHeight);
        Widgets.LabelFit(reusedRect, clique.Name);
        TooltipHandler.TipRegion(reusedRect, () => clique.Name, uniqueId: 55454031);

        reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, reusedRect.xMax + 4f, 30f, 25f);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: string.Empty,
            baseTex: IconLibrary.ellipsisButton,
            downTex: IconLibrary.ellipsisButton_Down))
        {

        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + 124f, inRectY, 470f, inRectHeight);
        Widgets.LabelFit(reusedRect, clique.ActiveDesc);
        TooltipHandler.TipRegion(reusedRect, () => clique.ActiveDesc, uniqueId: 55540347);

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRect.xMax - 110f, inRectY, 110f, inRectHeight);
        Widgets.Label(reusedRect, clique.Potency.ToStringPercent().Colorize(clique.Potency < 0f ? ColorLibrary.RedReadable : Color.green));

        OARO_WindowUtility.ResetText();
    }

    private void DrawInactiveClique_Normal(Rect inRect, QuestClique clique)
    {
        Rect topRect = inRect;
        float topRectX = inRect.xMin;
        float topRectY = inRect.yMin;
        topRect.height = 40f;
        float topRectHeight = topRect.height;
        GUI.DrawTexture(topRect, IconLibrary.DarkTex);

        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, topRectX + 12f, 24f, 24f);
        GUI.DrawTexture(reusedRect, IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(topRectX + 45f, topRectY, 300f - 45f, topRectHeight);
        Widgets.LabelFit(reusedRect, clique.Name);

        reusedRect = new(topRectX + 360f, topRectY, 220f, topRectHeight);
        Widgets.LabelFit(reusedRect, clique.InactiveDesc);

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

        reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, topRect.xMax - 200f, 135f, 24f);
        Widgets.FillableBar(reusedRect, clique.Willingness, BaseContent.GreyTex);

        reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, reusedRect.xMax + 8f, 60f, topRectHeight);
        Widgets.Label(reusedRect, clique.Willingness.ToStringPercent());

        Rect bottom = new(inRect.xMin, topRect.yMax, inRect.width, 24f);
        reusedRect = new(bottom.xMax - 92f, bottom.yMin, 92f, bottom.height);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_CliqueWin_Active".Translate(),
            acceptance: clique.CanActiveNow(directly: false, mapRecommendationCount: MapRecommendationCount.Value, resultOnly: false),
            baseTex: smallButton,
            downTex: smallButton_Down,
            doMouseoverSound: true))
        {
            clique.TryActive(directly: false, map: Map);
        }

        reusedRect = new(reusedRect.xMin - 92f, bottom.yMin, 92f, bottom.height);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_CliqueWin_Bribe".Translate(),
            acceptance: clique.CanBribable(Map, resultOnly: false),
            baseTex: smallButton,
            downTex: smallButton_Down,
            doMouseoverSound: true,
            tooltip: clique.IsBribable ? "OARO_CliqueWin_BribeTip".Translate(clique.BriberyCost.Named(KeyLibrary_FormatArgName.Count)) : null))
        {
            Dialog_NodeTree nodeTree = OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
                text: "OARO_Clique_BriberyConfirm".Translate(clique.Name.Named(KeyLibrary_FormatArgName.CliqueName), clique.BriberyCost.Named(KeyLibrary_FormatArgName.Count)),
                acceptAction: () => clique.Bribery(map: Map));

            Find.WindowStack.Add(nodeTree);
        }

        reusedRect = new(reusedRect.xMin - 92f, bottom.yMin, 92f, bottom.height);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_CliqueWin_Communicate".Translate(),
            acceptance: clique.CanCommunicable(resultOnly: false),
            baseTex: smallButton,
            downTex: smallButton_Down,
            doMouseoverSound: true))
        {
            clique.Communicate(branch: CliquesManager.Branch, map: Map);
        }
        OARO_WindowUtility.ResetText();
    }

    private void DrawInactiveClique_Branch(Rect inRect, QuestClique clique)
    {
        Branch branch = clique.RelatedBranch;

        Rect topRect = inRect;
        float topRectX = inRect.xMin;
        float topRectY = inRect.yMin;
        topRect.height = 40f;
        float topRectHeight = topRect.height;

        GUI.DrawTexture(topRect, IconLibrary.DarkTex);

        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, topRectX + 12f, 24f, 24f);
        OARO_WindowUtility.DrawBranchIcon(reusedRect, clique.RelatedBranch, expand: false);

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(topRectX + 45f, topRectY, 180f - 45f, topRectHeight);
        Widgets.LabelFit(reusedRect, clique.Name);

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

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(topRectX + 305f, topRectY, 90f, topRectHeight);
        Widgets.Label(reusedRect, branch.CurWorkStateDesc);

        reusedRect = new(topRectX + 450f, topRectY, 90f, topRectHeight);
        //

        reusedRect = new(topRectX + 600f, topRectY, 60f, topRectHeight);
        Widgets.Label(reusedRect, clique.Potency.ToStringPercent());

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            reusedRect = new(topRect.xMax - 138f, topRectY, 138f, topRectHeight);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "OARO_CliqueWin_Active".Translate(),
                acceptance: clique.CanActiveNow(directly: false, mapRecommendationCount: MapRecommendationCount.Value, resultOnly: false),
                baseTex: bigButton,
                downTex: bigButton_Down,
                doMouseoverSound: true,
                tooltip: "OARO_CliqueWin_ActiveFriendlyCliqueTip".Translate()))
            {
                clique.TryActive(directly: false, map: Map);
                MapRecommendationCount.MarkDirty();
            }
        }
        else
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, topRect.xMax - 200f, 135f, 24f);
            Widgets.FillableBar(reusedRect, clique.Willingness, BaseContent.GreyTex);

            reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, reusedRect.xMax + 8f, 60f, topRectHeight);
            Widgets.Label(reusedRect, clique.Willingness.ToStringPercent());
            Rect bottom = new(inRect.xMin, topRect.yMax, inRect.width, 24f);
            reusedRect = new(bottom.xMax - 92f, bottom.yMin, 92f, bottom.height);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "OARO_CliqueWin_Active".Translate(),
                acceptance: clique.CanActiveNow(directly: false, mapRecommendationCount: MapRecommendationCount.Value, resultOnly: false),
                baseTex: smallButton,
                downTex: smallButton_Down,
                doMouseoverSound: true))
            {
                clique.TryActive(directly: false, map: Map);
            }

            reusedRect = new(reusedRect.xMin - 92f, bottom.yMin, 92f, bottom.height);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "OARO_CliqueWin_Communicate".Translate(),
                acceptance: clique.CanCommunicable(resultOnly: false),
                baseTex: smallButton,
                downTex: smallButton_Down,
                doMouseoverSound: true))
            {
                clique.Communicate(branch: CliquesManager.Branch, map: Map);
            }
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawCornerRect(Rect inRect)
    {
        Rect bottonRect = inRect;
        bottonRect.height = 24f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = bottonRect;
        reusedRect.width = 92f;
        DrawBotton(reusedRect, CornerType.Clique);

        reusedRect.xMax += 92f;
        reusedRect.xMin += 92f;
        DrawBotton(reusedRect, CornerType.Success);

        reusedRect.xMax += 92f;
        reusedRect.xMin += 92f;
        DrawBotton(reusedRect, CornerType.Fail);

        Rect mainRect = inRect;
        mainRect.yMin += 24f;

        GUI.DrawTexture(mainRect, cornerBackground);

        Text.Anchor = TextAnchor.UpperLeft;
        Rect textRect = mainRect.ContractedBy(10f);

        void DrawBotton(Rect rect, CornerType cornerType)
        {
            if (cornerType == CornerShowType)
            {
                GUI.DrawTexture(rect, smallButton_Down);
                Widgets.Label(rect, $"OARO_CliqueWin_Corner_{cornerType}".Translate());
            }
            else if (OARO_WindowUtility.TextButtonImage(rect, $"OARO_CliqueWin_Corner_{cornerType}".Translate(), smallButton, smallButton_Down, doMouseoverSound: true))
            {
                CornerShowType = cornerType;
            }
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_MainBackground");
    private static readonly Texture2D cliqueRectBackground = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_CliqueRectBackground");
    private static readonly Texture2D cornerBackground = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_CornerBackground");

    private static readonly Texture2D potencyLace = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_PotencyLace");

    private static readonly Texture2D friendlyHeart = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_FriendlyHeart");
    private static readonly Texture2D nearClock = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_NearClock");

    private static readonly Texture2D smallButton = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_SmallButton");
    private static readonly Texture2D smallButton_Down = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_SmallButton_Down");

    private static readonly Texture2D bigButton = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_BigButton");
    private static readonly Texture2D bigButton_Down = ContentFinder<Texture2D>.Get("UI/QuestClique/OARO_BigButton_Down");
}
