using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_BranchDemand : OrderWindowBase
{
    private enum TabType
    {
        All,
        Friendly,
        Near,

        Accepted
    }

    public override Vector2 InitialSize => new(1360f, 930f);

    private RatkinOrder RatkinOrder { get; }
    private Map Map { get; }
    private LazyMutable<int> MapRecommendationCount { get; }

    private TabType CurTab { get; set; } = TabType.All;
    private List<TabRecord> Tabs { get; } = new(3);
    private List<TabRecord> AcceptedTab { get; } = new(1);

    private List<BranchDemandEntryDrawer> BranchWithDemandsCache { get; } = [];
    private List<BranchDemandEntryDrawer> TabDemandEntryCaches { get; }

    private Branch SelBranch { get; set; }
    private bool SelCritical { get; set; }
    private BranchDemand SelDemand { get; set; }
    private string SelFullDesc { get; set; } = string.Empty;
    private AcceptanceReport SelAcceptance { get; set; }

    private LazyMutable<QuestPart_CliquesManager> SelDemandCliqueManager { get; }

    public Action PostCloseAction { get; set; }

    private Vector2 scrollPosition_Demands;
    private Vector2 scrollPosition_DemandDesc;

    private QuestPart_CliquesManager RefreshCliquesManager()
    {
        if (SelDemand is null || !SelDemand.IsOngoing || SelDemand is not BranchDemand_Critical)
        {
            return null;
        }
        SelDemand.RelatedQuest.TryGetCliquesManager(addPartIfMiss: false, out QuestPart_CliquesManager cliqueManager);
        return cliqueManager;
    }

    public Window_BranchDemand(RatkinOrder ratkinOrder, Map map) : base()
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        Map = map ?? throw new ArgumentNullException(nameof(map));

        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationCount(Map));
        SelDemandCliqueManager = new(refreshFunc: RefreshCliquesManager);

        IReadOnlyList<Branch> allBranches = ratkinOrder.BranchManager.AllBranches;
        foreach (Branch branch in allBranches)
        {
            if (branch.DemandHandler.HasDemand)
            {
                BranchWithDemandsCache.Add(new BranchDemandEntryDrawer(branch, map));
            }
        }

        TabDemandEntryCaches = new(BranchWithDemandsCache.Count);
        GetCurTapBranchSummary();
    }

    private void SwitchTapBranchSummary(TabType tabType)
    {
        if (CurTab == tabType)
        {
            return;
        }
        CurTab = tabType;
        GetCurTapBranchSummary();
    }

    private void GetCurTapBranchSummary()
    {
        //Deselect();
        TabDemandEntryCaches.Clear();
        if (BranchWithDemandsCache.Count <= 0)
        {
            return;
        }

        switch (CurTab)
        {
            case TabType.All:
                {
                    TabDemandEntryCaches.AddRange(BranchWithDemandsCache);
                    break;
                }
            case TabType.Near:
                {
                    for (int i = 0; i < BranchWithDemandsCache.Count; i++)
                    {
                        if (BranchWithDemandsCache[i].SummaryUICache.IsInAffectedRange)
                        {
                            TabDemandEntryCaches.Add(BranchWithDemandsCache[i]);
                        }
                    }
                    break;
                }
            case TabType.Friendly:
                {
                    for (int i = 0; i < BranchWithDemandsCache.Count; i++)
                    {
                        if (BranchWithDemandsCache[i].Branch.IsBranchOfType(BranchType.Friendly))
                        {
                            TabDemandEntryCaches.Add(BranchWithDemandsCache[i]);
                        }
                    }
                    break;
                }
            case TabType.Accepted:
                {
                    HashSet<Branch> branches = [];
                    IReadOnlyList<AcceptedBranchDemand> acceptedRecords = AcceptedBranchDemandHandler.Instance.Records;
                    for (int i = 0; i < acceptedRecords.Count; i++)
                    {
                        if (branches.Add(acceptedRecords[i].Branch))
                        {
                            TabDemandEntryCaches.Add(new(acceptedRecords[i].Branch, Map));
                        }
                    }
                    break;
                }
        }
    }

    public override void PostClose()
    {
        base.PostClose();

        BranchWithDemandsCache.Clear();
        TabDemandEntryCaches.Clear();
        Tabs.Clear();

        try
        {
            PostCloseAction?.Invoke();
        }
        finally
        {
            PostCloseAction = null;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1339f, 908f);
        GUI.DrawTexture(mainRect, mainBackground);
        Rect mainInnerRect = mainRect.ContractedBy(3f);
        if (OARO_WindowUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }
        if (OARO_WindowUtility.DrawBackArrow_Corner(mainInnerRect))
        {
            Window_RatkinOrder ratkinOrderWin = new(Map);
            Find.WindowStack.Add(ratkinOrderWin);
            Close();
            return;
        }

        Rect demandListRect = new(mainInnerRect.x + 60f, mainInnerRect.y + 210f, 801f, 624f);

        Rect reusedRect = demandListRect;
        reusedRect.width = 350f;
        Tabs.Clear();
        Tabs.Add(new TabRecord("OARO_BranchSquad_All".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.All);
        }, CurTab == TabType.All));
        Tabs.Add(new TabRecord("OARO_BranchSquad_Near".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.Near);
        }, CurTab == TabType.Near));
        Tabs.Add(new TabRecord("OARO_BranchSquad_Friendly".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.Friendly);
        }, CurTab == TabType.Friendly));
        TabDrawer.DrawTabs(reusedRect, Tabs);

        reusedRect = demandListRect;
        reusedRect.xMin = reusedRect.xMax - 140f;
        AcceptedTab.Clear();
        AcceptedTab.Add(new TabRecord("OARO_HasAccepted".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.Accepted);
        }, CurTab == TabType.Accepted));
        TabDrawer.DrawTabs(reusedRect, AcceptedTab, maxTabWidth: 140f);

        reusedRect = new(demandListRect.x, demandListRect.y - (32f + 48f), 450f, 48f);
        DrawLeftText(reusedRect);

        DrawDemandListRect(demandListRect);

        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, demandListRect.xMax + 18f, 2f, 716f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);

        Rect rightRect = new(reusedRect.xMax + 18f, mainInnerRect.y + 167f, 379f, 667f);
        DrawRightRect(rightRect);

        OARO_WindowUtility.ResetText();
    }

    private void DrawLeftText(Rect inRect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        Rect reusedRect = inRect;
        reusedRect.width /= 2;
        reusedRect.height /= 2;
        Widgets.Label(reusedRect, "OARO_DemandWin_OrderEsteem".Translate() + ": " + RatkinOrder.Esteem.ToString());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = inRect.xMax;

        Rect reusedRectII = reusedRect;
        reusedRectII.width /= 2;
        string letterLabel = "OARO_RecommendationLetter".Translate();
        reusedRectII = new(reusedRect.x, reusedRect.y, Text.CalcSize(letterLabel).x, reusedRect.height);
        Widgets.Label(reusedRectII, "OARO_RecommendationLetter".Translate());

        reusedRectII = Rect.MinMaxRect(reusedRectII.xMax + 6f, reusedRect.yMin, reusedRect.xMax, reusedRect.yMax);
        OARO_WindowUtility.DrawRecommendationInfo(reusedRectII, MapRecommendationCount.Value);

        reusedRect = inRect;
        reusedRect.width /= 2;
        reusedRect.yMin += reusedRect.height / 2;
        Widgets.Label(reusedRect, "OARO_NormalDemandFulfillCount".Translate() + ": " + RatkinOrder.BranchManager.NormalDemandFulfillCount);

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = inRect.xMax;
        Widgets.Label(reusedRect, "OARO_CriticalDemandFulfillCount".Translate() + ": " + RatkinOrder.BranchManager.CriticalDemandFulfillCount);

        OARO_WindowUtility.ResetText();
    }

    private void DrawDemandListRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftMainBackground);
        Rect outRect = inRect.ContractedBy(2f);

        if (TabDemandEntryCaches.Count > 0)
        {
            Rect viewRect = outRect;
            viewRect.width = BranchDemandEntryDrawer.RectWidth;

            float entryX = viewRect.x;
            float entryY = viewRect.y;
            float entryHeight = BranchDemandEntryDrawer.RectHeight;
            viewRect.height = TabDemandEntryCaches.Count * entryHeight;

            Vector2 entryPosition;
            Widgets.BeginScrollView(outRect, ref scrollPosition_Demands, viewRect);
            for (int i = 0; i < TabDemandEntryCaches.Count; i++)
            {
                entryPosition = new(entryX, entryY);
                entryY += entryHeight - 2f;

                BranchDemandEntryDrawer.ButtonResult buttonResult = TabDemandEntryCaches[i].DrawDemandEntry(entryPosition);
                if (buttonResult == BranchDemandEntryDrawer.ButtonResult.CheckNormal)
                {
                    SelctDemand(TabDemandEntryCaches[i].Branch, isCritical: false);
                }
                else if (buttonResult == BranchDemandEntryDrawer.ButtonResult.CheckCritical)
                {
                    SelctDemand(TabDemandEntryCaches[i].Branch, isCritical: true);
                }
            }
            Widgets.EndScrollView();
        }
        else
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, "OARO_DemandWin_NoDemandNow".Translate().Colorize(Color.gray));
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawRightRect(Rect inRect)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin;
        float inRectWidth = inRect.width;

        GUI.DrawTexture(inRect, rightMainBackground);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Rect reusedRect = new(inRectX, inRectY - 32f, inRectWidth, 32f);
        Widgets.Label(reusedRect, "OARO_DemandWin_AcceptedDemand".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, $"{AcceptedBranchDemandHandler.Instance.AcceptanceCount}/{RatkinOrderSettings.MaxConcurrentAcceptedDemand}");
        if (SelDemand is null)
        {
            return;
        }

        reusedRect = new(inRectX + 18f, inRectY + 12f, inRectWidth - 36f, 32f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(reusedRect, SelDemand.Def.label);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.LowerRight;
        string rightUpText = SelDemand.HasAccepted ? "OARO_HasAccepted".Translate().Colorize(Color.green)
                                                   : "OARO_ExpiredDate".Translate() + ": " + GenDate.SeasonDateStringAt(GenTicks.TicksAbs + SelDemand.TicksToExpire, Find.WorldGrid.LongLatOf(Map.Tile)).Colorize(Color.cyan);
        Widgets.Label(reusedRect, rightUpText);

        reusedRect = new(inRect.xMax - (18f + 50f), reusedRect.yMax + 4f, 50f, 50f);
        switch (SelDemand.DemandTypeValue)
        {
            case BranchDemand.DemandType.Urgency:
                {
                    GUI.DrawTexture(reusedRect, urgencyDemandIcon, ScaleMode.ScaleToFit);
                    break;
                }
            case BranchDemand.DemandType.Supplementary:
                {
                    GUI.DrawTexture(reusedRect, supplementaryDemandIcon, ScaleMode.ScaleToFit);
                    break;
                }
            default: break;
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Rect textRect = new(inRectX + 18f, inRectY + 100f, inRectWidth - 36f, 245f);
        Widgets.LabelScrollable(textRect, SelFullDesc, ref scrollPosition_DemandDesc);

        if (!SelDemand.HasAccepted)
        {
            reusedRect = new(inRectX, inRect.yMax - (4f + 40f), inRectWidth, 40f);
            DrawDemandBranchInfo(reusedRect);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMin - 55f, 105f, 55f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "Accept".Translate(),
                acceptance: SelAcceptance,
                baseTex: acceptButton,
                downTex: acceptButton_Down,
                doMouseoverSound: true))
            {
                AcceptanceReport curAcceptance = BranchDemandUtility.CanAcceptDemand(SelBranch, SelCritical, resultOnly: false);
                if (curAcceptance)
                {
                    SelBranch.DemandHandler.TryAcceptDemand(SelCritical);
                }
                else
                {
                    Messages.Message("OARO_CanNotAcceptBrancDemand".Translate(curAcceptance.Reason), MessageTypeDefOf.RejectInput);
                }
                SelctDemand(SelBranch, SelCritical);
            }
        }
        else if (SelCritical && (SelDemand is BranchDemand_Critical))
        {
            reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, textRect.yMax + 16f, 105f, 55f);
            /////////////////////////////////////////////////////////////////////////////////////

            reusedRect = new(inRectX, reusedRect.yMax + 4f, inRectWidth, 40f);
            DrawDemandBranchInfo(reusedRect);

            reusedRect = Rect.MinMaxRect(inRectX, reusedRect.yMax + 16f, inRect.xMax, inRect.yMax);
            DrawRightRect_AcceptedCritical(reusedRect);
        }

        OARO_WindowUtility.ResetText();
    }

    /// <summary>
    /// 宽40f
    /// </summary>
    private void DrawDemandBranchInfo(Rect inRect)
    {
        Rect reusedRect = new(inRect.x + 40f, inRect.y, 40f, 40f);
        OARO_WindowUtility.DrawBranchIcon(reusedRect, SelBranch, expand: false);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.LowerRight;
        string linkText = "OARO_SuperLink".Translate() + ": " + SelBranch.Name;
        Vector2 linkTextSize = Text.CalcSize(linkText);
        reusedRect = new(inRect.xMax - (40f + linkTextSize.x), reusedRect.y, linkTextSize.x, 20f);
        Color color;
        if (Mouse.IsOver(reusedRect))
        {
            color = ColorLibrary.LightBlue;
        }
        else
        {
            color = SelBranch.Color;
        }
        Widgets.Label(reusedRect, linkText.Colorize(color));

        if (Widgets.ButtonInvisible(reusedRect))
        {
            Window_Branch branchWinow = new(SelBranch, caravan: null, Map);
            Find.WindowStack.Add(branchWinow);
        }

        reusedRect = new(inRect.xMax - (40f + 96f), reusedRect.yMax, 96f, 20f);
        if (SelBranch.IsBranchOfType(BranchType.Friendly))
        {
            Widgets.Label(reusedRect, "OARO_Friendly".Translate().Colorize(Color.green));
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_Strange".Translate());
        }
    }

    private void DrawRightRect_AcceptedCritical(Rect inRect)
    {
        Rect reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.yMax - (137f + 10f), 356f, 137f);
        GUI.DrawTexture(reusedRect, criticalDemandPotencyLace);
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.yMax - (163f + 2f), 354f, 2f);
        GUI.DrawTexture(reusedRect, rightCuttingLine);

        QuestPart_CliquesManager cliqueManager = SelDemandCliqueManager.Value;
        Rect cliqueRect = new(inRect.x + 50f, inRect.yMax - (32f + 114f), 146f, 90f);

        int column = 0;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        foreach (QuestClique clique in cliqueManager.AllCliques.Values)
        {
            Rect entryRect = new(cliqueRect.x, cliqueRect.y + column * 30f, 27f, 30f);
            //

            entryRect.xMax = cliqueRect.xMax;
            entryRect.xMin += 27f;
            Widgets.Label(entryRect, clique.Name);
            if ((++column) >= 3)
            {
                break;
            }
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_WindowUtility.CenterRectOnX(cliqueRect, cliqueRect.yMax + 4f, 92f, 25f);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: "OARO_DemandWin_CliqueDetail".Translate(),
            baseTex: checkButton,
            downTex: checkButton_Down,
            doMouseoverSound: true))
        {
            Window_QuestClique cliqueWin = new(SelDemand, Map);
            Find.WindowStack.Add(cliqueWin);
        }

        Rect potencyRect = new(inRect.xMax - (42f + 125f), inRect.yMax - (30f + 120f), 125f, 120f);
        GUI.DrawTexture(potencyRect, criticalDemandPotencyFrame);
        reusedRect = OARO_WindowUtility.CenterRectOnX(potencyRect, potencyRect.y + 12f, potencyRect.width, 22f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_DemandWin_QuestPotency".Translate());

        Text.Font = GameFont.Medium;
        reusedRect = OARO_WindowUtility.CenterRectOnX(potencyRect, reusedRect.yMax + 24f, potencyRect.width, 32f);
        Widgets.Label(reusedRect, cliqueManager.TotalPotency.Value.ToStringPercentSigned());

        OARO_WindowUtility.ResetText();
    }

    private void SelctDemand(Branch branch, bool isCritical)
    {
        Deselect();
        SelBranch = branch;
        SelCritical = isCritical;
        SelDemand = branch.DemandHandler.GetDemand(isCritical);
        if (SelDemand is null)
        {
            SelFullDesc = string.Empty;
            SelAcceptance = false;
        }
        else
        {
            SelAcceptance = BranchDemandUtility.CanAcceptDemand(branch, isCritical, resultOnly: false);
            SelFullDesc = SelDemand.GetFullDesc();
        }
    }

    private void Deselect()
    {
        SelBranch = null;
        SelCritical = false;
        SelDemand = null;
        SelFullDesc = string.Empty;
        SelDemandCliqueManager.Reset();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_MainBackground");
    private static readonly Texture2D leftMainBackground = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_LeftMainBackground");

    private static readonly Texture2D demandEntryRect = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_DemandEntryRect");
    private static readonly Texture2D checkButton = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_CheckButton");
    private static readonly Texture2D checkButton_Down = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_CheckButton_Down");

    private static readonly Texture2D criticalDemandTagBackground = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_CriticalDemandTagBackground");

    private static readonly Texture2D verticalCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_VerticalCuttingLine");

    private static readonly Texture2D rightMainBackground = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_RightMainBackground");
    private static readonly Texture2D acceptButton = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_AcceptButton");
    private static readonly Texture2D acceptButton_Down = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_AcceptButton_Down");
    private static readonly Texture2D rightCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_RightCuttingLine");
    private static readonly Texture2D criticalDemandPotencyLace = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_CriticalDemandPotencyLace");
    private static readonly Texture2D criticalDemandPotencyFrame = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_CriticalDemandPotencyFrame");

    private static readonly Texture2D supplementaryDemandIcon = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_SupplementaryDemandIcon");
    private static readonly Texture2D urgencyDemandIcon = ContentFinder<Texture2D>.Get("UI/BranchDemandWin/OARO_UrgencyDemandIcon");

    private class BranchDemandEntryDrawer
    {
        public enum ButtonResult
        {
            None,
            CheckNormal,
            CheckCritical
        }

        public const float RectWidth = 781f;
        public const float RectHeight = 264f;

        public Branch Branch { get; }
        private Map Map { get; }
        public BranchSummaryUICache SummaryUICache { get; }

        private Vector2 scrollPosition_Medals;
        private Vector2 scrollPosition_Tags;
        private ButtonResult CurButtonResult { get; set; }

        public BranchDemandEntryDrawer(Branch branch, Map map)
        {
            Branch = branch;
            Map = map;
            SummaryUICache = new(branch, map);
        }

        public ButtonResult DrawDemandEntry(Vector2 position)
        {
            CurButtonResult = ButtonResult.None;
            Rect inRect = new(position.x, position.y, 781f, 264f);
            GUI.DrawTexture(inRect, demandEntryRect);
            Rect innerRect = inRect.ContractedBy(2f);
            Rect reusedRect = new(inRect.x, inRect.y + 2f, 5f, innerRect.height);
            if (SummaryUICache.Branch.HonorDef is not null)
            {
                GUI.DrawTexture(reusedRect, Branch.HonorDef.HonorColorTex);
            }
            else
            {
                GUI.DrawTexture(reusedRect, BaseContent.WhiteTex);
            }
            innerRect.xMin = reusedRect.xMax;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(innerRect.x, innerRect.y, 142f, 28f);
            Widgets.Label(reusedRect, Branch.Name);

            reusedRect.yMin = reusedRect.yMax;
            reusedRect.yMax += 28f;
            if (Branch.IsBranchOfType(BranchType.Friendly))
            {
                Widgets.Label(reusedRect, "OARO_Friendly".Translate().Colorize(Color.green));
            }
            else
            {
                Widgets.Label(reusedRect, "OARO_Strange".Translate());
            }

            reusedRect.yMin = reusedRect.yMax;
            reusedRect.yMax += 28f;
            Widgets.Label(reusedRect, $"{SummaryUICache.AllCrewCount}/{SummaryUICache.CrewCeiling}");

            reusedRect = new(reusedRect.xMax + 2f, innerRect.y, 128f, 32f);
            Text.Font = GameFont.Medium;
            Widgets.Label(reusedRect, "OARO_DemandWin_BranchSiteDistance".Translate());

            reusedRect = new(reusedRect.x, reusedRect.yMax + 2f, reusedRect.width, 51f);
            Rect reusedRectII = reusedRect;
            reusedRectII.yMax = reusedRectII.yMin + reusedRect.height * 0.6f;
            Widgets.Label(reusedRectII, SummaryUICache.AffectedRange.ToString("F0").Colorize(SummaryUICache.IsInAffectedRange ? Color.green : Color.white));

            reusedRectII.yMin = reusedRectII.yMax;
            reusedRectII.yMax = reusedRect.yMax;
            Text.Font = GameFont.Small;
            if (SummaryUICache.IsInAffectedRange)
            {
                Widgets.Label(reusedRectII, "OARO_DemandWin_InAffectedRange".Translate().Colorize(Color.green));
            }
            else
            {
                Widgets.Label(reusedRectII, "OARO_DemandWin_OutOfAffectedRange".Translate());
            }

            reusedRect = new(reusedRect.xMax + 2f, innerRect.y, 150f, 86f);
            /*
            reusedRectII = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax - 73f, 175f, 73f);
            GUI.DrawTexture(reusedRectII, potencyLace, ScaleMode.ScaleToFit);
            */

            reusedRectII = reusedRect;
            reusedRectII.height /= 2;
            Text.Font = GameFont.Medium;
            Widgets.Label(reusedRectII, "OARO_DemandWin_BranchPotency".Translate());

            reusedRectII.yMin = reusedRectII.yMax;
            reusedRectII.yMax = reusedRect.yMax;
            Widgets.Label(reusedRectII, Branch.Potency.ToString("F0"));

            Rect normamDemandRect = new(innerRect.xMax - 352f, innerRect.y, 352f, 86f);
            DrawNormalDemand(normamDemandRect, Branch.DemandHandler.NormalDemand);

            Rect criticalDemandRect = Rect.MinMaxRect(innerRect.x, innerRect.y + 88f, innerRect.xMax, innerRect.yMax);
            DrawCriticalDemand(criticalDemandRect, Branch.DemandHandler.CriticalDemand);

            return CurButtonResult;
        }

        private void DrawNormalDemand(Rect inRect, BranchDemand demand)
        {
            if (demand is null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                return;
            }

            Rect labelRect = new(inRect.x + 24f, inRect.y + 16f, 128f, 28f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.LabelEllipses(labelRect, demand.Def.label);
            Text.Font = GameFont.Small;

            Rect reusedRect = OARO_WindowUtility.CenterRectOnX(labelRect, labelRect.yMax + 4f, 92f, 25f);
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OAFrame_LookOver".Translate(), checkButton, checkButton_Down))
            {
                CurButtonResult = ButtonResult.CheckNormal;
            }

            reusedRect = new(labelRect.xMax, labelRect.y, 72f, labelRect.height);

            Widgets.Label(reusedRect, $"OARO_BranchDemandType_{demand.DemandTypeValue}".Translate());

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(inRect.xMax - (10f + 128f), inRect.y + 4f, 128f, 20f);
            string rightUpText = demand.HasAccepted ? "OARO_HasAccepted".Translate().Colorize(Color.green)
                                                    : demand.TicksToExpire.ToStringTicksToPeriod().Colorize(Color.cyan);
            Widgets.Label(reusedRect, rightUpText);

            reusedRect = new(inRect.xMax - (10f + 54f), inRect.yMax - (4f + 54f), 54f, 54f);
            switch (demand.DemandTypeValue)
            {
                case BranchDemand.DemandType.Urgency:
                    {
                        GUI.DrawTexture(reusedRect, urgencyDemandIcon, ScaleMode.ScaleToFit);
                        break;
                    }
                case BranchDemand.DemandType.Supplementary:
                    {
                        GUI.DrawTexture(reusedRect, supplementaryDemandIcon, ScaleMode.ScaleToFit);
                        break;
                    }
                default: break;
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawCriticalDemand(Rect inRect, BranchDemand_Critical demand)
        {
            if (demand is null)
            {
                GUI.DrawTexture(inRect, IconLibrary.ShadeTexture);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(inRect, "OARO_DemandWin_NoDemandOfTypeNow".Translate());
                return;
            }

            Rect infoRect = inRect;
            infoRect.width = 470f;
            if (demand.Def.BackgroundTexture is not null)
            {
                GUI.DrawTexture(infoRect, demand.Def.BackgroundTexture, ScaleMode.ScaleToFit);
            }

            Rect reusedRect = new(infoRect.x + 48f, infoRect.y + 20f, 128f, 28f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.LabelEllipses(reusedRect, demand.Def.label);

            reusedRect = new(reusedRect.xMax, infoRect.y + 20f, 72f, 28f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(reusedRect, $"OARO_BranchDemandType_{demand.DemandTypeValue}".Translate());

            Rect lookOverRect = new(infoRect.x + 48f, reusedRect.yMax + 20f, 92f, 25f);
            if (OARO_WindowUtility.TextButtonImage(lookOverRect, "OAFrame_LookOver".Translate(), checkButton, checkButton_Down))
            {
                CurButtonResult = ButtonResult.CheckCritical;
            }

            reusedRect = new(infoRect.x, infoRect.y + 6f, infoRect.width - 12f, 20f);
            Text.Anchor = TextAnchor.LowerRight;
            if (demand.HasAccepted)
            {
                Widgets.Label(reusedRect, "OARO_HasAccepted".Translate().Colorize(Color.green));
                reusedRect = new(lookOverRect.xMax + 12f, lookOverRect.y, 92f, 25f);
                Text.Anchor = TextAnchor.MiddleCenter;
                if (OARO_WindowUtility.TextButtonImage(
                    butRect: reusedRect,
                    label: "OARO_DemandWin_CliqueDetail".Translate(),
                    baseTex: checkButton,
                    downTex: checkButton_Down))
                {
                    Window_QuestClique cliqueWin = new(demand, Map);
                    Find.WindowStack.Add(cliqueWin);
                }
            }
            else
            {
                Widgets.Label(reusedRect, demand.TicksToExpire.ToStringTicksToPeriod().Colorize(Color.cyan));
            }

            Rect medalOutRect = new(infoRect.xMax - (10f + 168f), infoRect.yMax - (6f + 50f), 168f, 50f);

            reusedRect = new(medalOutRect.x, medalOutRect.yMin - 22f, medalOutRect.width, 22f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, "OARO_DemandWin_PotentialBranchMedalType".Translate());

            Rect medalViewRect = medalOutRect;
            float entryX = medalViewRect.x;
            float entryY = medalViewRect.y;
            float entryWidth = 56f;
            float entryHeight = 36f;
            medalViewRect.width = entryWidth * demand.PotentialMedals.Count;

            Widgets.BeginScrollView(medalOutRect, ref scrollPosition_Medals, medalViewRect, showScrollbars: false);
            foreach (BranchMedalDef medalDef in demand.PotentialMedals)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryX += entryWidth;
                GUI.DrawTexture(entryRect.ContractedBy(4f), medalDef.iconTexture.Texture, ScaleMode.ScaleToFit);
            }
            Widgets.EndScrollView();

            Rect tagRect = Rect.MinMaxRect(reusedRect.xMax, inRect.yMin, inRect.xMax, inRect.yMax);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerCenter;
            reusedRect = new(tagRect.x + 8f, tagRect.y + 4f, 96f, 20f);
            Widgets.Label(reusedRect, "OARO_DemandWin_DemandTags".Translate());

            Rect tagOutRect = Rect.MinMaxRect(tagRect.xMin + 40f, tagRect.yMin + 30f, tagRect.xMax - 40f, tagRect.yMax - 30f);

            Rect tagViewRect = tagOutRect;
            entryX = tagViewRect.x;
            entryY = tagViewRect.y;
            entryWidth = (tagOutRect.width - 12f) / 2 - 0.01f;
            entryHeight = (tagOutRect.height - 8f) / 2 - 0.01f;
            float entryXInterval = 12f;
            float entryYInterval = 8f;
            int column = 0;
            IReadOnlyList<QuestEffectTag> questEffectTags = demand.QuestEffectTags;
            tagViewRect.height = Mathf.Ceil(questEffectTags.Count / 2) * (entryHeight + entryYInterval);
            Widgets.BeginScrollView(tagOutRect, ref scrollPosition_Tags, tagViewRect, showScrollbars: false);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int i = 0; i < questEffectTags.Count; i++)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                if ((++column) >= 2)
                {
                    entryX = tagViewRect.x;
                    entryY += entryHeight + entryYInterval;
                    column = 0;
                }
                else
                {
                    entryX += entryWidth + entryXInterval;
                }
                GUI.DrawTexture(entryRect, criticalDemandTagBackground, ScaleMode.ScaleToFit);
                Widgets.Label(entryRect, questEffectTags[i].Label);
            }
            Widgets.EndScrollView();

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }
}