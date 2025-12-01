using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_Branch : OrderWindowBase
{
    private enum TabType
    {
        Construction,
        Contract,
        Interaction
    }

    private enum SelectType
    {
        None,
        Facility,
        Building,
        ConstructingBuilding,
        EmptyBuildingSlot
    }
    public override Vector2 InitialSize => new(1586f, 907f);

    private readonly Branch branch;
    private BranchInfoUICache cachedBranchInfo;

    private readonly Caravan caravan;
    private readonly Map map;

    private TabType curTab = TabType.Construction;
    private SelectType curSelectType = SelectType.None;
    private readonly int allFacilityDefCount;

    /*
        建设部分缓存
    */
    private BranchBuilding selBuilding;
    private BranchBuildingDefSummaryUICache selBuildingDefCache;
    private BranchBuildingDef SelBuildingDef => selBuildingDefCache?.BuildingDef;
    private UnderConstructionRecord<BranchBuildingDef> UnderConstructionBuilding => branch.BuildingHandler.UnderConstructionBuilding;
    private UnderConstructionRecord<BranchFacilityDef> UnderConstructionFacility => branch.FacilityHandler.UnderConstructionFacility;

    private bool? selEmptyBuildingSlotIsSpecial;

    private BranchFacilityDef selFacilityDef;
    private BranchFacilityStageSummaryUICache curFacilityStageCache;
    private BranchFacilityStageSummaryUICache nextFacilityStageCache;

    private Dictionary<BranchBuildingDef, BranchBuildingDefSummaryUICache> optionalBuildingDefs;
    private Dictionary<BranchBuildingDef, BranchBuildingDefSummaryUICache> OptionalBuildingDefs
    {
        get
        {
            if (optionalBuildingDefs is null)
            {
                RecacheOptionalBuildingDefs();
            }
            return optionalBuildingDefs;
        }
    }

    private Vector2 scrollPosition_Facilities;
    private Vector2 scrollPosition_CurFacilityStage;
    private Vector2 scrollPosition_NextFacilityStage;
    private Vector2 scrollPosition_Buildings;
    private Vector2 scrollPosition_BuildingBaseEffect;
    private Vector2 scrollPosition_BuildingAdvancedEffect;
    private Vector2 scrollPosition_OptionalBuildings;

    /*
        需求部分缓存
    */
    private List<(BranchContract, AcceptanceReport)> contractAcceptances = [];
    private IReadOnlyList<(BranchContract, AcceptanceReport)> ContractAcceptances
    {
        get
        {
            if (contractAcceptanceDirty)
            {
                RecacheContractAcceptance();
            }
            return contractAcceptances;
        }
    }
    private bool contractAcceptanceDirty = true;
    private Vector2 scrollPosition_Contract;

    /*
        交互部分缓存
    */
    private readonly List<(BranchInteractionDef, AcceptanceReport)> commonInteractionAcceptances = [];
    private IReadOnlyList<(BranchInteractionDef, AcceptanceReport)> CommonInteractionAcceptances
    {
        get
        {
            if (interactionAcceptanceDirty)
            {
                RecacheInteractionAcceptance();
            }
            return commonInteractionAcceptances;
        }
    }
    private readonly List<(BranchBuildingComp_Interaction, AcceptanceReport)> buildingInteractionAcceptances = [];
    private IReadOnlyList<(BranchBuildingComp_Interaction, AcceptanceReport)> BuildingInteractionAcceptances
    {
        get
        {
            if (interactionAcceptanceDirty)
            {
                RecacheInteractionAcceptance();
            }
            return buildingInteractionAcceptances;
        }
    }

    private bool interactionAcceptanceDirty = true;

    private Vector2 scrollPosition_CommonInteraction;
    private Vector2 scrollPosition_BuildingInteraction;

    public Window_Branch(Branch branch, Caravan caravan, Map map) : base()
    {
        this.caravan = caravan;
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
        this.map = map ?? OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
        cachedBranchInfo = new(this.branch, this.map);

        allFacilityDefCount = DefDatabase<BranchFacilityDef>.DefCount;

        this.branch.BuildingHandler.PostConstructionChanged += PostConstructionChanged_Building;
        this.branch.PostApplyBranchInteraction += PostApplyBranchInteraction;
    }

    public override void Close(bool doCloseSound = true)
    {
        branch.BuildingHandler.PostConstructionChanged -= PostConstructionChanged_Building;
        branch.PostApplyBranchInteraction -= PostApplyBranchInteraction;
        base.Close(doCloseSound);
    }

    public override void PostClose()
    {
        base.PostClose();

        ClearConstructCache();
        ClearContractCache();
        ClearInteractionCache();
        curTab = TabType.Construction;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1519f, 904f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(4f);
        float mainInnerRectX = mainInnerRect.xMin;
        float mainInnerRectY = mainInnerRect.yMin;

        Rect reusedRect = new(mainInnerRect.xMax - 21f, mainInnerRectY + 1f, 20f, 20f);
        if (Widgets.ButtonImage(reusedRect, IconLibrary.colseX, doMouseoverSound: true))
        {
            Close();
            return;
        }

        float offsetMainInnerMidX = mainInnerRectX + 824f;

        reusedRect = new(mainInnerRectX + 546f, mainInnerRectY + 171f, 562f, 9f);
        Widgets.FillableBar(reusedRect, Mathf.Clamp01(branch.FacilityHandler.TotalFacilityLevel / (allFacilityDefCount * 4f)), IconLibrary.BarTex_Green, IconLibrary.BarTex_Black, doBorder: false);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(mainInnerRectX + (755f - 128f), mainInnerRectY + 65f, 128f, 32f);
        Widgets.Label(reusedRect, $"{branch.FacilityHandler.TotalFacilityLevel}/{allFacilityDefCount * 4}");

        reusedRect = new(mainInnerRectX + (755f - 192f), reusedRect.yMax + 10f, 192f, 32f);
        Widgets.Label(reusedRect, "OARO_BranchWin_TotalFacilitiesLevel".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = new(mainInnerRectX + 828f, mainInnerRectY + 36f, 241f, 125f);
        DrawStoresReserves(reusedRect);

        //中部区域
        Rect middleRect = new(offsetMainInnerMidX - 579f * 0.5f, mainInnerRectY + 210f, 579f, (538f + 46f));
        DrawMiddleRect(middleRect);

        //左侧区域
        Rect leftRect = new(mainInnerRect.x + 65f, mainInnerRectY + 196f, 392f, 589f);
        DrawLeftRect(leftRect);

        //右侧区域
        Rect rightRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMax + 66f, 305f, 635f);
        DrawRightRect(rightRect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawStoresReserves(Rect inRect)
    {
        IReadOnlyList<BranchStoresReserveHandler.ReserveRecord> storesReserves = branch.StoresReserveHandler.StoresReserves;
        Rect reusedRect;

        if (storesReserves.Count > 0)
        {
            reusedRect = new(inRect.x + 2f, inRect.y + 28f, 82f, 82f);
            reusedRect = reusedRect.ContractedBy(10f);
            GUI.DrawTexture(reusedRect, storesReserves[0].Target.IconTexture);
        }

        if (storesReserves.Count > 1)
        {
            reusedRect = new(inRect.x + 102f, inRect.y + 55f, 55f, 55f);
            reusedRect = reusedRect.ContractedBy(6f);
            GUI.DrawTexture(reusedRect, storesReserves[1].Target.IconTexture);
        }

        if (storesReserves.Count > 2)
        {
            reusedRect = new(inRect.x + 175f, inRect.y + 56f, 55f, 55f);
            reusedRect = reusedRect.ContractedBy(6f);
            GUI.DrawTexture(reusedRect, storesReserves[2].Target.IconTexture);
        }

        reusedRect = new(inRect.xMax - 110f, inRect.y + 10f, 110f, 22f);
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_BranchWin_StoresReservesConstruction".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        reusedRect = new(reusedRect.xMin - 13f, reusedRect.y, 13f, 22f);
        GUI.DrawTexture(reusedRect, IconLibrary.smallExclamation);
    }

    private void DrawMiddleRect(Rect inRect)
    {
        float tabRectWidth = inRect.width / 3f;
        Rect constructionTabRect = new(inRect.x, inRect.y, tabRectWidth, 45f);
        if (OARO_WindowUtility.TextButtonImage(constructionTabRect, "OARO_BranchWin_ConstructionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Construction);
        }
        Rect demandTabRect = new(constructionTabRect.xMax, inRect.y, tabRectWidth, 45f);
        if (OARO_WindowUtility.TextButtonImage(demandTabRect, "OARO_BranchWin_DemandTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Contract);
        }
        Rect interactionTabRect = new(demandTabRect.xMax, inRect.y, tabRectWidth, 45f);
        if (OARO_WindowUtility.TextButtonImage(interactionTabRect, "OARO_BranchWin_InteractionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Interaction);
        }

        Rect mainRect = inRect;
        mainRect.yMin += 45f;
        switch (curTab)
        {
            case TabType.Construction:
                Widgets.DrawBox(constructionTabRect);
                DrawConstructionTab(mainRect);
                return;
            case TabType.Contract:
                Widgets.DrawBox(demandTabRect);
                DrawContractTab(mainRect);
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
        Widgets.Label(reusedRect, "OARO_BranchWin_Facilities".Translate());

        Rect facilitiesRect = inRect;
        facilitiesRect.yMin = reusedRect.yMax + 3f;
        facilitiesRect.yMax = facilitiesRect.yMin + 225f;
        DrawFacilityList(facilitiesRect);

        reusedRect = inRect;
        reusedRect.yMin = facilitiesRect.yMax + 2f;
        reusedRect.yMax = reusedRect.yMin + 70f;

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_BranchWin_Buildings".Translate());

        reusedRect.xMax -= 12f;
        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, "OARO_BranchWin_BranchBuildingCeiling".Translate() + ": " + $"{cachedBranchInfo.BuildingCeiling}/{BranchStatDefOf.OARO_BuildingCeiling.maxValue:F0}");

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
        float entryWidth = inRect.width / 4f + 0.1f;
        float entryHeight = inRect.height - 20f;
        viewRect.width = facilities.Count * entryWidth;
        viewRect.height = entryHeight;

        Rect scrollRect = inRect;
        scrollRect.yMin = inRect.yMax - 16f;
        GUI.DrawTexture(scrollRect, IconLibrary.BarTex_Black);

        Widgets.BeginScrollView(inRect, ref scrollPosition_Facilities, viewRect);
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
        float levelItemWidth = (inRect.width - 10f) / 4f;
        reusedRect = new(reusedRect.x + 2f, reusedRect.y + 2f, levelItemWidth, 5f);
        for (BranchFacilityLevel i = 0; i < facilityLevel; i++)
        {
            GUI.DrawTexture(reusedRect, facilityLevelItem);
            reusedRect.xMin = reusedRect.xMax + 2f;
            reusedRect.width = levelItemWidth;
        }

        reusedRect = Rect.MinMaxRect(inRect.xMin, reusedRect.yMax, inRect.xMax, reusedRect.yMax + 108f);
        Rect textureRect = OARO_WindowUtility.CenterRect(reusedRect, 96f, 86f);
        GUI.DrawTexture(textureRect, facilityDef.ExpandingIconTexture, ScaleMode.ScaleToFit);

        reusedRect.yMax += 32f;
        reusedRect.yMin = reusedRect.yMax - 32f;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, facilityDef.LabelCap);

        float preMaxY = reusedRect.yMax;
        reusedRect.yMax = inRect.yMax;
        reusedRect.yMin = preMaxY + 2f;
        reusedRect = reusedRect.ContractedBy(2f);
        Widgets.Label(reusedRect, $"OARO_BranchFacilityLevel_{facilityLevel}".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        if (facilityLevel == BranchFacilityLevel.Excellent)
        {
            GUI.DrawTexture(reusedRect, maxFacilityLevelLace, ScaleMode.ScaleToFit);
        }

        if (UnderConstructionFacility?.TargetDef == facilityDef)
        {
            reusedRect = inRect;
            reusedRect.yMin = inRect.yMax - 12f;
            Widgets.FillableBar(reusedRect, UnderConstructionFacility.Progress, IconLibrary.BarTex_White, IconLibrary.BarTex_Black, doBorder: true);
        }

        bool selected = ((curSelectType == SelectType.Facility) && (selFacilityDef == facilityDef));
        if (Widgets.ButtonInvisible(inRect))
        {
            curSelectType = SelectType.Facility;
            if (selected)
            {
                DeselectConstruct();
            }
            else
            {
                selFacilityDef = facilityDef;
                curFacilityStageCache = new(facilityDef, facilityLevel, branch);
                if (facilityLevel < BranchFacilityLevel.Excellent)
                {
                    nextFacilityStageCache = new(facilityDef, facilityLevel.FacilityLevelOffSetBy(1), branch);
                }
                else
                {
                    nextFacilityStageCache = null;
                }
            }
        }
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
        }
    }

    private void DrawBuildingList(Rect inRect)
    {
        BranchBuildingHandler buildingHandler = branch.BuildingHandler;
        UnderConstructionRecord<BranchBuildingDef> underConstructionBuilding = UnderConstructionBuilding;
        bool isBusy = underConstructionBuilding is not null;

        int potentialBuildingCount = 1 + cachedBranchInfo.BuildingCeiling;
        Rect outRect = inRect;
        outRect.xMax -= 16f;
        outRect = outRect.ContractedBy(2f);

        float entryX = outRect.x;
        float entryY = outRect.y;
        float entryHeight = 81f;
        Rect viewRect = outRect;
        viewRect.height = Mathf.Max(Mathf.CeilToInt(potentialBuildingCount / 3f), 2) * entryHeight;
        float entryWidth = viewRect.width / 3f;

        int column = 0;
        Rect entryRect;
        Widgets.BeginScrollView(inRect, ref scrollPosition_Buildings, viewRect);

        AdjustEntryRect();
        if (buildingHandler.SpecialBuilding is null)
        {
            if (isBusy && underConstructionBuilding.TargetDef.isSpecial)
            {
                DrawConstructingBuilding(entryRect);
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

        IReadOnlyList<BranchBuilding> buildings = branch.BuildingHandler.NormalBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            AdjustEntryRect();
            DrawBulding(entryRect, buildings[i], isSpecialSlot: false);
        }

        if (isBusy && !underConstructionBuilding.TargetDef.isSpecial)
        {
            AdjustEntryRect();
            DrawConstructingBuilding(entryRect);
        }

        if (buildings.Count < cachedBranchInfo.BuildingCeiling)
        {
            AdjustEntryRect();
            DrawEmptyBulding(entryRect, isSpecialSlot: false, isBusy: isBusy);
        }
        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
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
        if (Widgets.ButtonInvisible(inRect))
        {
            if (selected)
            {
                DeselectConstruct();
            }
            else
            {
                if (selBuilding != building)
                {
                    selBuilding = building;
                    selBuildingDefCache = new(building.Def, branch);
                }
                curSelectType = SelectType.Building;
            }
        }
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
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
            string buttonLabel = isSpecialSlot ? "OARO_BranchWin_ClickToConstructBuilding_Special".Translate() : "OARO_BranchWin_ClickToConstructBuilding".Translate();
            bool selected = ((curSelectType == SelectType.EmptyBuildingSlot) && (selEmptyBuildingSlotIsSpecial == isSpecialSlot));
            if (selected)
            {
                GUI.DrawTexture(inRect, buildingConstructButton_Down, ScaleMode.ScaleToFit);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(inRect, buttonLabel);
                Widgets.DrawBox(inRect);
                if (Widgets.ButtonInvisible(inRect, doMouseoverSound: true))
                {
                    curSelectType = SelectType.EmptyBuildingSlot;
                    DeselectConstruct();
                }
            }
            else if (OARO_WindowUtility.TextButtonImage(inRect, buttonLabel, buildingConstructButton, buildingConstructButton_Down, doMouseoverSound: true))
            {
                selBuilding = null;
                selBuildingDefCache = null;
                selEmptyBuildingSlotIsSpecial = isSpecialSlot;
                curSelectType = SelectType.EmptyBuildingSlot;
            }
        }
    }

    private void DrawConstructingBuilding(Rect inRect)
    {
        UnderConstructionRecord<BranchBuildingDef> underConstructionBuilding = UnderConstructionBuilding;
        if (underConstructionBuilding is null)
        {
            return;
        }

        Rect reusedRect = new(inRect.x + 2f, inRect.y, inRect.width - 4f, Text.LineHeight);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_BranchWin_Constructing".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, underConstructionBuilding.DurationTicksLeft.ToStringTicksToPeriod());

        BranchBuildingDef buildingDef = underConstructionBuilding.TargetDef;
        reusedRect = inRect.ContractedBy(5f);
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.x + 15f, 40f, 40f);
        GUI.DrawTexture(reusedRect, buildingDef.IconTexture, ScaleMode.ScaleToFit);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 15f, 105f, 24f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, buildingDef.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = new(inRect.x + 2f, inRect.yMax - 12f, inRect.width - 4f, 12f);
        Widgets.FillableBar(reusedRect, underConstructionBuilding.Progress, IconLibrary.BarTex_White, IconLibrary.BarTex_Black, doBorder: true);

        bool selected = curSelectType == SelectType.ConstructingBuilding;
        if (Widgets.ButtonInvisible(inRect))
        {
            if (selected)
            {
                DeselectConstruct();
            }
            else
            {
                selBuildingDefCache = new(buildingDef, branch);
                curSelectType = SelectType.ConstructingBuilding;
            }
        }
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
        }
    }

    private void DrawContractTab(Rect inRect)
    {
        BranchPopulationHandler populationHandler = branch.PopulationHandler;
        IReadOnlyList<(BranchContract, AcceptanceReport)> contractAcceptances = ContractAcceptances;
        int contractCeilingByPop = populationHandler.ContractCeilingByPop;

        float entryX = inRect.xMin;
        float entryY = inRect.yMin;
        float entryWidth = inRect.width - 20f;
        float entryHeight = 135f;

        Rect viewRect = new(entryX, entryY, entryWidth, RatkinOrderSettings.MaxConcurrentContractPerBranch * entryHeight);

        Rect entryRect;
        int contractCount = contractAcceptances.Count;

        Widgets.BeginScrollView(inRect, ref scrollPosition_Contract, viewRect);
        for (int i = 0; i < contractCount; i++)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            GUI.DrawTexture(entryRect, contractBackground, ScaleMode.ScaleToFit);
            entryY += (entryHeight - 2f);
            entryRect.ContractedBy(2f);
            DrawContractEntry(entryRect, contractAcceptances[i].Item1, contractAcceptances[i].Item2);
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        int unlockCount = Mathf.Max(contractCeilingByPop, contractCount);
        if (unlockCount > contractCount)
        {
            for (int i = contractCount; i < contractCeilingByPop; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                GUI.DrawTexture(entryRect, contractBackground);
                entryY += (entryHeight - 2f);
                entryRect = entryRect.ContractedBy(2f);
                GUI.DrawTexture(entryRect, contractShade);
                Widgets.Label(entryRect, "OARO_BranchWin_NoContractNow".Translate());
            }
        }

        if (RatkinOrderSettings.MaxConcurrentContractPerBranch > unlockCount)
        {
            for (int i = unlockCount; i < RatkinOrderSettings.MaxConcurrentContractPerBranch; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                GUI.DrawTexture(entryRect, contractBackground);
                entryY += (entryHeight - 2f);
                entryRect = entryRect.ContractedBy(2f);
                int populationLimit = populationHandler.PopulationLimitByIndex(i);
                GUI.DrawTexture(entryRect, contractShade, ScaleMode.ScaleAndCrop);
                Widgets.Label(entryRect, "OARO_BranchWin_ContractUnlockPop".Translate(populationLimit));
            }
        }
        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawContractEntry(Rect inRect, BranchContract contract, AcceptanceReport acceptance)
    {
        switch (contract.CurState)
        {
            case BranchContract.ContractState.Ongoing or BranchContract.ContractState.Cooling:
                {
                    Text.Font = GameFont.Small;
                    bool cooling = contract.CurState == BranchContract.ContractState.Cooling;
                    Rect reusedRect = new(inRect.x + 20f, inRect.y + 10f, 460f, 48f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(reusedRect, contract.RequestReason);
                    reusedRect = new(inRect.xMax - (20f + 96f), inRect.y + 10f, 96f, 24f);
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(reusedRect, contract.TicksToExpire.ToStringTicksToPeriod());

                    Text.Anchor = TextAnchor.MiddleCenter;
                    reusedRect = new(inRect.x + 20f, inRect.yMax - (10f + 53f), 116f, 53f);
                    if (cooling)
                    {
                        GUI.DrawTexture(reusedRect, contractButton_Down);
                        Widgets.Label(reusedRect, "Submit".Translate());
                    }
                    else if (acceptance)
                    {
                        if (OARO_WindowUtility.TextButtonImage(reusedRect, "Submit".Translate(), contractButton, contractButton_Down, doMouseoverSound: true))
                        {
                            contract.Fulfill(caravan, branch);
                            contractAcceptanceDirty = true;
                        }
                    }
                    else
                    {
                        GUI.DrawTexture(reusedRect, contractButton_Down);
                        Widgets.Label(reusedRect, "Submit".Translate());
                        if (!string.IsNullOrEmpty(acceptance.Reason) && Mouse.IsOver(reusedRect))
                        {
                            string reason = acceptance.Reason;
                            TooltipHandler.TipRegion(reusedRect, () => reason, 67435700);
                        }
                    }

                    reusedRect = Rect.MinMaxRect(reusedRect.xMax, reusedRect.yMin, inRect.xMax - 20f, reusedRect.yMax);
                    reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.x + 8f, 115f, 33f);
                    GUI.DrawTexture(reusedRect, contractRequestBackground);

                    Rect iconRect = new(reusedRect.x, reusedRect.y, 33f, 33f);
                    Widgets.ThingIcon(iconRect, contract.RequestThingDef);

                    reusedRect.xMin = iconRect.xMax + 2f;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(reusedRect, $"× {contract.RequestCount}");

                    if (cooling)
                    {
                        GUI.DrawTexture(inRect.ContractedBy(2f), contractShade, ScaleMode.StretchToFill);
                        Text.Font = GameFont.Medium;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(inRect, "OARO_BranchWin_ContractCooling".Translate(contract.TicksToExpire.TicksToDays().ToString("0.##"))
                                                                    .Colorize(Color.green));
                    }

                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    return;
                }

            default:
                {
                    GUI.DrawTexture(inRect, contractShade);
                    Text.Font = GameFont.Medium;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(inRect, "OARO_BranchWin_ContractInvalid".Translate()
                                                                .Colorize(ColorLibrary.RedReadable));
                    return;
                }
        }
    }

    private void DrawInteractionTab(Rect inRect)
    {
        Rect reusedRect;

        Rect commonRect = new(inRect.x, inRect.y, 579f, 187f);
        GUI.DrawTexture(commonRect, commonInteractionBackground);
        commonRect = commonRect.ContractedBy(2f);
        reusedRect = commonRect;
        float commonEntryHeight = 70f;

        reusedRect.height = commonRect.height - commonEntryHeight * 2f;
        string label = "OARO_BranchWin_CommonInteraction".Translate();
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, Text.CalcSize(label).x, reusedRect.height);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, label);
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 13f, 22f);
        GUI.DrawTexture(reusedRect, IconLibrary.smallExclamation);

        Rect commonOutRect = commonRect;
        commonOutRect.yMin = commonRect.yMax - commonEntryHeight * 2f;
        commonOutRect = commonOutRect.ContractedBy(2f);

        float entryRectX = commonOutRect.xMin;
        float entryRectY = commonOutRect.yMin;
        float entryRectWidth = commonOutRect.width / 4f;
        float entryRectHeight = commonEntryHeight;

        IReadOnlyList<(BranchInteractionDef, AcceptanceReport)> commonInteractionAcceptances = CommonInteractionAcceptances;
        int rowCount = Mathf.CeilToInt(commonInteractionAcceptances.Count / 4f);
        Rect commonViewRect = commonOutRect;
        commonViewRect.height = rowCount * entryRectHeight;

        int column = 0;
        Rect entryRect;
        Widgets.BeginScrollView(commonOutRect, ref scrollPosition_CommonInteraction, commonViewRect, showScrollbars: false);
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < commonInteractionAcceptances.Count; i++)
        {
            entryRect = new(entryRectX, entryRectY, entryRectWidth, entryRectHeight);
            column++;
            if (column >= 4)
            {
                entryRectX = commonOutRect.xMin;
                entryRectY += entryRectHeight;
            }
            else
            {
                entryRectX += entryRectWidth;
            }

            (BranchInteractionDef interactionDef, AcceptanceReport acceptance) = commonInteractionAcceptances[i];
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: entryRect,
                label: interactionDef.label,
                acceptance: acceptance,
                baseTex: commonInteractionButton,
                downTex: commonInteractionButton_Down,
                doMouseoverSound: true))
            {
                interactionDef.Worker.TryApplyInteraction(new BranchInteractionParms(branch, caravan));
                break;
            }

        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndScrollView();

        reusedRect = new(inRect.x, commonRect.yMax + 2f, commonRect.width, 155f);
        Rect ratkinTexRect = OARO_WindowUtility.CenterRect(reusedRect, 80f, 85f);
        GUI.DrawTexture(ratkinTexRect, interactionRatkinTexture);

        Rect buildingRect = new(inRect.x, reusedRect.yMax, 579f, 164f);
        GUI.DrawTexture(buildingRect, buildingInteractionBackground);
        buildingRect = buildingRect.ContractedBy(2f);

        reusedRect = buildingRect;
        reusedRect.height = 43f;
        Text.Anchor = TextAnchor.MiddleCenter;
        label = "OARO_BranchWin_BuildingInteraction".Translate();
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, Text.CalcSize(label).x, reusedRect.height);
        Widgets.Label(reusedRect, label);
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 13f, 22f);
        GUI.DrawTexture(reusedRect, IconLibrary.smallExclamation);

        Rect buildingOutRect = Rect.MinMaxRect(buildingRect.x, buildingRect.yMin + 45f, buildingRect.xMax, buildingRect.yMax);

        IReadOnlyList<(BranchBuildingComp_Interaction, AcceptanceReport)> buildingInteractionAcceptances = BuildingInteractionAcceptances;
        Rect buildingViewRect = buildingOutRect;
        buildingViewRect.xMax -= 16f;
        entryRectX = buildingViewRect.xMin;
        entryRectY = buildingViewRect.yMin;
        entryRectWidth = buildingViewRect.width;
        entryRectHeight = 53f;
        buildingViewRect.height = buildingInteractionAcceptances.Count * entryRectHeight;

        Widgets.BeginScrollView(buildingOutRect, ref scrollPosition_BuildingInteraction, buildingViewRect);
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < buildingInteractionAcceptances.Count; i++)
        {
            entryRect = new(entryRectX, entryRectY, entryRectWidth, entryRectHeight);
            entryRectY += entryRectHeight;
            DrawBuildingInteractionEntry(entryRect, buildingInteractionAcceptances[i].Item1, buildingInteractionAcceptances[i].Item2);
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndScrollView();
    }

    private void DrawBuildingInteractionEntry(Rect inRect, BranchBuildingComp_Interaction interactionComp, AcceptanceReport acceptance)
    {
        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.x + 15f, 36f, 36f);
        GUI.DrawTexture(reusedRect, interactionComp.Parent.Def.IconTexture, ScaleMode.ScaleToFit);

        Widgets.Label(inRect, interactionComp.InteractionLabel);

        reusedRect = new(inRect.xMax - 72f, inRect.y, 72f, inRect.height);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_BranchWin_Interaction".Translate(),
            acceptance: acceptance,
            baseTex: buildingInteractionButton,
            downTex: buildingInteractionButton_Down,
            doMouseoverSound: true))
        {
            interactionComp.TryApplyInteraction(caravan);
        }
    }

    private void DrawLeftRect(Rect inRect)
    {
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

        Widgets.TextArea(textRect, "", readOnly: true);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        reusedRect = new(inRect.x, textRect.yMax + 2f, 264f, 36f);
        Widgets.Label(reusedRect, "OARO_BranchWin_Population".Translate() + $"   {branch.PopulationHandler.Population}");
        reusedRect = new(inRect.x, reusedRect.yMax + 2f, 264f, 36f);
        Widgets.Label(reusedRect, "OARO_BranchWin_PopulationCeiling".Translate() + $"   {cachedBranchInfo.PopulationCeiling}");

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + (2f + 10f), textRect.yMax + (2f + 10f), 90f, 24f);
        Widgets.Label(reusedRect, "OARO_BranchWin_DailyChange".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Medium;
        reusedRect = new(inRect.xMax - (12f + 100f), reusedRect.yMax + 2f, 100f, 24f);
        Widgets.Label(reusedRect, "OARO_NumberRangePeople".Translate(cachedBranchInfo.DailyPopulationGrowth_Bottom.ToString(), cachedBranchInfo.DailyPopulationGrowth_Ceiling.ToString())
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
        float inRectX = inRect.xMin;
        Rect reusedRect = new(inRectX + 10f, inRect.y + 32f, 105f, 96f);
        GUI.DrawTexture(reusedRect, selFacilityDef.ExpandingIconTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(reusedRect.xMax + 4f, reusedRect.y, 185f, 48f);
        Widgets.Label(reusedRect, selFacilityDef.LabelCap);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;
        reusedRect = new(reusedRect.x, reusedRect.yMax, 185f, 48f);
        Widgets.Label(reusedRect, selFacilityDef.description);

        float commonWidth = 298f;
        float stageRectHeight = 24f + 2f + 158f;
        Rect descRect = new(inRectX, reusedRect.yMax + 36f, commonWidth, stageRectHeight);
        if (curFacilityStageCache is not null)
        {
            TaggedString label = "OARO_BranchWin_CurFacilityStage".Translate();
            if (curFacilityStageCache.Level == BranchFacilityLevel.Excellent)
            {
                label += " (Max)";
            }
            reusedRect = descRect;
            reusedRect.height = 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, label);
            DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 2f), label, curFacilityStageCache.StageEffectDesc, ref scrollPosition_CurFacilityStage);
        }

        descRect = new(inRectX, descRect.yMax + 48f, commonWidth, stageRectHeight);
        if (nextFacilityStageCache is not null)
        {
            TaggedString label = "OARO_BranchWin_NextFacilityStage".Translate();
            reusedRect = descRect;
            reusedRect.height = 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, label);
            DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 2f), label, nextFacilityStageCache.StageEffectDesc, ref scrollPosition_NextFacilityStage);
        }

        DrawRight_FacilityBottom(new Vector2(inRectX, descRect.yMax + 16f));

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    /// <summary>
    /// 长298f, 宽78f
    /// </summary>
    private void DrawRight_FacilityBottom(Vector2 position)
    {
        float inRectWidth = 298f;
        float inRectHeight = 24f + 24f + 2f + 28f;
        Rect inRect = new(position.x, position.y, inRectWidth, inRectHeight);
        float inRectX = inRect.x;

        if (curFacilityStageCache.Level >= BranchFacilityLevel.Excellent)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, "OARO_FacilityAlreadyAtMaxLevel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return;
        }

        Rect reusedRect;
        BranchFacilityHandler facilityHandler = branch.FacilityHandler;
        if (facilityHandler.IsBusy)
        {
            UnderConstructionRecord<BranchFacilityDef> underConstructionFacility = UnderConstructionFacility;
            if (underConstructionFacility?.TargetDef == selFacilityDef)
            {
                reusedRect = new(inRectX, inRect.y, inRect.width, 24f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(reusedRect, "OARO_BranchWin_Constructing".Translate());
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(reusedRect, underConstructionFacility.DurationTicksLeft.ToStringTicksToPeriod());

                reusedRect = new(inRectX, reusedRect.yMax, inRectWidth, 24f);
                Widgets.FillableBar(reusedRect, underConstructionFacility.Progress, IconLibrary.BarTex_White, IconLibrary.BarTex_Black, doBorder: true);

                reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.yMax - 28f, 89f, 28f);
                Text.Anchor = TextAnchor.MiddleCenter;
                if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_BranchWin_CancelConstruct".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
                {
                    Dialog_NodeTree dialog_Node = OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
                         text: "OARO_BranchWin_CancelConstructWarnning".Translate(),
                         acceptAction: facilityHandler.CancelFacilityConstruction);
                    Find.WindowStack.Add(dialog_Node);
                }
            }
            else
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(inRect, "OARO_OtherFacilityConstructing".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
        }
        else if (nextFacilityStageCache is not null)
        {
            Rect textRect = new(inRectX, inRect.y, inRectWidth, 24f);
            reusedRect = textRect;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(reusedRect, "OARO_BranchWin_ExpectedCost".Translate());
            reusedRect.xMin += 0.33f * inRectWidth;
            Widgets.Label(reusedRect, nextFacilityStageCache.TimeCost.TicksToDays().ToString() + "Day".Translate());

            float silverCostWidth = Text.CalcSize($"× {nextFacilityStageCache.SilverCost}").x;
            reusedRect = textRect;
            reusedRect.xMin = textRect.xMax - silverCostWidth;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, $"× {nextFacilityStageCache.SilverCost}");

            reusedRect = new(reusedRect.xMin - 24f, reusedRect.y, 24f, 24f);
            Widgets.ThingIcon(reusedRect, ThingDefOf.Silver, graphicIndexOverride: 2);

            reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.yMax - 28f, 89f, 28f);
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_BranchWin_StartConstruct".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
            {
                AcceptanceReport acceptance = facilityHandler.CanConstructFacility(selFacilityDef, byPlayer: true, caravan: caravan, resultOnly: false);
                if (acceptance)
                {
                    facilityHandler.StartFacilityConstruction(selFacilityDef, byPlayer: true, caravan: caravan);
                }
                else
                {
                    Messages.Message("OARO_CanNotStartFacilityConstruction".Translate(acceptance.Reason), MessageTypeDefOf.RejectInput, historical: false);
                }
            }
        }
    }

    private void DrawRight_Building(Rect inRect)
    {
        BranchBuildingDef buildingDef;
        string buildingLabel;
        if (curSelectType == SelectType.Building)
        {
            if (selBuilding is null)
            {
                return;
            }
            buildingDef = selBuilding.Def;
            buildingLabel = selBuilding.Label;
        }
        else
        {
            if (UnderConstructionBuilding is null)
            {
                return;
            }
            buildingDef = UnderConstructionBuilding.TargetDef;
            buildingLabel = buildingDef.LabelCap;
        }

        if (selBuildingDefCache.BuildingDef != buildingDef)
        {
            selBuildingDefCache = new BranchBuildingDefSummaryUICache(buildingDef, branch);
        }

        float inRectX = inRect.x;

        Rect reusedRect = new(inRectX + 12f, inRect.y + 75f, 105f, 96f);
        GUI.DrawTexture(reusedRect, buildingDef.ExpandingIconTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(reusedRect.xMax + 4f, reusedRect.y, 185f, 48f);
        Widgets.Label(reusedRect, buildingLabel);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;
        reusedRect = new(reusedRect.x, reusedRect.yMax, 185f, 48f);
        Widgets.Label(reusedRect, buildingDef.description);

        float commonWidth = 298f;

        Rect descRect = DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 32f), "OARO_BranchWin_BuildingBaseEffect".Translate(), selBuildingDefCache.BaseEffectDesc, ref scrollPosition_BuildingBaseEffect);

        if (buildingDef.IsUpgradable)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX, descRect.yMax + 8f, commonWidth, 48f);
            Widgets.Label(reusedRect, "OARO_BranchWin_BuildingUpgradePopGE".Translate(buildingDef.advancedProperties.advancedPopulation.ToString()));

            descRect = DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 8f), "OARO_BranchWin_BuildingAdvancedEffect".Translate(), selBuildingDefCache.AdvancedEffectDesc, ref scrollPosition_BuildingAdvancedEffect);
        }

        if (curSelectType == SelectType.ConstructingBuilding)
        {
            DrawRight_ConstructingBuildingBottom(new(inRectX, descRect.yMax + 16f));
        }
    }

    /// <summary>
    /// 长：298f, 宽：xx
    /// </summary>
    private void DrawRight_ConstructingBuildingBottom(Vector2 position)
    {
        UnderConstructionRecord<BranchBuildingDef> underConstructionBuilding = UnderConstructionBuilding;
        if (underConstructionBuilding is null)
        {
            return;
        }

        float inRectX = position.x;
        float inRectWidth = 298f;
        float inRectHeight = 24f + 24f + 2f + 28f;
        Rect inRect = new(position.x, position.y, inRectWidth, inRectHeight);

        Rect reusedRect = inRect;
        reusedRect.height = 24f;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_BranchWin_Constructing".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, underConstructionBuilding.DurationTicksLeft.ToStringTicksToPeriod());

        reusedRect = new(inRectX, reusedRect.yMax, inRectWidth, 24f);
        Widgets.FillableBar(reusedRect, underConstructionBuilding.Progress, IconLibrary.BarTex_White, IconLibrary.BarTex_Black, doBorder: true);

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.yMax - 28f, 89f, 28f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_BranchWin_CancelConstruct".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
        {
            Dialog_NodeTree dialog_Node = OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
                text: "OARO_BranchWin_CancelConstructWarnning".Translate(),
                acceptAction: branch.BuildingHandler.CancelBuildingConstruction);
            Find.WindowStack.Add(dialog_Node);
        }
    }

    private void DrawRight_EmptyBuildingSlot(Rect inRect)
    {
        if (!selEmptyBuildingSlotIsSpecial.HasValue)
        {
            return;
        }
        bool isSpecialSlot = selEmptyBuildingSlotIsSpecial.Value;

        float inRectX = inRect.xMin;
        Rect optionalOutRect = new(inRectX, inRect.y + 75f, 295f, 372f);
        GUI.DrawTexture(optionalOutRect, optionalBuildingBackground);
        optionalOutRect = optionalOutRect.ContractedBy(2f);

        DrawOptionalBuildingList(optionalOutRect, isSpecialSlot);

        if (selBuildingDefCache is null)
        {
            return;
        }

        Rect detailRect = inRect;
        detailRect.yMin = optionalOutRect.yMax + 65f;
        DrawOptionalBuildingDetail(detailRect, isSpecialSlot);
    }

    private void DrawOptionalBuildingList(Rect optionalOutRect, bool isSpecialSlot)
    {
        Rect optionalViewRect = optionalOutRect;
        optionalViewRect.xMax -= 16f;

        float entryX = optionalViewRect.xMin;
        float entryY = optionalViewRect.yMin;
        float entryWidth = optionalViewRect.width;
        float entryHeight = 96f;
        Rect entryRect;

        Dictionary<BranchBuildingDef, BranchBuildingDefSummaryUICache> optionalBuildingDefs = OptionalBuildingDefs;
        optionalViewRect.height = entryHeight * optionalBuildingDefs.Count;

        Widgets.BeginScrollView(optionalOutRect, ref scrollPosition_OptionalBuildings, optionalViewRect);
        int index = 0;
        foreach (BranchBuildingDefSummaryUICache summaryUICache in optionalBuildingDefs.Values.Where(v => v.BuildingDef.isSpecial == isSpecialSlot))
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            index++;
            if ((index & 1) == 0)
            {
                GUI.DrawTexture(entryRect, optionalBuildingEntry_Dark);
            }

            if (DrawOptionalBuildingEntry(entryRect, summaryUICache))
            {
                if (selBuildingDefCache?.BuildingDef != summaryUICache.BuildingDef)
                {
                    selBuildingDefCache = new BranchBuildingDefSummaryUICache(summaryUICache.BuildingDef, branch);
                }
            }
        }
        optionalViewRect.yMax = entryY;
        Widgets.EndScrollView();
    }

    private void DrawOptionalBuildingDetail(Rect inRect, bool isSpecialSlot)
    {
        Rect reusedRect = inRect;
        reusedRect.height = 24f;
        Widgets.Label(reusedRect, "Description".Translate());

        reusedRect.yMin = reusedRect.yMax;
        reusedRect.yMax += 2f;
        GUI.DrawTexture(reusedRect, optionalBuildingDescCuttingLine);

        reusedRect = new(reusedRect.x, reusedRect.yMax + 8f, reusedRect.width, 80f);
        Widgets.TextArea(reusedRect, selBuildingDefCache.BuildingDef.description, readOnly: true);

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 2f, 88f, 29f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_BranchWin_StartConstruct".Translate(), constructButton, constructButton_Down))
        {
            BranchBuildingConstructParms constructParameter = new(branch, selBuildingDefCache.BuildingDef)
            {
                ByPlayer = true,
                Caravan = caravan
            };
            AcceptanceReport acceptanceReport = branch.BuildingHandler.CanConstructBuilding(constructParameter);
            if (acceptanceReport)
            {
                branch.BuildingHandler.StartBuildingConstruction(constructParameter);
            }
            else
            {
                Messages.Message("OARO_CanNotStartBuildingConstruction".Translate(acceptanceReport.Reason), MessageTypeDefOf.RejectInput, historical: false);
            }
        }
    }

    private bool DrawOptionalBuildingEntry(Rect inRect, BranchBuildingDefSummaryUICache summaryUICache)
    {
        BranchBuildingDef buildingDef = summaryUICache.BuildingDef;

        Rect reusedRect = new(inRect.x + 8f, inRect.y, inRect.height, inRect.height);
        float textXMin = reusedRect.xMax;
        reusedRect = reusedRect.ContractedBy(12f);
        GUI.DrawTexture(reusedRect, buildingDef.IconTexture, ScaleMode.ScaleToFit);

        float textHeight = inRect.height / 4f;
        float textWidth = inRect.xMax - textXMin;
        reusedRect = new(textXMin, inRect.y, textWidth, textHeight);

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, buildingDef.label);

        List<string> baseEffectDesc = summaryUICache.BaseEffectDesc;
        if (baseEffectDesc.Count > 0)
        {
            reusedRect = new(textXMin, reusedRect.yMax, textWidth, textHeight);
            Widgets.Label(reusedRect, baseEffectDesc[0]);
            if (baseEffectDesc.Count > 1)
            {
                reusedRect = new(textXMin, reusedRect.yMax, textWidth, textHeight);
                Widgets.Label(reusedRect, baseEffectDesc[1]);
            }
            if (baseEffectDesc.Count > 2)
            {
                Rect tipTriggerRect = new(textXMin, inRect.y + textHeight, textWidth, 2 * textHeight);
                if (Mouse.IsOver(tipTriggerRect))
                {
                    string detailDesc = summaryUICache.BaseEffectDescJoint;
                    if (!string.IsNullOrEmpty(detailDesc))
                    {
                        TooltipHandler.TipRegion(reusedRect, () => detailDesc, 64130862);
                    }
                }
            }
        }

        reusedRect = new(textXMin, inRect.yMax - textHeight, textWidth, textHeight);
        reusedRect.width /= 2f;
        Widgets.Label(reusedRect, summaryUICache.TimeCost.TicksToDays().ToString("0.#") + "Day".Translate());

        float textSize = Text.CalcSize($"× {summaryUICache.SilverCost}").x;
        reusedRect = new(inRect.xMax - (textSize + 4f), reusedRect.y, textSize, textHeight);
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, $"× {summaryUICache.SilverCost}");
        reusedRect = new(reusedRect.xMin - textHeight, reusedRect.y, textHeight, textHeight);
        reusedRect = reusedRect.ContractedBy(2f);
        Widgets.ThingIcon(reusedRect, ThingDefOf.Silver, graphicIndexOverride: 2);

        Text.Anchor = TextAnchor.UpperLeft;

        if (selBuildingDefCache?.BuildingDef == buildingDef)
        {
            Widgets.DrawHighlight(inRect);
        }
        return Widgets.ButtonInvisible(inRect);
    }

    /// <summary>
    /// 长298f, 宽158f
    /// </summary>
    private Rect DrawEffectDescriptions(Vector2 position, string title, List<string> stageEffectDesc, ref Vector2 scrollPosition)
    {
        Rect rect = new(position.x, position.y, 298f, 158f);
        Rect inRect = rect;

        GUI.DrawTexture(inRect, effectDescBackground);

        Rect viewRect = inRect;
        float entryX = viewRect.xMin + 2f;
        float entryY = viewRect.yMin;
        float entryWidth = viewRect.width - 5f;
        float entryHeight = 26f;
        int entryCount = stageEffectDesc.Count;
        int useCount = Mathf.Max(6, entryCount);
        viewRect.height = entryHeight * useCount;

        Rect entryRect;
        int column = 0;

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, showScrollbars: false);


        entryRect = new(entryX, entryY, entryWidth, entryHeight);
        column++;
        entryY += entryHeight;
        if ((column & 1) == 0)
        {
            GUI.DrawTexture(entryRect, effectDescEntry_Dark);
        }
        Widgets.Label(entryRect, title);


        for (int i = 0; i < entryCount; i++)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            column++;
            entryY += entryHeight;
            if ((column & 1) == 0)
            {
                GUI.DrawTexture(entryRect, effectDescEntry_Dark);
            }
            Widgets.Label(entryRect, stageEffectDesc[i]);
        }

        if (useCount > entryCount)
        {
            for (int i = entryCount; i < useCount; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                column++;
                entryY += entryHeight;
                if ((column & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, effectDescEntry_Dark);
                }
            }
        }
        Widgets.EndScrollView();
        Text.Anchor = TextAnchor.UpperLeft;

        return rect;
    }

    private void SwitchTab(TabType tabType)
    {
        if (curTab == tabType)
        {
            return;
        }
        DeselectConstruct();
        switch (tabType)
        {
            case TabType.Construction:
                ClearConstructCache();
                break;
            case TabType.Interaction:
                ClearInteractionCache();
                break;
            default: break;
        }
        curTab = tabType;
    }

    private void DeselectConstruct()
    {
        SelectType oldSelectType = curSelectType;
        curSelectType = SelectType.None;
        switch (oldSelectType)
        {
            case SelectType.Facility:
                selFacilityDef = null;
                curFacilityStageCache = null;
                nextFacilityStageCache = null;
                break;
            case SelectType.Building:
                selBuilding = null;
                selBuildingDefCache = null;
                break;
            case SelectType.ConstructingBuilding:
                selBuildingDefCache = null;
                break;
            case SelectType.EmptyBuildingSlot:
                selEmptyBuildingSlotIsSpecial = null;
                selBuildingDefCache = null;
                break;
            default:
                break;
        }
    }

    private void ClearConstructCache()
    {
        curSelectType = SelectType.None;

        selBuilding = null;
        selBuildingDefCache = null;

        selFacilityDef = null;
        curFacilityStageCache = null;
        nextFacilityStageCache = null;

        selEmptyBuildingSlotIsSpecial = null;
        optionalBuildingDefs = null;
    }

    private void ClearContractCache()
    {
        contractAcceptanceDirty = true;
        contractAcceptances.Clear();
    }

    private void ClearInteractionCache()
    {
        interactionAcceptanceDirty = true;
        commonInteractionAcceptances.Clear();
        buildingInteractionAcceptances.Clear();
    }

    private void RecacheOptionalBuildingDefs()
    {
        optionalBuildingDefs = new(Mathf.RoundToInt(DefDatabase<BranchBuildingDef>.DefCount * 0.5f));
        BranchBuildingHandler buildingHandler = branch.BuildingHandler;
        HashSet<BranchBuildingDef> existBuildingDefs = buildingHandler.NormalBuildings.Select(b => b.Def).ToHashSet();
        if (buildingHandler.SpecialBuilding is not null)
        {
            existBuildingDefs.Add(buildingHandler.SpecialBuilding.Def);
        }
        if (buildingHandler.UnderConstructionBuilding is not null)
        {
            existBuildingDefs.Add(buildingHandler.UnderConstructionBuilding.TargetDef);
        }
        foreach (BranchBuildingDef buildingDef in DefDatabase<BranchBuildingDef>.AllDefs)
        {
            if (!existBuildingDefs.Contains(buildingDef))
            {
                optionalBuildingDefs.Add(buildingDef, new BranchBuildingDefSummaryUICache(buildingDef, branch));
            }
        }
    }

    private void RecacheContractAcceptance()
    {
        contractAcceptanceDirty = false;
        contractAcceptances.Clear();
        IReadOnlyList<BranchContract> contracts = branch.PopulationHandler.Contracts;
        foreach (BranchContract contract in contracts)
        {
            AcceptanceReport acceptanceReport;
            try
            {
                acceptanceReport = contract.CanFulfill(caravan);
            }
            catch
            {
                acceptanceReport = false;
            }
            contractAcceptances.Add((contract, acceptanceReport));
        }
    }

    private void RecacheInteractionAcceptance()
    {
        interactionAcceptanceDirty = false;
        commonInteractionAcceptances.Clear();
        foreach (BranchInteractionDef interactionDef in DefDatabase<BranchInteractionDef>.AllDefs.Where(d => !d.onlyBuildingInteraction))
        {
            AcceptanceReport acceptanceReport;
            try
            {
                acceptanceReport = interactionDef.Worker.CanUseInteraction(new BranchInteractionParms(branch, caravan), resultOnly: false);
            }
            catch
            {
                acceptanceReport = false;
            }
            commonInteractionAcceptances.Add((interactionDef, acceptanceReport));
        }

        buildingInteractionAcceptances.Clear();
        foreach (BranchBuildingComp_Interaction interactionComp in branch.BuildingHandler.InteractionComps)
        {
            AcceptanceReport acceptanceReport;
            try
            {
                acceptanceReport = interactionComp.CanUseInteraction(caravan, resultOnly: false);
            }
            catch
            {
                acceptanceReport = false;
            }
            buildingInteractionAcceptances.Add((interactionComp, acceptanceReport));
        }
    }

    private void PostConstructionChanged_Building(BranchBuildingDef buildingDef, bool added)
    {
        if (curSelectType == SelectType.ConstructingBuilding)
        {
            DeselectConstruct();
        }
        if (curSelectType == SelectType.EmptyBuildingSlot)
        {
            DeselectConstruct();
            if (UnderConstructionBuilding?.TargetDef == buildingDef)
            {
                selBuildingDefCache = new BranchBuildingDefSummaryUICache(buildingDef, branch);
                curSelectType = SelectType.ConstructingBuilding;
            }
        }

        if (optionalBuildingDefs is null)
        {
            return;
        }
        if (added)
        {
            optionalBuildingDefs.Remove(buildingDef);
        }
        else
        {
            optionalBuildingDefs[buildingDef] = new BranchBuildingDefSummaryUICache(buildingDef, branch);
        }
    }

    private void PostApplyBranchInteraction(BranchInteractionDef interactionDef, BranchInteractionParms parms, bool succeeded) => interactionAcceptanceDirty = true;

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MainBackground");

    private static readonly Texture2D middleTopButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MiddleTopButton");
    private static readonly Texture2D middleTopButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MiddleTopButton_Down");

    private static readonly Texture2D facilityLevelBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_FacilityLevelBackground");
    private static readonly Texture2D facilityLevelItem = ContentFinder<Texture2D>.Get("UI/Branch/OARO_FacilityLevelItem");
    private static readonly Texture2D maxFacilityLevelLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_MaxFacilityLevelLace");

    //中部Interaction交互
    private static readonly Texture2D commonInteractionBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_CommonInteractionBackground");
    private static readonly Texture2D commonInteractionButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_CommonInteractionButton");
    private static readonly Texture2D commonInteractionButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_CommonInteractionButton_Down");
    private static readonly Texture2D interactionRatkinTexture = ContentFinder<Texture2D>.Get("UI/Branch/OARO_InteractionRatkinTexture");
    private static readonly Texture2D buildingInteractionBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingInteractionBackground");
    private static readonly Texture2D buildingInteractionButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingInteractionButton");
    private static readonly Texture2D buildingInteractionButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingInteractionButton_Down");

    //中部合约交互
    private static readonly Texture2D contractBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ContractBackground");
    private static readonly Texture2D contractShade = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ContractShade");

    private static readonly Texture2D contractButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ContractButton");
    private static readonly Texture2D contractButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ContractButton_Down");
    private static readonly Texture2D contractRequestBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ContractRequestBackground");

    //右侧建设信息
    private static readonly Texture2D buildingConstructButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingConstructButton");
    private static readonly Texture2D buildingConstructButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingConstructButton_Down");
    private static readonly Texture2D upgradedBuildingLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_UpgradedBuildingLace");
    private static readonly Texture2D specialBuildingLace = ContentFinder<Texture2D>.Get("UI/Branch/OARO_SpecialBuildingLace");

    private static readonly Texture2D constructionBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructionBackground");
    private static readonly Texture2D facilityRect = ContentFinder<Texture2D>.Get("UI/Branch/OARO_FacilityRect");
    private static readonly Texture2D buildingRect = ContentFinder<Texture2D>.Get("UI/Branch/OARO_BuildingRect");

    private static readonly Texture2D effectDescBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_EffectDescBackground");
    private static readonly Texture2D effectDescEntry_Dark = ContentFinder<Texture2D>.Get("UI/Branch/OARO_EffectDescEntry_Dark");
    private static readonly Texture2D constructButton = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructButton");
    private static readonly Texture2D constructButton_Down = ContentFinder<Texture2D>.Get("UI/Branch/OARO_ConstructButton_Down");

    private static readonly Texture2D optionalBuildingBackground = ContentFinder<Texture2D>.Get("UI/Branch/OARO_OptionalBuildingBackground");
    private static readonly Texture2D optionalBuildingEntry_Dark = ContentFinder<Texture2D>.Get("UI/Branch/OARO_OptionalBuildingEntry_Dark");

    private static readonly Texture2D optionalBuildingDescCuttingLine = ContentFinder<Texture2D>.Get("UI/Branch/OARO_OptionalBuildingDescCuttingLine");

    private static readonly Texture2D leftTopSiteIcon = ContentFinder<Texture2D>.Get("UI/Branch/OARO_LeftTopSiteIcon");
}