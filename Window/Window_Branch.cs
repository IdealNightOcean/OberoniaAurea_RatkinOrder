using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_Branch : MainTabWindow
{
    private enum TabType
    {
        Construction,
        Demand,
        Interaction
    }

    private enum SelectType
    {
        None,
        Facility,
        Building,
        ConstructingBuilding,
        EmptyBuildingSlot,
        Demand
    }
    protected override float Margin => 0f;
    public override Vector2 InitialSize => new(1586f, 907f);
    public override Vector2 RequestedTabSize => new(1586f, 907f);

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    private Vector2 scrollPosition_Facilities;
    private Vector2 scrollPosition_CurFacilityStage;
    private Vector2 scrollPosition_NextFacilityStage;
    private Vector2 scrollPosition_Buildings;
    private Vector2 scrollPosition_BuildingBaseEffect;
    private Vector2 scrollPosition_BuildingAdvancedEffect;

    private Caravan caravan;
    private Branch branch;
    private BranchInfoUICache cachedBranchInfo;

    private TabType curTab = TabType.Construction;
    private SelectType curSelectType = SelectType.None;

    private BranchBuilding selBuilding;
    private BranchBuildingDefSummaryUICache selBuildingDefCache;
    private BranchBuildingDef SelBuildingDef => selBuildingDefCache?.BuildingDef;
    private BranchBuildingConstructionRecord underConstructionBuilding;

    private bool? selEmptyBuildingSlotIsSpecial;

    private BranchFacilityDef selFacilityDef;
    private BranchFacilityStageSummaryUICache curFacilityStageCache;
    private BranchFacilityStageSummaryUICache nextFacilityStageCache;

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

    public override void PreOpen()
    {
        base.PreOpen();
        ClearSelect();
        branch = RatkinOrderManager.AllRatkinOrders[0].BranchManager.AllBranches[0];
        Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
        cachedBranchInfo = new(branch, map);
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1522f, 907f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(4f);
        float mainInnerRectY = mainInnerRect.yMin;

        Rect reusedRect;

        float offsetMainInnerMidX = mainInnerRect.xMin + mainInnerRect.width * 0.55f;
        reusedRect = new(offsetMainInnerMidX - 638f * 0.5f, mainInnerRectY + 48f, 638f, 152f);
        GUI.DrawTexture(reusedRect, topTitleBackground);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(offsetMainInnerMidX - (12f + 128f), mainInnerRectY + 65f, 128f, 32f);
        Widgets.Label(reusedRect, branch.FacilityHandler.TotalFacilityLevel.ToString());

        reusedRect = new(offsetMainInnerMidX - (12f + 192f), reusedRect.yMax + 10f, 192f, 32f);
        Widgets.Label(reusedRect, "OARO_TotalFacilitiesLevel".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = new(offsetMainInnerMidX + 70f, mainInnerRectY + 36f, 240f, 126f);
        DrawStoresReserves(reusedRect);

        //中部区域
        Rect middleRect = new(offsetMainInnerMidX - 578f * 0.5f, mainInnerRectY + 210f, 579f, (538f + 46f));
        DrawMiddleRect(middleRect);

        //左丨中分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMin - (32f + 3f), 3f, 717f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);

        //左侧区域
        Rect leftRect = new(reusedRect.xMin - (48f + 393f), mainInnerRectY + 198f, 393f, 590f);
        DrawLeftRect(leftRect);

        //中丨右分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMax + 32f, 3f, 717f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);

        Rect rightRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, reusedRect.xMax + 32f, 305f, 635f);
        DrawRightRect(reusedRect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawStoresReserves(Rect inRect)
    {
        IReadOnlyList<BranchStoresReserveHandler.ReserveRecord> storesReserves = branch.StoresReserveHandler.StoresReserves;
        Rect reusedRect = new(inRect.x, inRect.y, 106f, 126f);
        GUI.DrawTexture(reusedRect, topStoresReserveFrameI);
        if (storesReserves.Count > 0)
        {
            reusedRect = new(reusedRect.x + 4f, reusedRect.y + 30f, 80f, 80f);
            GUI.DrawTexture(reusedRect, storesReserves[0].Target.IconTexture);
        }

        reusedRect = new(inRect.x + 100f, inRect.yMax - 85f, 73f, 85f);
        GUI.DrawTexture(reusedRect, topStoresReserveFrameII);
        if (storesReserves.Count > 1)
        {
            reusedRect = new(reusedRect.x + 2f, reusedRect.y + 16f, 55f, 55f);
            GUI.DrawTexture(reusedRect, storesReserves[1].Target.IconTexture);
        }

        reusedRect = new(inRect.xMax - 69f, inRect.yMax - 81f, 69f, 81f);
        GUI.DrawTexture(reusedRect, topStoresReserveFrameIII);
        if (storesReserves.Count > 2)
        {
            reusedRect = new(reusedRect.x + 2f, reusedRect.y + 12f, 55f, 55f);
            GUI.DrawTexture(reusedRect, storesReserves[2].Target.IconTexture);
        }

        reusedRect = new(inRect.xMax - 110f, inRect.y + 10f, 110f, 22f);
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_StoresReservesConstruction".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        reusedRect = new(reusedRect.xMin - 13f, reusedRect.y, 13f, 22f);
        GUI.DrawTexture(reusedRect, topExclamation);
    }

    private void DrawMiddleRect(Rect inRect)
    {
        Rect constructionTabRect = new(inRect.x, inRect.y, 193f, 46f);
        if (OARO_WindowUtility.TextButtonImage(constructionTabRect, "OARO_ConstructionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Construction);
        }
        Rect demandTabRect = new(constructionTabRect.xMax, inRect.y, 193f, 46f);
        if (OARO_WindowUtility.TextButtonImage(demandTabRect, "OARO_DemandTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Demand);
        }
        Rect interactionTabRect = new(demandTabRect.xMax, inRect.y, 193f, 46f);
        if (OARO_WindowUtility.TextButtonImage(interactionTabRect, "OARO_InteractionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Interaction);
        }

        Rect mainRect = inRect;
        mainRect.yMin += 46f;
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

        Rect facilitiesRect = inRect;
        facilitiesRect.yMin = reusedRect.yMax + 3f;
        facilitiesRect.yMax = facilitiesRect.yMin + 225f;
        DrawFacilityList(facilitiesRect);

        reusedRect = inRect;
        reusedRect.yMin = facilitiesRect.yMax + 2f;
        reusedRect.yMax = reusedRect.yMin + 70f;

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_BranchBuildings".Translate());

        reusedRect.xMax -= 12f;
        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, "OARO_BranchBuildingCeiling".Translate(cachedBranchInfo.BuildingCeiling.ToString(), BranchStatDefOf.OARO_BuildingCeiling.maxValue.ToString("F0")));

        float yMin = reusedRect.yMax + 4f;
        reusedRect = inRect;
        reusedRect.yMin = yMin;
        DrawBuildingList(reusedRect);

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private void DrawFacilityList(Rect inRect)
    {
        IReadOnlyDictionary<BranchFacilityDef, BranchFacilityLevel> facilities = branch.FacilityHandler.Facilities;
        Rect viewRect = inRect;
        float entryWidth = 144f;
        float entryHeight = inRect.height;
        viewRect.width = facilities.Count * entryWidth;
        viewRect.height = entryHeight;

        Widgets.BeginScrollView(inRect, ref scrollPosition_Facilities, viewRect, showScrollbars: false);
        float entryX = inRect.x;
        float entryY = inRect.y;

        Rect entryRect;
        foreach (KeyValuePair<BranchFacilityDef, BranchFacilityLevel> kv in facilities)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryX += entryWidth;
            GUI.DrawTexture(entryRect, facilityRect);
            entryRect.xMax -= 2f;
            DrawFacility(entryRect, kv.Key, kv.Value);
        }
        Widgets.EndScrollView();
    }

    private void DrawFacility(Rect inRect, BranchFacilityDef facilityDef, BranchFacilityLevel facilityLevel)
    {
        Rect reusedRect = inRect;
        reusedRect.height = 9f;
        GUI.DrawTexture(reusedRect, facilityLevelBackground);
        reusedRect = new(reusedRect.x + 2f, reusedRect.y + 2f, 33f, 5f);
        for (BranchFacilityLevel i = 0; i < facilityLevel; i++)
        {
            GUI.DrawTexture(reusedRect, facilityLevelItem);
            reusedRect.xMin = reusedRect.xMax + 2f;
            reusedRect.width = 33f;
        }

        reusedRect = Rect.MinMaxRect(inRect.xMin, reusedRect.yMax, inRect.xMax, reusedRect.yMax + 156f);
        Rect textureRect = OARO_WindowUtility.CenterRect(reusedRect, 105f, 96f);
        GUI.DrawTexture(textureRect, facilityDef.ExpandingIconTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;

        reusedRect = Rect.MinMaxRect(inRect.xMin, reusedRect.yMax - 32f, inRect.xMax, reusedRect.yMax);
        Widgets.Label(reusedRect, facilityDef.LabelCap);

        float preYMax = reusedRect.yMax;
        reusedRect.yMax = inRect.yMax;
        reusedRect.yMin = preYMax + 2f;
        reusedRect = reusedRect.ContractedBy(2f);
        Widgets.Label(reusedRect, facilityLevel.ToString());
        if (facilityLevel == BranchFacilityLevel.Excellent)
        {
            GUI.DrawTexture(reusedRect, maxFacilityLevelLace);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        bool selected = ((curSelectType == SelectType.Facility) && (selFacilityDef == facilityDef));
        if (selected)
        {
            Widgets.DrawBox(inRect);
        }
        if (Widgets.ButtonInvisible(inRect))
        {
            curSelectType = SelectType.Facility;
            if (selected)
            {
                Deselect();
            }
            else
            {
                selFacilityDef = facilityDef;
                curFacilityStageCache = new(facilityDef, facilityLevel);
                if (facilityLevel < BranchFacilityLevel.Excellent)
                {
                    nextFacilityStageCache = new(facilityDef, facilityLevel.FacilityLevelOffSetBy(1));
                }
                else
                {
                    nextFacilityStageCache = null;
                }
            }
        }
    }

    private void DrawBuildingList(Rect inRect)
    {
        float entryWidth = 192f;
        float entryHeight = 81f;
        float entryX = inRect.x;
        float entryY = inRect.y;
        int column = 0;
        Rect entryRect;

        BranchBuildingHandler buildingHandler = branch.BuildingHandler;
        BranchBuildingConstructionRecord underConstructionBuilding = buildingHandler.UnderConstructionBuilding;
        bool isBusy = underConstructionBuilding is not null;

        int potentialBuildingCount = 1 + cachedBranchInfo.BuildingCeiling;
        Rect viewRect = inRect;
        viewRect.height = Mathf.Max(Mathf.CeilToInt(potentialBuildingCount / 3f), 2) * entryHeight;
        Widgets.BeginScrollView(inRect, ref scrollPosition_Buildings, viewRect);

        AdjustEntryRect();
        if (buildingHandler.SpecialBuilding is null)
        {
            if (isBusy && underConstructionBuilding.InSpecialSlot)
            {
                DrawConstructingBuilding(entryRect, underConstructionBuilding);
            }
            else
            {
                DrawEmptyBulding(entryRect, isSpecialSlot: true, isBusy: isBusy);
            }
        }
        else
        {
            DrawBulding(entryRect, buildingHandler.SpecialBuilding, isSpecialSlot: true);
        }

        IReadOnlyList<BranchBuilding> buildings = branch.BuildingHandler.Buildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            AdjustEntryRect();
            DrawBulding(entryRect, buildings[i], isSpecialSlot: false);
        }

        if (isBusy && !underConstructionBuilding.InSpecialSlot)
        {
            AdjustEntryRect();
            DrawConstructingBuilding(entryRect, underConstructionBuilding);
        }

        if (buildings.Count < cachedBranchInfo.BuildingCeiling)
        {
            AdjustEntryRect();
            DrawEmptyBulding(entryRect, isSpecialSlot: false, isBusy: false);
        }
        Widgets.EndScrollView();

        void AdjustEntryRect()
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            column++;
            if (column >= 3)
            {
                column = 0;
                entryX = 0f;
                entryY += entryHeight;
            }
            else
            {
                entryX += entryWidth;
            }

            GUI.DrawTexture(entryRect, buildingRect);
            entryRect.xMax -= 2f;
            entryRect.yMax -= 2f;
        }
    }

    private void DrawBulding(Rect inRect, BranchBuilding building, bool isSpecialSlot)
    {
        Rect innerRect = inRect.ContractedBy(5f);
        if (isSpecialSlot)
        {
            GUI.DrawTexture(innerRect, specialBuildingLace, ScaleMode.ScaleToFit);
        }
        else if (building.HasUpgraded)
        {
            GUI.DrawTexture(innerRect, upgradedBuildingLace, ScaleMode.ScaleToFit);
        }

        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(innerRect, innerRect.x + 15f, 40f, 40f);
        GUI.DrawTexture(reusedRect, building.Def.IconTexture, ScaleMode.ScaleToFit);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 15f, 105f, 24f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, building.Label);
        Text.Anchor = TextAnchor.UpperLeft;

        bool selected = ((curSelectType == SelectType.Building) && (SelBuildingDef == building.Def));
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
        }
        if (Widgets.ButtonInvisible(inRect))
        {
            curSelectType = SelectType.Building;
            if (selected)
            {
                Deselect();
            }
            else
            {
                if (selBuilding != building)
                {
                    selBuilding = building;
                    selBuildingDefCache = new(building.Def);
                }
            }
        }
    }

    private void DrawEmptyBulding(Rect inRect, bool isSpecialSlot, bool isBusy)
    {
        if (isBusy)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.DrawTexture(inRect, buildingConstructButton_Down);
            Widgets.Label(inRect, "OARO_OtherBuildingConstructing".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }
        else
        {
            bool selected = ((curSelectType == SelectType.EmptyBuildingSlot) && (selEmptyBuildingSlotIsSpecial == isSpecialSlot));
            if (selected)
            {
                Widgets.DrawBox(inRect);
            }

            if (OARO_WindowUtility.TextButtonImage(inRect, "OARO_ClickToConstructBuilding".Translate(), buildingConstructButton, buildingConstructButton_Down))
            {
                curSelectType = SelectType.EmptyBuildingSlot;
                if (selected)
                {
                    Deselect();
                }
                else
                {
                    selEmptyBuildingSlotIsSpecial = isSpecialSlot;
                }
            }
        }
    }

    private void DrawConstructingBuilding(Rect inRect, BranchBuildingConstructionRecord underConstructionBuilding)
    {
        BranchBuildingDef buildingDef = underConstructionBuilding.BuildingDef;
        bool selected = ((curSelectType == SelectType.ConstructingBuilding) && (this.underConstructionBuilding == underConstructionBuilding));
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
        }

        if (Widgets.ButtonInvisible(inRect))
        {
            curSelectType = SelectType.ConstructingBuilding;
            if (selected)
            {
                Deselect();
            }
            else
            {
                this.underConstructionBuilding = underConstructionBuilding;
                selBuildingDefCache = new(buildingDef);
            }
        }

        inRect = inRect.ContractedBy(5f);
        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.x + 15f, 40f, 40f);
        GUI.DrawTexture(reusedRect, buildingDef.IconTexture, ScaleMode.ScaleToFit);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 15f, 105f, 24f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, buildingDef.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;
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

        Rect reusedRect;
        Rect titleRect = new(inRect.x, inRect.y - (24f + 40f), inRect.width, 40f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Medium;
        float textWidth = Text.CalcSize(branch.Name).x;
        reusedRect = OARO_WindowUtility.CenterRectOnX(titleRect, titleRect.y, Mathf.Min(textWidth, 256f), 40f);
        reusedRect.xMax += 12f;
        reusedRect.xMin += 12f;
        Widgets.Label(reusedRect, branch.Name);
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMin - (40f + 4f), 45f, 45f);
        GUI.DrawTexture(reusedRect, leftTopSiteIcon, ScaleMode.ScaleToFit);

        reusedRect = OARO_WindowUtility.DrawBranchSummary(new Vector2(inRect.x, inRect.y), cachedBranchInfo);

        inRect.ContractedBy(2f);

        Rect textRect = new(inRect.x, reusedRect.yMax + 2f, inRect.width, 420f);
        reusedRect = OARO_WindowUtility.CenterRect(textRect, 381f, 416f);
        GUI.DrawTexture(reusedRect, leftBackgroundLace);

        reusedRect = OARO_WindowUtility.CenterRectOnX(textRect, textRect.yMax - 134f, 361f, 134f);
        GUI.DrawTexture(reusedRect, leftDownBackgroundPattern);

        Widgets.TextArea(textRect, "", readOnly: true);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        reusedRect = new(inRect.x, textRect.yMax + 2f, 264f, 36f);
        Widgets.Label(reusedRect, "OARO_BranchPopulation".Translate(branch.PopulationHandler.Population.ToString()));
        reusedRect = new(inRect.x, reusedRect.yMax + 2f, 264f, 36f);
        Widgets.Label(reusedRect, "OARO_BranchPopulationCeiling".Translate(cachedBranchInfo.PopulationCeiling.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + (2f + 10f), textRect.yMax + (2f + 10f), 90f, 24f);
        Widgets.Label(reusedRect, "OARO_DailyChange".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Medium;
        reusedRect = new(inRect.xMax - (12f + 100f), reusedRect.yMax + 2f, 100f, 24f);
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

    private void DrawRightRect(Rect inRect)
    {
        switch (curSelectType)
        {
            case SelectType.None: return;
            case SelectType.Facility:
                DrawRight_Facility(inRect);
                return;
            case SelectType.Building or SelectType.ConstructingBuilding:
                DrawRight_Building(inRect);
                return;
            case SelectType.EmptyBuildingSlot:
                DrawRight_EmptyBuildingSlot(inRect);
                return;
            default: return;
        }
    }

    private void DrawRight_Facility(Rect inRect)
    {
        if (selFacilityDef is null)
        {
            return;
        }

        Rect reusedRect = new(inRect.x + 42f, inRect.y + 75f, 105f, 96f);
        GUI.DrawTexture(reusedRect, selFacilityDef.ExpandingIconTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(reusedRect.xMax + 4f, reusedRect.y, 185f, 48f);
        Widgets.Label(reusedRect, selFacilityDef.LabelCap);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;
        reusedRect = new(reusedRect.x, reusedRect.yMax, 185f, 48f);
        Widgets.Label(reusedRect, selFacilityDef.description);

        float commonXMin = inRect.x + 32f;
        float commonWidth = 298f;
        float stageRectHeight = 24f + 2f + 134f;
        Rect descRect = new(commonXMin, reusedRect.yMax + 50f, commonWidth, stageRectHeight);
        if (curFacilityStageCache is not null)
        {
            TaggedString label = "OARO_CurFacilityStage".Translate();
            if (curFacilityStageCache.Level == BranchFacilityLevel.Excellent)
            {
                label += " (Max)";
            }
            reusedRect = descRect;
            reusedRect.height = 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, "OARO_CurFacilityStage".Translate());
            DrawEffectDescriptions(new Vector2(descRect.x, reusedRect.yMax + 2f), label, curFacilityStageCache.StageEffectDesc, ref scrollPosition_CurFacilityStage);
        }

        descRect = new(commonXMin, descRect.yMax + 48f, commonWidth, stageRectHeight);
        if (nextFacilityStageCache is not null)
        {
            reusedRect = descRect;
            reusedRect.height = 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, "OARO_NextFacilityStage".Translate());
            DrawEffectDescriptions(new Vector2(descRect.x, reusedRect.yMax + 2f), "OARO_NextFacilityStage".Translate(), nextFacilityStageCache.StageEffectDesc, ref scrollPosition_NextFacilityStage);
        }

        BranchFacilityHandler facilityHandler = branch.FacilityHandler;
        if (facilityHandler.IsBusy)
        {
            BranchFacilityConstructionRecord buildingFacility = facilityHandler.BuildingFacility;
            if (buildingFacility?.FacilityDef == selFacilityDef)
            {
                reusedRect = new(commonXMin, reusedRect.yMax + 16f, commonWidth, 24f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(reusedRect, "OARO_Constructing".Translate());
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(reusedRect, "OARO_DaysToCompleted".Translate(buildingFacility.DurationTicksLeft.TicksToDays().ToString("0.##")));
                reusedRect = new(commonXMin, reusedRect.yMax, commonWidth, 24f);

                //reusedRect = reusedRect.ContractedBy(2f);
                Widgets.FillableBar(reusedRect, buildingFacility.Progress);
                reusedRect = new(commonXMin + 2f, reusedRect.yMax, 89f, 30f);
                if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_CancelConstruct".Translate(), constructButton, constructButton_Down))
                {
                    Dialog_NodeTree dialog_Node = OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
                         text: "OARO_CancelConstructWarnning".Translate(),
                         acceptAction: branch.FacilityHandler.CancelFacilityConstruction);
                    Find.WindowStack.Add(dialog_Node);
                }

                reusedRect = new(commonXMin + commonWidth - (2f + 89f), reusedRect.y, 89f, 30f);
                GUI.DrawTexture(reusedRect, constructButton_Down);
            }
            else
            {
                reusedRect = new(commonXMin, reusedRect.yMax + 16f, commonWidth, 32f);
                Widgets.Label(reusedRect, "OARO_OtherFacilityBuilding".Translate());
            }
        }
        else if(nextFacilityStageCache is not null)
        {
            reusedRect = new(commonXMin, reusedRect.yMax + 16f, commonWidth, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(reusedRect, "OARO_ExpectedCost".Translate());
            reusedRect.xMin += 0.33f * commonWidth;
            Widgets.Label(reusedRect, nextFacilityStageCache.Stage.constructionDays.ToString() + "Day".Translate());
            reusedRect.xMin += 0.33f * commonWidth;
            //
            reusedRect.xMin += 24f;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(reusedRect, $"× {nextFacilityStageCache.Stage.silverCost}");
            reusedRect = new(commonXMin + 2f, reusedRect.yMax + 24f, 89f, 30f);
            GUI.DrawTexture(reusedRect, constructButton_Down);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, "OARO_CancelConstruct".Translate());
            reusedRect = new(commonXMin + commonWidth - (2f + 89f), reusedRect.y, 89f, 30f);
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_StartConstruct".Translate(), constructButton, constructButton_Down))
            {
                AcceptanceReport acceptance = branch.FacilityHandler.CanConstructFacility(selFacilityDef, byPlayer: true, resultOnly: false);
                if (acceptance)
                {
                    branch.FacilityHandler.StartFacilityConstruction(selFacilityDef, byPlayer: true);
                }
                else
                {
                    Messages.Message("OARO_CanNotStartFacilityConstruction".Translate(acceptance.Reason), MessageTypeDefOf.RejectInput, historical: false);
                }
            }
        }
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    /// <summary>
    /// 298,134
    /// </summary>
    private Rect DrawEffectDescriptions(Vector2 position, string title, List<string> stageEffectDesc, ref Vector2 scrollPosition)
    {
        Rect rect = new(position.x, position.y, 298f, 134f);
        Rect inRect = rect;

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        GUI.DrawTexture(inRect, effectDescRect, ScaleMode.ScaleToFit);

        Rect viewRect = inRect.ContractedBy(2f);

        float entryX = viewRect.xMin;
        float entryY = viewRect.yMin;
        float entryWidth = viewRect.width;
        float entryHeight = 26f;
     
        int entryCount = stageEffectDesc.Count;
        int useCount = Mathf.Max(6, entryCount);
        viewRect.height = entryHeight * useCount;
        
        Rect entryRect;
        int index = 0;

        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, showScrollbars: false);
        AdjustEntryRect();
        Widgets.Label(entryRect, title);

        for (int i = 0; i < entryCount; i++)
        {
            AdjustEntryRect();
            Widgets.Label(entryRect, stageEffectDesc[i]);
        }

        if (useCount > entryCount)
        {
            for (int i = entryCount; i < useCount; i++)
            {
                AdjustEntryRect();
            }
        }
        Widgets.EndScrollView();

        Text.Anchor = TextAnchor.UpperLeft;
        return rect;

        void AdjustEntryRect()
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            index++;
            entryY += entryHeight;
            if ((index & 1) == 0)
            {
                GUI.DrawTexture(entryRect, effectDesc_Light);
            }
        }
    }

    private void DrawRight_Building(Rect inRect)
    {
        if (selBuildingDefCache is null)
        {
            return;
        }
        BranchBuildingDef buildingDef = selBuildingDefCache.BuildingDef;

        Rect reusedRect = new(inRect.x + 42f, inRect.y + 75f, 105f, 96f);
        GUI.DrawTexture(reusedRect, buildingDef.ExpandingIconTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(reusedRect.xMax + 4f, reusedRect.y, 185f, 48f);
        string buildingLabel = curSelectType == SelectType.Building ? selBuilding.Label : buildingDef.label;
        Widgets.Label(reusedRect, buildingLabel);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;
        reusedRect = new(reusedRect.x, reusedRect.yMax, 185f, 48f);
        Widgets.Label(reusedRect, buildingDef.description);

        float commonXMin = inRect.x + 32f;
        float commonWidth = 298f;

        Rect descRect = DrawEffectDescriptions(new Vector2(commonXMin, reusedRect.yMax + 32f), "OARO_BuildingBaseEffect".Translate(), selBuildingDefCache.BaseEffectDesc, ref scrollPosition_BuildingBaseEffect);

        if (buildingDef.IsUpgradable)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(commonXMin, descRect.yMax + 8f, commonWidth, 24f);
            Widgets.Label(reusedRect, "OARO_BuildingUpgradePop".Translate());
            reusedRect = new(commonXMin, reusedRect.yMax, commonWidth, 24f);
            Widgets.Label(reusedRect, "OARO_BuildingUpgradePopGE".Translate(buildingDef.advancedProperties.advancedPopulation.ToString()));

            descRect = DrawEffectDescriptions(new Vector2(commonXMin, reusedRect.yMax + 8f), "OARO_BuildingAdvancedEffect".Translate(), selBuildingDefCache.AdvancedEffectDesc, ref scrollPosition_BuildingAdvancedEffect);
        }

        if (curSelectType == SelectType.ConstructingBuilding)
        {

        }
    }

    private void DrawRight_ConstructingBuilding(Rect inRect)
    {

    }

    private void DrawRight_EmptyBuildingSlot(Rect inRect)
    {

    }

    private void SwitchTab(TabType tabType)
    {
        if (curTab == tabType)
        {
            return;
        }
        ClearSelect();
        curTab = tabType;
    }

    private void Deselect()
    {
        switch (curSelectType)
        {
            case SelectType.Facility:
                selFacilityDef = null;
                curFacilityStageCache = null;
                nextFacilityStageCache = null;
                return;
            case SelectType.Building:
                selBuilding = null;
                selBuildingDefCache = null;
                return;
            case SelectType.ConstructingBuilding:
                underConstructionBuilding = null;
                selBuildingDefCache = null;
                return;
            case SelectType.EmptyBuildingSlot:
                selEmptyBuildingSlotIsSpecial = null;
                return;
            default:
                return;
        }
    }

    private void ClearSelect()
    {
        curSelectType = SelectType.None;
        selBuilding = null;
        selBuildingDefCache = null;
        selFacilityDef = null;
        curFacilityStageCache = null;
        nextFacilityStageCache = null;
        selEmptyBuildingSlotIsSpecial = null;
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MainBackground");
    private static readonly Texture2D topTitleBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopTitleBackground");

    private static readonly Texture2D topStoresReserveFrameI = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopStoresReserveFrameI");
    private static readonly Texture2D topStoresReserveFrameII = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopStoresReserveFrameII");
    private static readonly Texture2D topStoresReserveFrameIII = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopStoresReserveFrameIII");

    private static readonly Texture2D topExclamation = ContentFinder<Texture2D>.Get("UI/Branch/OARO_TopExclamation");

    private static readonly Texture2D middleTopButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MiddleTopButton");
    private static readonly Texture2D middleTopButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MiddleTopButton_Down");

    private static readonly Texture2D facilityLevelBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_FacilityLevelBackground");
    private static readonly Texture2D facilityLevelItem = ContentFinder<Texture2D>.Get("UI/Branch/OARO_FacilityLevelItem");
    private static readonly Texture2D maxFacilityLevelLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MaxFacilityLevelLace");


    private static readonly Texture2D buildingConstructButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingConstructButton");
    private static readonly Texture2D buildingConstructButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingConstructButton_Down");
    private static readonly Texture2D upgradedBuildingLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_UpgradedBuildingLace");
    private static readonly Texture2D specialBuildingLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_SpecialBuildingLace");

    private static readonly Texture2D constructionBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructionBackground");
    private static readonly Texture2D facilityRect = ContentFinder<Texture2D>.Get("UI/Branch/OARO_FacilityRect");
    private static readonly Texture2D buildingRect = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingRect");

    private static readonly Texture2D effectDescRect = ContentFinder<Texture2D>.Get("UI/Branch/OARO_EffectRect");
    private static readonly Texture2D effectDesc_Light = ContentFinder<Texture2D>.Get("UI/Branch/OARO_EffectDesc_Light");
    private static readonly Texture2D constructButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructButton");
    private static readonly Texture2D constructButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructButton_Down");


    private static readonly Texture2D leftBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftBackground");
    private static readonly Texture2D leftBackgroundLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftBackgroundLace");
    private static readonly Texture2D leftTopSiteIcon = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftTopSiteIcon");
    private static readonly Texture2D leftDownBackgroundPattern = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftDownBackgroundPattern");

    private static readonly Texture2D verticalCuttingLine = ContentFinder<Texture2D>.Get("UI/Branch/OARO_VerticalCuttingLine");
}
