using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingSummaryCacheEntry
{
    public BranchBuilding Building;
    public List<string> EffectDesc;

    private string effectDescJoint;
    public string EffectDescJoint
    {
        get
        {
            if (effectDescJoint is null)
            {
                if (EffectDesc.NullOrEmpty())
                {
                    effectDescJoint = string.Empty;
                }
                else
                {
                    StringBuilder sb = new();
                    for (int i = 0; i < EffectDesc.Count; i++)
                    {
                        sb.AppendLine(EffectDesc[i]);
                    }
                    effectDescJoint = sb.ToString();
                }
            }
            return effectDescJoint;
        }
    }

    public BranchBuildingSummaryCacheEntry() { }
    public BranchBuildingSummaryCacheEntry(BranchBuilding building)
    {
        Building = building ?? throw new ArgumentNullException(nameof(building));
    }
}

public class BranchFacilitySummaryCacheEntry
{
    public BranchFacilityDef Def;
    public BranchFacilityLevel Level;

    public BranchFacilityLevelStage CurStage;
    public List<string> CurStageEffectDesc;
    private string curStageEffectDescJoint;
    public string CurStageEffectDescJoint
    {
        get
        {
            if (curStageEffectDescJoint is null)
            {
                if (CurStageEffectDesc.NullOrEmpty())
                {
                    curStageEffectDescJoint = string.Empty;
                }
                else
                {
                    StringBuilder sb = new();
                    for (int i = 0; i < CurStageEffectDesc.Count; i++)
                    {
                        sb.AppendLine(CurStageEffectDesc[i]);
                    }
                    curStageEffectDescJoint = sb.ToString();
                }
            }
            return curStageEffectDescJoint;
        }
    }


    public BranchFacilityLevelStage NextStage;
    public List<string> NextStageEffectDesc;
    private string nextStageEffectDescJoint;
    public string NextStageEffectDescJoint
    {
        get
        {
            if (nextStageEffectDescJoint is null)
            {
                if (NextStageEffectDesc.NullOrEmpty())
                {
                    nextStageEffectDescJoint = string.Empty;
                }
                else
                {
                    StringBuilder sb = new();
                    for (int i = 0; i < NextStageEffectDesc.Count; i++)
                    {
                        sb.AppendLine(NextStageEffectDesc[i]);
                    }
                    nextStageEffectDescJoint = sb.ToString();
                }
            }
            return nextStageEffectDescJoint;
        }
    }

    public BranchFacilitySummaryCacheEntry() { }
    public BranchFacilitySummaryCacheEntry(BranchFacilityDef def, BranchFacilityLevel level)
    {
        Def = def ?? throw new ArgumentNullException(nameof(def));
        Level = level;
        CurStage = def.GetLevelStage(level);
        if (CurStage is not null)
        {

        }



        if (level >= BranchFacilityLevel.Excellent)
        {
            return;
        }

        NextStage = def.GetLevelStage(level.FacilityLevelOffSetBy(1));
        if (NextStage is not null)
        {

        }
    }
}

public class Window_Branch : Window
{
    private enum TabType
    {
        Construction,
        Demand,
        Interaction
    }

    private enum SelectType
    {
        Facility,
        Building,
        EmptyBuildingSlot
    }

    public override Vector2 InitialSize => new(1522f, 907f);

    private Vector2 scrollPosition_Facilities;

    private BranchInfoCacheEntry cachedBranchInfo;
    protected Branch Branch => cachedBranchInfo.Branch;

    private TabType curTab = TabType.Construction;

    private BranchBuildingSummaryCacheEntry selBuildingCache;
    private BranchBuildingDef SelBuildingDef => selBuildingCache?.Building.Def;
    private BranchFacilitySummaryCacheEntry selFacilityCache;
    private BranchFacilityDef SelFacilityDef => selFacilityCache?.Def;

    public Window_Branch()
    {
        forcePause = true;
        draggable = false;
        resizeable = false;
        doCloseButton = false;
        doCloseX = false;

        layer = WindowLayer.Dialog;  //窗体层级
        doWindowBackground = false; //绘制泰南的界面背景
        drawShadow = false; //绘制主体界面阴影

        //声音
        //注：用的通讯台声音
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect reusedRect = default;
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1522f, 907f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(4f);
        float mainInnerRectY = mainInnerRect.yMin;

        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 48f, 638f, 152f);
        GUI.DrawTexture(mainRect, topTitleBackground);


        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleRight;
        float mainInnerRectMidX = (mainInnerRect.xMin + mainInnerRect.xMax) / 2;
        reusedRect = new(mainInnerRectMidX - (12f + 128f), mainInnerRectY + 65f, 128f, 32f);
        Widgets.Label(reusedRect, Branch.FacilityHandler.TotalFacilityLevel.ToString());

        reusedRect = new(mainInnerRectMidX - (12f + 192f), reusedRect.yMax + 10f, 192f, 32f);
        Widgets.Label(reusedRect, "OARO_TotalFacilitiesLevel".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = new(mainInnerRectMidX + 70f, mainInnerRectY + 36f, 240f, 126f);
        DrawStoresReserves(reusedRect);

        //中部区域
        Rect middleRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 90f, 579f, (538f + 46f));
        DrawMiddleRect(middleRect);

        //左丨中分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMin - (32f + 3f), 3f, 717f);
        GUI.DrawTexture(mainRect, verticalCuttingLine);

        //左侧区域
        Rect leftRect = new(reusedRect.xMin - (32f + 393f), mainInnerRectY + 198f, 393f, 590f);
        DrawLeftRect(leftRect);

        //中丨右分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMax + 32f, 3f, 717f);
        GUI.DrawTexture(mainRect, verticalCuttingLine);

        Rect rightRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, reusedRect.xMax + 32f, 305f, 717f);
        DrawRightRect(reusedRect);

    }

    private void DrawStoresReserves(Rect inRect)
    {
        IReadOnlyList<BranchStoresReserveHandler.ReserveRecord> storesReserves = Branch.StoresReserveHandler.StoresReserves;
        Rect reusedRect = new(inRect.x, inRect.y, 106f, 126f);
        GUI.DrawTexture(reusedRect, topStoresReserveFrameI);
        if (storesReserves.Count > 0)
        {
            reusedRect = new(reusedRect.x + 4f, reusedRect.y + 30f, 80f, 80f);
        }

        reusedRect = new(inRect.x + (106f + 14f), inRect.yMax - 85f, 73f, 85f);
        GUI.DrawTexture(reusedRect, topStoresReserveFrameII);
        if (storesReserves.Count > 1)
        {
            reusedRect = new(reusedRect.x + 2f, reusedRect.y + 16f, 55f, 55f);
        }

        reusedRect = new(inRect.xMax - 69f, inRect.yMax - 81f, 69f, 81f);
        GUI.DrawTexture(reusedRect, topStoresReserveFrameIII);
        if (storesReserves.Count > 2)
        {
            reusedRect = new(reusedRect.x + 2f, reusedRect.y + 12f, 55f, 55f);
        }

        reusedRect = new(inRect.xMax - 110f, inRect.y + 10f, 110f, 22f);
        Widgets.Label(reusedRect, "OARO_StoresReservesConstruction".Translate());
        reusedRect = new(reusedRect.xMax - 13f, reusedRect.y, 13f, 22f);
        GUI.DrawTexture(reusedRect, topExclamation);
    }

    private void DrawMiddleRect(Rect inRect)
    {
        Rect constructionTabRect = new(inRect.x, inRect.y, 193f, 46f);
        if (OARO_WindowUtility.TextButtonImage(constructionTabRect, "OARO_ConstructionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            curTab = TabType.Construction;
        }
        Rect demandTabRect = new(constructionTabRect.xMax, inRect.y, 193f, 46f);
        if (OARO_WindowUtility.TextButtonImage(demandTabRect, "OARO_DemandTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            curTab = TabType.Demand;
        }
        Rect interactionTabRect = new(demandTabRect.xMax, inRect.y, 193f, 46f);
        if (OARO_WindowUtility.TextButtonImage(interactionTabRect, "OARO_InteractionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            curTab = TabType.Interaction;
        }

        Rect mainRect = inRect;
        mainRect.xMin += 46f;
        switch (curTab)
        {
            case TabType.Construction:
                Widgets.DrawBox(constructionTabRect);
                DrawConstructionTab(mainRect);
                return;
            case TabType.Demand:
                Widgets.DrawBox(demandTabRect);
                DrawDemandTab(mainRect);
                return;
            case TabType.Interaction:
                Widgets.DrawBox(interactionTabRect);
                DrawInteractionTab(mainRect);
                return;
        }
    }

    private void DrawConstructionTab(Rect inRect)
    {
        GUI.DrawTexture(inRect, constructionBackground);
        inRect = inRect.ContractedBy(2f);
        Rect reusedRect = inRect;
        reusedRect.yMax = reusedRect.yMin + 70f;

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_BranchFacilities".Translate());
        Text.Font = GameFont.Small;

        Rect facilitiesRect = inRect;
        facilitiesRect.yMin = reusedRect.yMax + 3f;
        facilitiesRect.yMax = facilitiesRect.yMin + 225f;
        DrawFacilities(facilitiesRect);

        reusedRect = inRect;
        reusedRect.yMin = facilitiesRect.yMax + 2f;
        reusedRect.yMax = reusedRect.yMin + 70f;

        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_BranchBuildings".Translate());
        Text.Font = GameFont.Small;
        reusedRect.xMax -= 12f;
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_BranchBuildingCeiling".Translate(cachedBranchInfo.BuildingCeiling.ToString(), BranchStatDefOf.OARO_BuildingCeiling.maxValue.ToString("F0")));
        Text.Anchor = TextAnchor.MiddleCenter;

        reusedRect = inRect;
        reusedRect.yMin = reusedRect.yMax + 2f;
        DrawBuildings(reusedRect);
    }

    private void DrawFacilities(Rect inRect)
    {
        IReadOnlyDictionary<BranchFacilityDef, BranchFacilityLevel> facilities = Branch.FacilityHandler.Facilities;
        Rect viewRect = inRect;
        float entryWidth = 144f;
        float entryHeight = inRect.height;
        viewRect.width = facilities.Count * entryWidth;

        Widgets.BeginScrollView(inRect, ref scrollPosition_Facilities, viewRect);
        float entryX = inRect.x;
        float entryY = inRect.y;

        Rect entryRect;
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> kv in facilities)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryX += entryWidth;

            DrawFacility(entryRect, kv.Key, kv.Value);
        }


    }

    private void DrawFacility(Rect inRect, BranchFacilityDef facilityDef, BranchFacilityLevel facilityLevel)
    {

    }

    private void DrawBuildings(Rect inRect)
    {

    }

    private void DrawBulding(Rect inRect, BranchBuilding building)
    {

    }

    private void DrawConstructingBuilding(Rect inRect, BranchBuildingDef buildingDef)
    {

    }

    private void DrawDemandTab(Rect inRect)
    {

    }

    private void DrawInteractionTab(Rect inRect)
    {

    }

    private void DrawLeftRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftBackground);

        Branch branch = Branch;
        Rect reusedRect = new(inRect.x + 40f, inRect.y - (40f + 44f), 41f, 44f);
        GUI.DrawTexture(reusedRect, leftTopSiteIcon);
        reusedRect = Rect.MinMaxRect(reusedRect.xMax + 40f, reusedRect.y, inRect.xMax - 40f, reusedRect.yMax);
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, branch.Name);

        inRect.ContractedBy(2f);
        reusedRect = new(inRect.x, inRect.y, 299f, 87f);
        OARO_WindowUtility.DrawBranchSummary(reusedRect, cachedBranchInfo);

        Rect textRect = new(inRect.x, reusedRect.yMax + 4f, inRect.width, 420f);
        reusedRect = OARO_WindowUtility.CenterRect(textRect, 381f, 416f);
        GUI.DrawTexture(reusedRect, leftBackgroundLace);

        reusedRect = OARO_WindowUtility.CenterRectOnX(textRect, textRect.yMax - 134f, 361f, 134f);
        GUI.DrawTexture(reusedRect, leftDownBackgroundTexture);

        Widgets.TextArea(textRect, "", readOnly: true);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        reusedRect = new(inRect.x, textRect.xMax + 2f, 36f, 264f);
        Widgets.Label(reusedRect, "OARO_BranchPopulation".Translate(branch.PopulationHandler.Population.ToString()));
        reusedRect = new(inRect.x, reusedRect.xMax + 2f, 36f, 264f);
        Widgets.Label(reusedRect, "OARO_BranchPopulationCeiling".Translate(cachedBranchInfo.PopulationCeiling.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + (2f + 10f), textRect.yMax + (2f + 10f), 90f, 16f);
        Widgets.Label(reusedRect, "OARO_DailyChange".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Medium;
        reusedRect = new(inRect.xMax - (12f + 100f), reusedRect.yMax + 12f, 100f, 24f);
        Widgets.Label(reusedRect, "OARO_PopulationDailyChange".Translate(cachedBranchInfo.DailyPopulationGrowth_Bottom.ToString(), cachedBranchInfo.DailyPopulationGrowth_Ceiling.ToString())
                                                              .Colorize(cachedBranchInfo.DailyPopulationGrowth_Bottom > 0 ? Color.green : ColorLibrary.RedReadable));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;


        if (Mouse.IsOver(reusedRect))
        {
            string tipStr = cachedBranchInfo.DailyPopulationGrowthExplanation;
            if (!string.IsNullOrEmpty(tipStr))
            {
                TooltipHandler.TipRegion(reusedRect, () => tipStr, 36746149);
            }
        }
    }

    private void DrawRightRect(Rect inRect) { }


    private void ClearSelect()
    {

    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MainBackground");
    private static readonly Texture2D topTitleBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopTitleBackground");

    private static readonly Texture2D topStoresReserveFrameI = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopStoresReserveFrameI");
    private static readonly Texture2D topStoresReserveFrameII = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopStoresReserveFrameII");
    private static readonly Texture2D topStoresReserveFrameIII = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopStoresReserveFrameIII");

    private static readonly Texture2D topExclamation = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopExclamation");


    private static readonly Texture2D middleTopButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MiddleTopButton");
    private static readonly Texture2D middleTopButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MiddleTopButton_Down");

    private static readonly Texture2D constructionBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructionBackground");

    private static readonly Texture2D leftBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftBackground");
    private static readonly Texture2D leftBackgroundLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftBackgroundLace");
    private static readonly Texture2D leftTopSiteIcon = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftTopSiteIcon");
    private static readonly Texture2D leftDownBackgroundTexture = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftDownBackgroundTexture");

    private static readonly Texture2D verticalCuttingLine = ContentFinder<Texture2D>.Get("UI/Branch/OARO_VerticalCuttingLine");
}
