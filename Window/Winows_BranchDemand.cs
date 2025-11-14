using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Winows_BranchDemand : MainTabWindow
{
    private enum TabType
    {
        All,
        Friendly,
        Near,

        Accepted
    }
    private RatkinOrder ratkinOrder;
    private Map map;
    private int mapRecommendationLetterCount;
    public override Vector2 InitialSize => new(1360f, 930f);
    public override Vector2 RequestedTabSize => new(1360f, 930f);
    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    private TabType curTab;
    private readonly List<TabRecord> tabs = new(3);
    private readonly List<TabRecord> acceptedTab = new(1);

    private readonly List<BranchDemandEntryDrawer> branchWithDemandsCache = [];
    private readonly List<BranchDemandEntryDrawer> tabDemandEntryCaches = [];

    private bool selCritical;
    private BranchDemand selNormalDemand;
    private BranchDemand_Critical selCriticalDemand;
    private QuestPart_CliquesManager selDemandCliqueManager;
    private QuestPart_CliquesManager SelDemandCliqueManager
    {
        get
        {
            if (selCriticalDemand is not null && selCriticalDemand.IsOngoing && selDemandCliqueManager is null)
            {
                QuestPart_CliquesManager.TryGetCliquesManager(selCriticalDemand.RelatedQuest, addPartIfMiss: false, out selDemandCliqueManager);
            }
            return selDemandCliqueManager;
        }
    }

    private Vector2 scrollPosition_Demands;

    public Winows_BranchDemand() { }
    public Winows_BranchDemand(RatkinOrder ratkinOrder, Map map) : base()
    {
        this.ratkinOrder = ratkinOrder;
        mapRecommendationLetterCount = RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map);
        IReadOnlyList<Branch> allBranches = ratkinOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            if (allBranches[i].DemandHandler.HasDemand)
            {
                branchWithDemandsCache.Add(new BranchDemandEntryDrawer(allBranches[i], map));
            }
        }
        curTab = TabType.All;
        GetCurTapBranchSummary();
    }

    public override void PreOpen()
    {
        base.PreOpen();
        ratkinOrder = RatkinOrderManager.AllRatkinOrders[0];
        map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
        mapRecommendationLetterCount = RecommendationUtility.CurRecommendationOfMap(ratkinOrder, map);
        IReadOnlyList<Branch> allBranches = ratkinOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            if (allBranches[i].DemandHandler.HasDemand)
            {
                branchWithDemandsCache.Add(new BranchDemandEntryDrawer(allBranches[i], map));
            }
        }
        curTab = TabType.All;
        GetCurTapBranchSummary();
    }

    private void SwitchTapBranchSummary(TabType tabType)
    {
        if (curTab == tabType)
        {
            return;
        }
        curTab = tabType;
        GetCurTapBranchSummary();
    }

    private void GetCurTapBranchSummary()
    {
        //Deselect();
        tabDemandEntryCaches.Clear();
        if (branchWithDemandsCache.Count > 0)
        {
            switch (curTab)
            {
                case TabType.All:
                    {
                        tabDemandEntryCaches.AddRange(branchWithDemandsCache);
                        break;
                    }
                case TabType.Near:
                    {
                        for (int i = 0; i < branchWithDemandsCache.Count; i++)
                        {
                            if (branchWithDemandsCache[i].SummaryUICache.IsInAffectedRange)
                            {
                                tabDemandEntryCaches.Add(branchWithDemandsCache[i]);
                            }
                        }
                        break;
                    }
                case TabType.Friendly:
                    {
                        for (int i = 0; i < branchWithDemandsCache.Count; i++)
                        {
                            if (branchWithDemandsCache[i].Branch.IsBranchOfType(BranchType.Friendly))
                            {
                                tabDemandEntryCaches.Add(branchWithDemandsCache[i]);
                            }
                        }
                        break;
                    }
                case TabType.Accepted:
                    {
                        HashSet<Branch> branches = [];
                        IReadOnlyList<AcceptedBranchDemand> acceptedRecords = AcceptedBranchDemandHandler.Records;
                        for (int i = 0; i < acceptedRecords.Count; i++)
                        {
                            if (branches.Add(acceptedRecords[i].Branch))
                            {
                                tabDemandEntryCaches.Add(new(acceptedRecords[i].Branch, map));
                            }
                        }
                        break;
                    }
            }

            if (tabDemandEntryCaches.Count > 0)
            {
                // SelectSquad(0);
            }
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        branchWithDemandsCache.Clear();
        tabDemandEntryCaches.Clear();
        tabs.Clear();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1339f, 908f);
        GUI.DrawTexture(mainRect, mainBackground);
        Rect mainInnerRect = mainRect.ContractedBy(3f);

        Rect demandListRect = new(mainInnerRect.x + 60f, mainInnerRect.y + 210f, 801f, 624f);

        Rect reusedRect = demandListRect;
        reusedRect.width = 350f;
        tabs.Clear();
        tabs.Add(new TabRecord("OARO_BranchSquad_All".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.All);
        }, curTab == TabType.All));
        tabs.Add(new TabRecord("OARO_BranchSquad_Near".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.Near);
        }, curTab == TabType.Near));
        tabs.Add(new TabRecord("OARO_BranchSquad_Friendly".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.Friendly);
        }, curTab == TabType.Friendly));
        TabDrawer.DrawTabs(reusedRect, tabs);

        reusedRect = demandListRect;
        reusedRect.xMin = reusedRect.xMax - 140f;
        acceptedTab.Clear();
        acceptedTab.Add(new TabRecord("OARO_BranchDemand_Accepted".Translate().CapitalizeFirst(), delegate
        {
            SwitchTapBranchSummary(TabType.Accepted);
        }, curTab == TabType.Accepted));
        TabDrawer.DrawTabs(reusedRect, acceptedTab, maxTabWidth: 140f);

        reusedRect = new(demandListRect.x + 8f, demandListRect.y - (32f + 48f), 450f, 48f);
        DrawLeftText(reusedRect);

        DrawDemandListRect(demandListRect);

        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, demandListRect.xMax + 18f, 2f, 716f);


        Rect rightRect = new(reusedRect.xMax + 18f, mainInnerRect.y + 156f, 379f, 667f);
        DrawRightRect(rightRect);
    }

    private void DrawLeftText(Rect inRect)
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;

        Rect reusedRect = inRect;
        reusedRect.width /= 2;
        reusedRect.height /= 2;
        Widgets.Label(reusedRect, "OARO_OrderEsteem" + ": " + ratkinOrder.Esteem.ToString());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = inRect.xMax;

        Rect reusedRectII = reusedRect;
        reusedRectII.width /= 3;
        Widgets.Label(reusedRectII, "OARO_RecommendationLetter".Translate());

        reusedRectII.xMin = reusedRectII.xMax;
        reusedRectII.xMax += reusedRect.width / 3;
        reusedRectII = OARO_WindowUtility.CenterRectOnX(reusedRectII, reusedRectII.y, reusedRectII.height, reusedRectII.height);
        //GUI.DrawTexture(reusedRectII,);

        reusedRectII = reusedRect;
        reusedRectII.xMin = reusedRectII.xMax - reusedRect.width / 3;
        Widgets.Label(reusedRectII, $"× {mapRecommendationLetterCount}");

        reusedRect = inRect;
        reusedRect.width /= 2;
        reusedRect.yMin += reusedRect.height / 2;
        Widgets.Label(reusedRect, "OARO_NormalDemandFulfillCount".Translate() + ": " + ratkinOrder.BranchManager.NormalDemandFulfillCount);

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = inRect.xMax;
        Widgets.Label(reusedRect, "OARO_CriticalDemandFulfillCount".Translate() + ": " + ratkinOrder.BranchManager.CriticalDemandFulfillCount);

        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawDemandListRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftMainBackground);
        Rect outRect = inRect.ContractedBy(2f);

        Rect viewRect = outRect;
        viewRect.width = BranchDemandEntryDrawer.RectWidth;

        float entryX = viewRect.x;
        float entryY = viewRect.y;
        float entryHeight = BranchDemandEntryDrawer.RectHeight;
        viewRect.height = tabDemandEntryCaches.Count * entryHeight;

        Vector2 entryPosition;
        Widgets.BeginScrollView(outRect, ref scrollPosition_Demands, viewRect);
        for (int i = 0; i < tabDemandEntryCaches.Count; i++)
        {
            entryPosition = new(entryX, entryY);
            entryY += (entryHeight - 2f);

            BranchDemandEntryDrawer.ButtonResult buttonResult = tabDemandEntryCaches[i].DrawDemandEntry(entryPosition);
            if (buttonResult == BranchDemandEntryDrawer.ButtonResult.CheckNormal)
            {
                selCritical = false;
                selNormalDemand = tabDemandEntryCaches[i].Branch.DemandHandler.GetDemand(selCritical);
            }
            else if (buttonResult == BranchDemandEntryDrawer.ButtonResult.CheckCritical)
            {
                selCritical = true;
                selCriticalDemand = (BranchDemand_Critical)tabDemandEntryCaches[i].Branch.DemandHandler.GetDemand(selCritical);
                selDemandCliqueManager = null;
            }
        }
        Widgets.EndScrollView();
    }

    private void DrawRightRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, rightMainBackground);
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_MainBackground");
    private static readonly Texture2D leftMainBackground = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_LeftMainBackground");

    private static readonly Texture2D demandEntryRect = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_DemandEntryRect");
    private static readonly Texture2D potencyLace = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_PotencyLace");
    private static readonly Texture2D checkButton = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_CheckButton");
    private static readonly Texture2D checkButton_Down = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_CheckButton_Down");

    private static readonly Texture2D criticalDemandInfoLace = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_CriticalDemandInfoLace");
    private static readonly Texture2D criticalDemandTagLace = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_CriticalDemandTagLace");
    private static readonly Texture2D criticalDemandCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_CriticalDemandCuttingLine");
    private static readonly Texture2D criticalDemandTagBackground = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_CriticalDemandTagBackground");

    private static readonly Texture2D rightMainBackground = ContentFinder<Texture2D>.Get("UI/BranchDemand/OARO_RightMainBackground");

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

        public Branch Branch;
        public BranchSummaryUICache SummaryUICache;

        private Vector2 scrollPosition_Medals;
        private Vector2 scrollPosition_Tags;
        private ButtonResult curButtonResult;

        public BranchDemandEntryDrawer(Branch branch, Map map)
        {
            Branch = branch;
            SummaryUICache = new(branch, map);
        }

        public ButtonResult DrawDemandEntry(Vector2 position)
        {
            curButtonResult = ButtonResult.None;
            Rect inRect = new(position.x, position.y, 781f, 264f);
            GUI.DrawTexture(inRect, demandEntryRect);
            Rect innerRect = inRect.ContractedBy(2f);
            Rect reusedRect = new(inRect.x, inRect.y + 2f, 5f, innerRect.height);
            if (SummaryUICache.Branch.HonorDef is not null)
            {
                GUI.DrawTexture(reusedRect, Branch.HonorDef.HonorBarTexture);
            }
            else
            {
                GUI.DrawTexture(reusedRect, IconLibrary.BarTex_White);
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
            Widgets.Label(reusedRect, $"{SummaryUICache.CurAllCrewCount}/{SummaryUICache.CrewCeiling}");

            reusedRect = new(reusedRect.xMax + 2f, innerRect.y, 128f, 32f);
            Text.Font = GameFont.Medium;
            Widgets.Label(reusedRect, "OARO_BranchSiteDistanceStr".Translate());

            reusedRect = new(reusedRect.x, reusedRect.yMax + 2f, reusedRect.width, 51f);
            Rect reusedRectII = reusedRect;
            reusedRectII.yMax = reusedRectII.yMin + reusedRect.height * 0.67f;
            Widgets.Label(reusedRectII, SummaryUICache.AffectedRange.ToString("F0").Colorize(SummaryUICache.IsInAffectedRange ? Color.green : Color.white));

            reusedRectII.yMin = reusedRectII.yMax;
            reusedRectII.yMax = reusedRect.yMax;
            Text.Font = GameFont.Small;
            if (SummaryUICache.IsInAffectedRange)
            {
                Widgets.Label(reusedRectII, "OARO_InAffectedRange".Translate().Colorize(Color.green));
            }
            else
            {
                Widgets.Label(reusedRectII, "OARO_OutOfAffectedRange".Translate());
            }

            reusedRect = new(reusedRect.xMax + 2f, innerRect.y, 150f, 86f);
            reusedRectII = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax - 73f, 175f, 73f);
            GUI.DrawTexture(reusedRectII, potencyLace, ScaleMode.ScaleToFit);

            reusedRectII = reusedRect;
            reusedRectII.height /= 2;
            Text.Font = GameFont.Medium;
            Widgets.Label(reusedRectII, "OARO_BranchPotencyStr".Translate());

            reusedRectII.yMin = reusedRectII.yMax;
            reusedRectII.yMax = reusedRect.yMax;
            Widgets.Label(reusedRectII, SummaryUICache.Potency.ToString());

            Rect normamDemandRect = new(innerRect.xMax - 352f, innerRect.y, 352f, 86f);
            DrawNormalDemand(normamDemandRect, Branch.DemandHandler.NormalDemand);

            Rect criticalDemandRect = Rect.MinMaxRect(innerRect.x, innerRect.y + 88f, innerRect.xMax, innerRect.yMax);
            DrawCriticalDemand(criticalDemandRect, Branch.DemandHandler.CriticalDemand);

            return curButtonResult;
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
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_Check".Translate(), checkButton, checkButton_Down))
            {

            }

            reusedRect = new(labelRect.xMax, labelRect.y, 72f, labelRect.height);

            Widgets.Label(reusedRect, $"OARO_BranchDemandType_{demand.DemandTypeValue}".Translate());

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(inRect.xMax - (10f + 128f), inRect.y + 4f, 128f, 16f);
            string rightUpText = demand.HasAccepted ? "OARO_HasAccepted".Translate()
                                                    : demand.TicksToExpire.ToStringTicksToPeriod();
            Widgets.Label(reusedRect, rightUpText);

            reusedRect = new(inRect.xMax - (10f + 54f), inRect.yMax - (4f + 54f), 54f, 54f);



            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawCriticalDemand(Rect inRect, BranchDemand_Critical demand)
        {
            if (demand is null)
            {
                return;
            }

            Rect infoRect = inRect;
            infoRect.width = 496f;

            Rect reusedRect = new(infoRect.x + 4f, infoRect.yMax - (4f + 153f), 153f, 153f);
            GUI.DrawTexture(reusedRect, criticalDemandInfoLace, ScaleMode.ScaleToFit);

            reusedRect = new(infoRect.x + 48f, infoRect.y + 20f, 128f, 28f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.LabelEllipses(reusedRect, demand.Def.label);

            reusedRect = new(reusedRect.xMax, infoRect.y + 20f, 72f, 28f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(reusedRect, $"OARO_BranchDemandType_{demand.DemandTypeValue}".Translate());

            reusedRect = new(infoRect.x + 48f, reusedRect.yMax + 20f, 92f, 25f);
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_Check".Translate(), checkButton, checkButton_Down))
            {

                curButtonResult = ButtonResult.CheckCritical;
            }

            reusedRect = new(reusedRect.xMax + 12f, reusedRect.y, 92f, 25f);
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_Check".Translate(), checkButton, checkButton_Down))
            {

            }

            reusedRect = new(infoRect.xMax - (25f + 128f), infoRect.yMax - 52f, 128f, 22f);
            Widgets.Label(reusedRect, "OARO_PotentialBranchMedalType".Translate());

            Rect medalOutRect = new(infoRect.xMax - (10f + 168f), infoRect.yMax - (3f + 36f), 168f, 36f);

            Rect medalViewRect = medalOutRect;
            float entryX = medalViewRect.x;
            float entryY = medalViewRect.y;
            float entryWidth = 56f;
            float entryHeight = 36f;
            IReadOnlyList<BranchMedalDef> potentialMedals = demand.PotentialMedals;
            medalViewRect.width = entryWidth * potentialMedals.Count;

            Widgets.BeginScrollView(medalOutRect, ref scrollPosition_Medals, medalViewRect, showScrollbars: false);
            Rect entryRect;
            for (int i = 0; i < potentialMedals.Count; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryX += entryWidth;
                GUI.DrawTexture(entryRect, potentialMedals[i].IconTexture, ScaleMode.ScaleToFit);
            }
            Widgets.EndScrollView();

            reusedRect = infoRect;
            reusedRect.xMin = reusedRect.xMax;
            reusedRect.xMax += 2f;
            GUI.DrawTexture(reusedRect, criticalDemandCuttingLine);

            Rect tagRect = Rect.MinMaxRect(reusedRect.xMax, inRect.yMin, inRect.xMax, inRect.yMax);
            reusedRect = tagRect.ContractedBy(3f);
            reusedRect.height = 137f;
            GUI.DrawTexture(reusedRect, criticalDemandTagLace, ScaleMode.ScaleToFit);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerCenter;
            reusedRect = new(tagRect.x + 8f, tagRect.y + 2f, 96f, 18f);
            Widgets.Label(reusedRect, "OARO_BranchDemandTags".Translate());

            Rect tagOutRect = Rect.MinMaxRect(tagRect.xMin + 40f, tagRect.yMin + 30f, tagRect.xMax - 40f, tagRect.yMax - 30f);

            Rect tagViewRect = tagOutRect;
            entryX = tagViewRect.x;
            entryY = tagViewRect.y;
            entryWidth = (tagOutRect.width - 12f) / 2 - float.Epsilon;
            entryHeight = (tagOutRect.height - 8f) / 2;
            float entryXInterval = 12f;
            float entryYInterval = 8f;
            int column = 0;
            IReadOnlyList<QuestEffectTag> questEffectTags = demand.QuestEffectTags;
            tagViewRect.height = Mathf.Ceil(questEffectTags.Count / 2) * (entryHeight + entryYInterval);
            Widgets.BeginScrollView(medalOutRect, ref scrollPosition_Tags, medalViewRect, showScrollbars: false);

            entryRect = default;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            for (int i = 0; i < questEffectTags.Count; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                if ((++column) >= 2)
                {
                    entryX = tagViewRect.x;
                    entryY += (entryHeight + entryYInterval);
                }
                else
                {
                    entryX += (entryWidth + entryXInterval);
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