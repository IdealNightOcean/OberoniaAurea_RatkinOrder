using NightOcean;
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

    private Branch Branch { get; }
    private BranchFacilityHandler FacilityHandler { get; }
    private BranchBuildingHandler BuildingHandler { get; }
    private BranchInfoUICache CachedBranchInfo { get; set; }
    private Caravan Caravan { get; }
    private Map Map { get; }

    private TabType CurTab { get; set; } = TabType.Construction;

    private SelectType CurSelectType { get; set; } = SelectType.None;
    private int AllFacilityDefCount { get; }

    /*
        建设部分缓存
    */
    private BranchBuilding SelBuilding { get; set; }
    private UnderConstructionRecord<BranchBuildingDef> SelUnderConstructionBuilding { get; set; }
    private BranchBuildingDefSummaryUICache SelBuildingDefCache { get; set; }
    private LazyMutable<AcceptanceReport> BuildingConstructionAcceptance { get; }

    private BranchFacilityDef SelFacilityDef { get; set; }
    private BranchFacilityStageSummaryUICache CurFacilityStageCache { get; set; }
    private BranchFacilityStageSummaryUICache NextFacilityStageCache { get; set; }
    private LazyMutable<AcceptanceReport> FacilityConstructionAcceptance { get; }

    private LazyMutable<Dictionary<BranchBuildingDef, BranchBuildingDefSummaryUICache>> OptionalBuildingDefs { get; }
    private LazyMutable<int> OptionalSpecialBuildingCount { get; }

    private Vector2 scrollPosition_GreetingDesc;

    private Vector2 scrollPosition_Facilities;
    private Vector2 scrollPosition_FacilityDesc;
    private Vector2 scrollPosition_CurFacilityStage;
    private Vector2 scrollPosition_NextFacilityStage;

    private Vector2 scrollPosition_Buildings;
    private Vector2 scrollPosition_BuildingDesc;
    private Vector2 scrollPosition_BuildingBaseEffect;
    private Vector2 scrollPosition_BuildingAdvancedEffect;
    private Vector2 scrollPosition_OptionalBuildings;
    private Vector2 scrollPosition_Contract;

    /*
        需求部分缓存
    */
    private LazyMutable<List<KeyValuePair<BranchContract, AcceptanceReport>>> ContractAcceptances { get; }

    /*
        交互部分缓存
    */
    private readonly List<(BranchInteractionDef, AcceptanceReport)> commonInteractionAcceptances = [];
    private IReadOnlyList<(BranchInteractionDef, AcceptanceReport)> CommonInteractionAcceptances
    {
        get
        {
            if (InteractionAcceptanceDirty)
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
            if (InteractionAcceptanceDirty)
            {
                RecacheInteractionAcceptance();
            }
            return buildingInteractionAcceptances;
        }
    }

    private bool InteractionAcceptanceDirty { get; set; } = true;

    private Vector2 scrollPosition_CommonInteraction;
    private Vector2 scrollPosition_BuildingInteraction;

    private Lazy<string> NaturalPopulationCeilingExplanation { get; }
    private Lazy<string> BuildingCeilingExplanation { get; }

    public Window_Branch(Branch branch, Caravan caravan = null, Map map = null) : base()
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        FacilityHandler = Branch.FacilityHandler;
        BuildingHandler = Branch.BuildingHandler;
        Map = map ?? OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true) ?? Find.CurrentMap;
        Caravan = caravan;
        CachedBranchInfo = new(Branch, Map);

        AllFacilityDefCount = DefDatabase<BranchFacilityDef>.DefCount;

        FacilityConstructionAcceptance = new(refreshFunc: delegate
        {
            if (this.SelFacilityDef is null)
            {
                return false;
            }
            else
            {
                return this.Branch.FacilityHandler.CanConstructFacility(this.SelFacilityDef, byPlayer: true, map: this.Map, resultOnly: false);
            }
        });

        BuildingConstructionAcceptance = new(refreshFunc: delegate
        {
            if (this.SelBuildingDefCache is null)
            {
                return false;
            }
            else
            {
                BranchBuildingConstructParms parms = new(this.Branch, this.SelBuildingDefCache.BuildingDef)
                {
                    ByPlayer = true,
                    Map = Map
                };
                return this.Branch.BuildingHandler.CanConstructBuilding(parms, resultOnly: false);
            }
        });

        OptionalBuildingDefs = new(refreshFunc: delegate
        {
            Dictionary<BranchBuildingDef, BranchBuildingDefSummaryUICache> options = new(DefDatabase<BranchBuildingDef>.DefCount - BuildingHandler.AllBuildingsCount);

            HashSet<BranchBuildingDef> allBuildingDefsHash = BuildingHandler.AllBuildingDefsHash;
            HashSet<BranchBuildingDef> underConstructionBuildingDefs = BuildingHandler.UnderConstructionBuildingDefs;
            foreach (BranchBuildingDef buildingDef in DefDatabase<BranchBuildingDef>.AllDefs)
            {
                if (!allBuildingDefsHash.Contains(buildingDef) && !underConstructionBuildingDefs.Contains(buildingDef))
                {
                    options.Add(buildingDef, new BranchBuildingDefSummaryUICache(buildingDef, Branch));
                }
            }

            return options;
        });

        OptionalSpecialBuildingCount = new(refreshFunc: delegate
        {
            return OptionalBuildingDefs.Value.Values.Count(cache => cache.BuildingDef.isSpecial);
        });

        ContractAcceptances = new(refreshFunc: delegate
        {
            IReadOnlyList<BranchContract> contracts = Branch.PopulationHandler.Contracts;
            List<KeyValuePair<BranchContract, AcceptanceReport>> pairs = [];
            foreach (BranchContract contract in contracts)
            {
                AcceptanceReport acceptance;
                try
                {
                    acceptance = contract.CanFulfill(Caravan, resultOnly: false);
                }
                catch
                {
                    acceptance = false;
                }
                pairs.Add(new KeyValuePair<BranchContract, AcceptanceReport>(contract, acceptance));
            }
            return pairs;
        });

        NaturalPopulationCeilingExplanation = new(valueFactory: () => BranchStatDefOf.OARO_NaturalPopulationCeiling.GetStatModifyExplanation(new BranchStatRequestData(Branch)).explanation);
        BuildingCeilingExplanation = new(valueFactory: () => BranchStatDefOf.OARO_BuildingCeiling.GetStatModifyExplanation(new BranchStatRequestData(Branch)).explanation);
    }

    public override void PreOpen()
    {
        base.PreOpen();
        BindCallbacks();
    }

    public override void PostClose()
    {
        base.PostClose();
        UnbindCallbacks();
        ContractAcceptances.Reset();
        ClearInteractionCache();
        ClearConstructCache();
        OptionalBuildingDefs.MarkDirty();
        OptionalSpecialBuildingCount.MarkDirty();
        CurTab = TabType.Construction;
    }

    private void BindCallbacks()
    {
        BuildingHandler.PostConstructionChanged += PostConstructionChanged_Building;
        Branch.PostApplyBranchInteraction.Register(PostApplyBranchInteraction);
    }

    private void UnbindCallbacks()
    {
        BuildingHandler.PostConstructionChanged -= PostConstructionChanged_Building;
        Branch.PostApplyBranchInteraction.Deregister(PostApplyBranchInteraction);
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_UIUtility.CenterRect(inRect, 1519f, 904f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(4f);
        float mainInnerRectX = mainInnerRect.xMin;
        float mainInnerRectY = mainInnerRect.yMin;

        if (OARO_UIUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }
        if (OARO_UIUtility.DrawBackArrow_Corner(mainInnerRect))
        {
            Window_BranchList branchListWin = new(Branch.RatkinOrder, Map, initWithConstructTab: false);
            Find.WindowStack.Add(branchListWin);
            Close();
            return;
        }

        float offsetMainInnerMidX = mainInnerRectX + 824f;

        Rect reusedRect = new(mainInnerRectX + 546f, mainInnerRectY + 171f, 562f, 9f);
        Widgets.FillableBar(reusedRect, Mathf.Clamp01(FacilityHandler.TotalFacilityLevel / (AllFacilityDefCount * 4f)), OARO_ColorLibrary.GreenTex, BaseContent.BlackTex, doBorder: false);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(mainInnerRectX + (755f - 128f), mainInnerRectY + 65f, 128f, 32f);
        Widgets.Label(reusedRect, $"{FacilityHandler.TotalFacilityLevel}/{AllFacilityDefCount * 4}");

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
        Rect rightRect = OARO_UIUtility.CenterRectOnY(mainInnerRect, middleRect.xMax + 66f, 305f, 635f);
        DrawRightRect(rightRect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawStoresReserves(Rect inRect)
    {
        IReadOnlyList<BranchStoresReserveHandler.ReserveRecord> storesReserves = Branch.StoresReserveHandler.StoresReserves;

        Rect reusedRect = new(inRect.x + 2f, inRect.y + 28f, 82f, 82f);
        DrawEntry(reusedRect, 10f, 0);

        reusedRect = new(inRect.x + 102f, inRect.y + 55f, 55f, 55f);
        DrawEntry(reusedRect, 6f, 1);

        reusedRect = new(inRect.x + 175f, inRect.y + 56f, 55f, 55f);
        DrawEntry(reusedRect, 6f, 2);

        reusedRect = new(inRect.xMax - 110f, inRect.y + 10f, 110f, 22f);
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_BranchWin_StoresReservesConstruction".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        reusedRect = new(reusedRect.xMin - 13f, reusedRect.y, 13f, 22f);
        GUI.DrawTexture(reusedRect, OARO_IconLibrary.SmallExclamation);
        TooltipHandler.TipRegion(reusedRect, () => "OARO_BranchWin_StoresReservesTip".Translate(), uniqueId: 73484661);

        void DrawEntry(Rect entryRect, float iconMargin, int index)
        {
            if (index < storesReserves.Count)
            {
                Rect iconRect = entryRect.ContractedBy(iconMargin);
                BranchStoresReserveHandler.ReserveRecord reserves = storesReserves[index];
                GUI.DrawTexture(iconRect, reserves.Target.iconTexture.Texture);
                string reservesDesc = "OARO_StoresReserve_EffectDesc".Translate(
                    Branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName),
                    reserves.Target.Named("TARGET"),
                    reserves.CostRateReduce.ToStringPercent("0.##").Named("Reduce"));
                if (!String.IsNullOrEmpty(reservesDesc))
                {
                    TooltipHandler.TipRegion(iconRect, () => reservesDesc, uniqueId: 8310234);
                }
            }

            AcceptanceReport acceptance = BranchUtility.CanAssignStoreReserveByPlayer(Branch, resultOnly: false);
            if (acceptance)
            {
                if (Mouse.IsOver(entryRect))
                {
                    Widgets.DrawHighlight(entryRect);
                }

                if (Widgets.ButtonInvisible(entryRect))
                {
                    StoresReservesFloatMenu(Branch, index);
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(acceptance.Reason))
                {
                    TooltipHandler.TipRegion(entryRect, () => acceptance.Reason, uniqueId: 24500513);
                }
            }
        }
    }

    private void StoresReservesFloatMenu(Branch branch, int index)
    {
        List<FloatMenuOption> options = new(32);
        foreach (BranchFacilityDef facilityDef in BranchUtility.GetAllStorableFacilityDefs(branch))
        {
            options.Add(new FloatMenuOption(facilityDef.label, () => StoreAction(facilityDef)));
        }
        foreach (BranchBuildingDef buildingDef in BranchUtility.GetAllStorableBuildingDefs(branch))
        {
            options.Add(new FloatMenuOption(buildingDef.label, () => StoreAction(buildingDef)));
        }
        Find.WindowStack.Add(new FloatMenu(options));

        void StoreAction<T>(T def) where T : BranchConstructionDef, new()
        {
            if (index < branch.StoresReserveHandler.StoresReserves.Count)
            {
                branch.StoresReserveHandler.SetReserve(def, index);
            }
            else
            {
                branch.StoresReserveHandler.AddNewReserve(def);
            }
        }
    }

    private void DrawMiddleRect(Rect inRect)
    {
        float tabRectWidth = inRect.width / 3f;
        Rect constructionTabRect = new(inRect.x, inRect.y, tabRectWidth, 45f);
        if (OARO_UIUtility.TextButtonImage(constructionTabRect, "OARO_BranchWin_ConstructionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Construction);
        }
        Rect demandTabRect = new(constructionTabRect.xMax, inRect.y, tabRectWidth, 45f);
        if (OARO_UIUtility.TextButtonImage(demandTabRect, "OARO_BranchWin_ContractTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Contract);
        }
        Rect interactionTabRect = new(demandTabRect.xMax, inRect.y, tabRectWidth, 45f);
        if (OARO_UIUtility.TextButtonImage(interactionTabRect, "OARO_BranchWin_InteractionTab".Translate(), middleTopButton, middleTopButton_Down))
        {
            SwitchTab(TabType.Interaction);
        }

        Rect mainRect = inRect;
        mainRect.yMin += 45f;
        switch (CurTab)
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
        Widgets.Label(reusedRect, "OARO_BranchWin_BuildingCeiling".Translate() + ": " + $"{BuildingHandler.AllBuildings.Count}/{CachedBranchInfo.BuildingCeiling}");
        TooltipHandler.TipRegion(reusedRect, () => BuildingCeilingExplanation.Value, uniqueId: 86485309);

        float yMin = reusedRect.yMax + 4f;
        reusedRect = inRect;
        reusedRect.yMin = yMin;
        DrawBuildingList(reusedRect);

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private void DrawFacilityList(Rect inRect)
    {
        IReadOnlyDictionary<BranchFacilityDef, BranchFacilityLevel> facilities = FacilityHandler.Facilities;
        Rect viewRect = inRect;
        float entryWidth = inRect.width / 4f + 0.1f;
        float entryHeight = inRect.height - 20f;
        viewRect.width = facilities.Count * entryWidth;
        viewRect.height = entryHeight;

        Rect scrollRect = inRect;
        scrollRect.yMin = inRect.yMax - 16f;
        GUI.DrawTexture(scrollRect, BaseContent.BlackTex);

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
        Rect textureRect = OARO_UIUtility.CenterRect(reusedRect, 96f, 86f);
        GUI.DrawTexture(textureRect, facilityDef.iconTexture.ExpandedTexture, ScaleMode.ScaleToFit);

        reusedRect.yMax += 32f;
        reusedRect.yMin = reusedRect.yMax - 32f;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, facilityDef.LabelCap);

        float preMaxY = reusedRect.yMax;
        reusedRect.yMax = inRect.yMax;
        reusedRect.yMin = preMaxY + 2f;
        reusedRect = reusedRect.ContractedBy(2f);
        Widgets.Label(reusedRect, facilityLevel.GetFacilityLevelLabel());
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        if (facilityLevel == BranchFacilityLevel.Excellent)
        {
            GUI.DrawTexture(reusedRect, maxFacilityLevelLace, ScaleMode.ScaleToFit);
        }

        if (FacilityHandler.UnderConstructionFacilities.TryGetValue(facilityDef, out UnderConstructionRecord<BranchFacilityDef> record))
        {
            reusedRect = inRect;
            reusedRect.yMin = inRect.yMax - 12f;
            Widgets.FillableBar(reusedRect, record.Progress, BaseContent.WhiteTex, BaseContent.BlackTex, doBorder: true);
        }

        TooltipHandler.TipRegion(inRect, () => facilityDef.description ?? string.Empty, uniqueId: 84832792);

        bool selected = ((CurSelectType == SelectType.Facility) && (SelFacilityDef == facilityDef));
        if (Widgets.ButtonInvisible(inRect))
        {
            CurSelectType = SelectType.Facility;
            if (selected)
            {
                DeselectConstruct();
            }
            else
            {
                SelFacilityDef = facilityDef;
                CurFacilityStageCache = new(facilityDef, facilityLevel, Branch);
                FacilityConstructionAcceptance.MarkDirty();
                if (facilityLevel < BranchFacilityLevel.Excellent)
                {
                    NextFacilityStageCache = new(facilityDef, facilityLevel.FacilityLevelOffSetBy(1), Branch);
                }
                else
                {
                    NextFacilityStageCache = null;
                }
            }
        }
        if (selected)
        {
            Widgets.DrawHighlightSelected(inRect);
        }
    }

    private void DrawBuildingList(Rect inRect)
    {
        int potentialBuildingCount = 1 + CachedBranchInfo.BuildingCeiling;
        Rect outRect = inRect;
        outRect.xMax -= 16f;
        outRect = outRect.ContractedBy(2f);

        float entryX = outRect.x;
        float entryY = outRect.y;
        float entryHeight = outRect.height / 2f - 0.001f;
        Rect viewRect = outRect;
        viewRect.height = Mathf.Max(Mathf.CeilToInt(potentialBuildingCount / 3f), 2) * entryHeight;
        float entryWidth = viewRect.width / 3f;

        int column = 0;
        Rect entryRect;
        Widgets.BeginScrollView(inRect, ref scrollPosition_Buildings, viewRect);

        IReadOnlyList<BranchBuilding> buildings = BuildingHandler.AllBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            AdjustEntryRect();
            DrawBulding(entryRect, buildings[i]);
        }

        IReadOnlyList<UnderConstructionRecord<BranchBuildingDef>> underConstructionBuildings = BuildingHandler.UnderConstructionBuildings;
        for (int i = 0; i < underConstructionBuildings.Count; i++)
        {
            AdjustEntryRect();
            DrawConstructingBuilding(entryRect, underConstructionBuildings[i]);
        }

        if (BuildingHandler.HasUnusedSlots)
        {
            AdjustEntryRect();
            DrawEmptyBulding(entryRect);
        }
        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        void AdjustEntryRect()
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            if ((++column) >= 3)
            {
                column = 0;
                entryX = outRect.x;
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

    private void DrawBulding(Rect inRect, BranchBuilding building)
    {
        BranchBuildingDef buildingDef = building.Def;
        Rect innerRect = inRect.ContractedBy(5f);
        if (buildingDef.isSpecial)
        {
            GUI.DrawTexture(innerRect, specialBuildingLace, ScaleMode.ScaleToFit);
            if (buildingDef.honorDef is not null)
            {
                Rect ribbonRect = new(inRect.xMin, inRect.yMin - 2f, inRect.width, 55f);
                Material tintMat = OAFrame_UIUtility.GetTintMaterial(buildingDef.honorDef.color, OARO_IconLibrary.HonorRibbonMask);
                GenUI.DrawTextureWithMaterial(ribbonRect, OARO_IconLibrary.HonorRibbonTex, tintMat);
            }
        }
        else if (building.HasUpgraded)
        {
            GUI.DrawTexture(innerRect, upgradedBuildingLace, ScaleMode.ScaleToFit);
        }

        Rect reusedRect = OARO_UIUtility.CenterRectOnY(innerRect, innerRect.x, 64f, 64f);
        GUI.DrawTexture(reusedRect, buildingDef.iconTexture.Texture, ScaleMode.ScaleToFit);

        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.xMax, 105f, inRect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, building.Label);
        Text.Anchor = TextAnchor.UpperLeft;

        bool selected = (CurSelectType == SelectType.Building) && (SelBuilding.Def == buildingDef);
        if (Widgets.ButtonInvisible(inRect))
        {
            if (selected)
            {
                DeselectConstruct();
            }
            else
            {
                SelBuilding = building;
                if (SelBuildingDefCache?.BuildingDef != buildingDef)
                {
                    SelBuildingDefCache = new(buildingDef, Branch);
                }
                CurSelectType = SelectType.Building;
            }
        }
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
        }
    }

    private void DrawEmptyBulding(Rect inRect)
    {
        bool selected = (CurSelectType == SelectType.EmptyBuildingSlot);
        if (selected)
        {
            GUI.DrawTexture(inRect, buildingConstructButton_Down, ScaleMode.ScaleToFit);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, "OARO_BranchWin_ClickToConstructBuilding".Translate());
            Widgets.DrawBox(inRect);
            if (Widgets.ButtonInvisible(inRect, doMouseoverSound: true))
            {
                CurSelectType = SelectType.EmptyBuildingSlot;
                DeselectConstruct();
            }
        }
        else if (OARO_UIUtility.TextButtonImage(
            butRect: inRect,
            label: "OARO_BranchWin_ClickToConstructBuilding".Translate(),
            baseTex: buildingConstructButton,
            downTex: buildingConstructButton_Down,
            doMouseoverSound: true))
        {
            SelBuilding = null;
            SelBuildingDefCache = null;
            CurSelectType = SelectType.EmptyBuildingSlot;
        }
    }

    private void DrawConstructingBuilding(Rect inRect, UnderConstructionRecord<BranchBuildingDef> constructionRecord)
    {
        Rect reusedRect = new(inRect.x + 2f, inRect.y, inRect.width - 4f, Text.LineHeight);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_BranchWin_Constructing".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, constructionRecord.DurationTicksLeft.ToStringTicksToPeriod());

        BranchBuildingDef buildingDef = constructionRecord.TargetDef;
        reusedRect = inRect.ContractedBy(5f);
        if (buildingDef.isSpecial)
        {
            GUI.DrawTexture(reusedRect, specialBuildingLace, ScaleMode.ScaleToFit);
        }

        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.x, 64f, 64f);
        GUI.DrawTexture(reusedRect, buildingDef.iconTexture.Texture, ScaleMode.ScaleToFit);

        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.xMax, 105f, inRect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, buildingDef.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = new(inRect.x + 2f, inRect.yMax - 12f, inRect.width - 4f, 12f);
        Widgets.FillableBar(reusedRect, constructionRecord.Progress, BaseContent.WhiteTex, BaseContent.BlackTex, doBorder: true);

        bool selected = (CurSelectType == SelectType.ConstructingBuilding) && (SelUnderConstructionBuilding == constructionRecord);
        if (Widgets.ButtonInvisible(inRect))
        {
            if (selected)
            {
                DeselectConstruct();
            }
            else
            {
                SelUnderConstructionBuilding = constructionRecord;
                if (SelBuildingDefCache?.BuildingDef != buildingDef)
                {
                    SelBuildingDefCache = new(buildingDef, Branch);
                }
                CurSelectType = SelectType.ConstructingBuilding;
            }
        }
        if (selected)
        {
            Widgets.DrawHighlight(inRect);
        }
    }

    private void DrawContractTab(Rect inRect)
    {
        BranchPopulationHandler populationHandler = Branch.PopulationHandler;
        List<KeyValuePair<BranchContract, AcceptanceReport>> contractAcceptances = ContractAcceptances.Value;
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
            GUI.DrawTexture(entryRect, contractBackground);
            entryY += (entryHeight - 2f);
            DrawContractEntry(entryRect.ContractedBy(2f), contractAcceptances[i].Key, contractAcceptances[i].Value);
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
                        Widgets.Label(reusedRect, "OAFrame_Submit".Translate());
                    }
                    else if (OARO_UIUtility.TextButtonImageDisableable(
                        butRect: reusedRect,
                        label: "OAFrame_Submit".Translate(),
                        acceptance: acceptance,
                        baseTex: contractButton,
                        downTex: contractButton_Down,
                        doMouseoverSound: true))
                    {
                        contract.Fulfill(Caravan, Branch);
                        ContractAcceptances.MarkDirty();
                    }

                    reusedRect = Rect.MinMaxRect(reusedRect.xMax, reusedRect.yMin, inRect.xMax - 20f, reusedRect.yMax);
                    reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.x + 8f, 115f, 32f);
                    GUI.DrawTexture(reusedRect, contractRequestBackground);

                    Rect iconRect = new(reusedRect.x + 8f, reusedRect.y, 32f, 32f);
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
        reusedRect = OARO_UIUtility.CenterRectOnX(reusedRect, reusedRect.y, Text.CalcSize(label).x, reusedRect.height);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, label);
        Text.Anchor = TextAnchor.UpperLeft;

        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 13f, 22f);
        GUI.DrawTexture(reusedRect, OARO_IconLibrary.SmallExclamation);
        TooltipHandler.TipRegion(reusedRect, () => "OARO_BranchWin_CommonInteractionTip".Translate(), uniqueId: 58990376);

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
            if (OARO_UIUtility.TextButtonImageDisableable(
                butRect: entryRect,
                label: interactionDef.label,
                acceptance: acceptance,
                baseTex: commonInteractionButton,
                downTex: commonInteractionButton_Down,
                doMouseoverSound: true,
                tooltip: interactionDef.description))
            {
                interactionDef.Worker.TryApplyInteraction(new BranchInteractionParms(Branch, Caravan));
                break;
            }

        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndScrollView();

        reusedRect = new(inRect.x, commonRect.yMax + 2f, commonRect.width, 155f);
        Rect ratkinTexRect = OARO_UIUtility.CenterRect(reusedRect, 80f, 85f);
        GUI.DrawTexture(ratkinTexRect, interactionRatkinTexture);

        Rect buildingRect = new(inRect.x, reusedRect.yMax, 579f, 164f);
        GUI.DrawTexture(buildingRect, buildingInteractionBackground);
        buildingRect = buildingRect.ContractedBy(2f);

        reusedRect = buildingRect;
        reusedRect.height = 43f;
        Text.Anchor = TextAnchor.MiddleCenter;
        label = "OARO_BranchWin_BuildingInteraction".Translate();
        reusedRect = OARO_UIUtility.CenterRectOnX(reusedRect, reusedRect.y, Text.CalcSize(label).x, reusedRect.height);
        Widgets.Label(reusedRect, label);
        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 13f, 22f);
        GUI.DrawTexture(reusedRect, OARO_IconLibrary.SmallExclamation);
        TooltipHandler.TipRegion(reusedRect, () => "OARO_BranchWin_BuildingInteractionTip".Translate(), uniqueId: 98968387);

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
            if (DrawBuildingInteractionEntry(entryRect, buildingInteractionAcceptances[i].Item1, buildingInteractionAcceptances[i].Item2))
            {
                break;
            }
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndScrollView();
    }

    private bool DrawBuildingInteractionEntry(Rect inRect, BranchBuildingComp_Interaction interactionComp, AcceptanceReport acceptance)
    {
        Rect reusedRect = OARO_UIUtility.CenterRectOnY(inRect, inRect.x + 15f, 36f, 36f);
        GUI.DrawTexture(reusedRect, interactionComp.Parent.Def.iconTexture.Texture, ScaleMode.ScaleToFit);

        Widgets.Label(inRect, interactionComp.InteractionLabel);

        reusedRect = new(inRect.xMax - 72f, inRect.y, 72f, inRect.height);
        if (OARO_UIUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_BranchWin_Interaction".Translate(),
            acceptance: acceptance,
            baseTex: buildingInteractionButton,
            downTex: buildingInteractionButton_Down,
            doMouseoverSound: true))
        {
            interactionComp.TryApplyInteraction(Caravan);
            return true;
        }
        return false;
    }

    private void DrawLeftRect(Rect inRect)
    {
        Rect reusedRect;
        Rect titleRect = new(inRect.x, inRect.y - (24f + 40f), inRect.width, 40f);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(titleRect.x, titleRect.y - 40f, titleRect.width, 40f);
        Widgets.Label(reusedRect, Branch.RatkinOrder.Name);

        Text.Anchor = TextAnchor.MiddleLeft;
        float textWidth = Text.CalcSize(Branch.Name).x;
        reusedRect = OARO_UIUtility.CenterRectOnX(titleRect, titleRect.y, Mathf.Min(textWidth, 256f), 40f);
        reusedRect.xMax += 12f;
        reusedRect.xMin += 12f;
        Widgets.Label(reusedRect, Branch.Name);
        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, reusedRect.xMin - (40f + 4f), 45f, 45f);
        GUI.DrawTexture(reusedRect, leftTopSiteIcon, ScaleMode.ScaleToFit);
        if (Mouse.IsOver(reusedRect))
        {
            Widgets.DrawHighlight(reusedRect);
            TooltipHandler.TipRegion(reusedRect, () => "OARO_BranchWin_SiteIconTip".Translate(), uniqueId: 5450869);
        }
        if (Widgets.ButtonInvisible(reusedRect))
        {
            if (Branch.BaseSite is not null)
            {
                CameraJumper.TryJumpAndSelect(Branch.BaseSite);
            }
        }

        reusedRect = OARO_UIUtility.DrawBranchSummary(new Vector2(inRect.x, inRect.y), CachedBranchInfo);

        inRect.ContractedBy(2f);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;
        Rect textRect = new(inRect.x, reusedRect.yMax + 2f, inRect.width, 420f);
        Widgets.LabelScrollable(textRect.ContractedBy(18f), Branch.GreetingDesc, ref scrollPosition_GreetingDesc);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        reusedRect = new(inRect.x, textRect.yMax + 2f, 264f, 36f);
        Widgets.Label(reusedRect, "OARO_BranchWin_Population".Translate() + $"   {Branch.PopulationHandler.Population}");
        TooltipHandler.TipRegion(reusedRect, () => "OARO_BranchWin_PopulationTip".Translate(), uniqueId: 5344164);
        reusedRect = new(inRect.x, reusedRect.yMax + 2f, 264f, 36f);
        Widgets.Label(reusedRect, "OARO_BranchWin_PopulationCeiling".Translate() + $"   {CachedBranchInfo.PopulationCeiling}");
        TooltipHandler.TipRegion(reusedRect, () => NaturalPopulationCeilingExplanation.Value, 48614123);

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + (2f + 10f), textRect.yMax + (2f + 10f), 90f, 24f);
        Widgets.Label(reusedRect, "OARO_BranchWin_DailyChange".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        Text.Font = GameFont.Medium;
        reusedRect = new(inRect.xMax - (12f + 100f), reusedRect.yMax + 2f, 100f, 24f);
        int growthBottom = CachedBranchInfo.DailyPopulationGrowth_Bottom.Value;
        int growthCeiling = CachedBranchInfo.DailyPopulationGrowth_Ceiling.Value;
        if (growthBottom > growthCeiling)
        {
            (growthBottom, growthCeiling) = (growthCeiling, growthBottom);
        }
        Widgets.Label(reusedRect, "OARO_NumberRangePeople".Translate(growthBottom.ToString(), growthCeiling.ToString())
                                                          .Colorize(growthBottom > 0 ? Color.green : ColorLibrary.RedReadable));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        if (Mouse.IsOver(reusedRect))
        {
            string tipStr = CachedBranchInfo.DailyPopulationGrowthExplanation.Value;
            if (!String.IsNullOrEmpty(tipStr))
            {
                TooltipHandler.TipRegion(reusedRect, () => tipStr, 36746149);
            }
        }
    }

    private void DrawRightRect(Rect inRect)
    {
        switch (CurSelectType)
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
        if (SelFacilityDef is null)
        {
            return;
        }
        float inRectX = inRect.xMin;
        Rect reusedRect = new(inRectX, inRect.y + 32f, 105f, 96f);
        GUI.DrawTexture(reusedRect, SelFacilityDef.iconTexture.ExpandedTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(reusedRect.xMax + 4f, reusedRect.y, 195f, 48f);
        Widgets.Label(reusedRect, SelFacilityDef.LabelCap);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;
        reusedRect = new(reusedRect.x, reusedRect.yMax, 195f, 48f);
        Widgets.LabelScrollable(reusedRect, SelFacilityDef.description, ref scrollPosition_FacilityDesc);

        float commonWidth = 298f;
        float stageRectHeight = 24f + 2f + 158f;
        Rect descRect = new(inRectX, reusedRect.yMax + 36f, commonWidth, stageRectHeight);
        if (CurFacilityStageCache is not null)
        {
            TaggedString label = "OARO_BranchWin_CurFacilityStage".Translate();
            if (CurFacilityStageCache.Level == BranchFacilityLevel.Excellent)
            {
                label += " (Max)";
            }
            reusedRect = descRect;
            reusedRect.height = 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, label);
            DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 2f), label, CurFacilityStageCache.StageEffectDesc, ref scrollPosition_CurFacilityStage);
        }

        descRect = new(inRectX, descRect.yMax + 48f, commonWidth, stageRectHeight);
        if (NextFacilityStageCache is not null)
        {
            TaggedString label = "OARO_BranchWin_NextFacilityStage".Translate();
            reusedRect = descRect;
            reusedRect.height = 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, label);
            DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 2f), label, NextFacilityStageCache.StageEffectDesc, ref scrollPosition_NextFacilityStage);
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

        if (CurFacilityStageCache.Level >= BranchFacilityLevel.Excellent)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(inRect, "OARO_ReachMax_FacilityLevel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            return;
        }

        Rect reusedRect;
        if (FacilityHandler.IsBusy && FacilityHandler.UnderConstructionFacilities.TryGetValue(SelFacilityDef, out UnderConstructionRecord<BranchFacilityDef> record))
        {
            reusedRect = new(inRectX, inRect.y, inRect.width, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(reusedRect, "OARO_BranchWin_Constructing".Translate());
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(reusedRect, record.DurationTicksLeft.ToStringTicksToPeriod());

            reusedRect = new(inRectX, reusedRect.yMax, inRectWidth, 24f);
            Widgets.FillableBar(reusedRect, record.Progress, BaseContent.WhiteTex, BaseContent.BlackTex, doBorder: true);

            reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRect.yMax - 28f, 89f, 28f);
            Text.Anchor = TextAnchor.MiddleCenter;
            if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_BranchWin_CancelConstruct".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
            {
                Dialog_NodeTree dialog_Node = OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
                     text: "OARO_BranchWin_CancelConstructWarnning".Translate(),
                     acceptAction: () => FacilityHandler.CancelFacilityConstruction(record.TargetDef));
                Find.WindowStack.Add(dialog_Node);
            }
        }
        else if (NextFacilityStageCache is not null)
        {
            Rect textRect = new(inRectX, inRect.y, inRectWidth, 24f);
            reusedRect = textRect;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(reusedRect, "OARO_BranchWin_ExpectedCost".Translate());
            reusedRect.xMin += 0.33f * inRectWidth;
            Widgets.Label(reusedRect, NextFacilityStageCache.TimeCost.TicksToDays().ToString() + "Day".Translate());

            float silverCostWidth = Text.CalcSize($"× {NextFacilityStageCache.SilverCost}").x;
            reusedRect = textRect;
            reusedRect.xMin = textRect.xMax - silverCostWidth;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(reusedRect, $"× {NextFacilityStageCache.SilverCost}");
            TooltipHandler.TipRegion(reusedRect, () => NextFacilityStageCache?.GetSilverCostExplanation(Branch) ?? string.Empty, uniqueId: 15254109);

            reusedRect = new(reusedRect.xMin - 24f, reusedRect.y, 24f, 24f);
            Widgets.ThingIcon(reusedRect, ThingDefOf.Silver, graphicIndexOverride: 2);

            reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRect.yMax - 28f, 89f, 28f);
            if (OARO_UIUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                acceptance: FacilityConstructionAcceptance.Value,
                label: "OARO_BranchWin_StartConstruct".Translate(),
                baseTex: constructButton,
                downTex: constructButton_Down,
                doMouseoverSound: true))
            {
                AcceptanceReport acceptance = FacilityHandler.CanConstructFacility(SelFacilityDef, byPlayer: true, map: Map, resultOnly: false);
                if (acceptance)
                {
                    FacilityHandler.StartFacilityConstruction(SelFacilityDef, byPlayer: true, map: Map);
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
        string buildingDesc;

        switch (CurSelectType)
        {
            case SelectType.Building:
                {
                    if (SelBuilding is null)
                    {
                        return;
                    }
                    buildingDef = SelBuilding.Def;
                    buildingLabel = SelBuilding.Label;
                    buildingDesc = SelBuilding.Description;
                    break;
                }
            case SelectType.ConstructingBuilding:
                {
                    if (SelUnderConstructionBuilding is null)
                    {
                        return;
                    }
                    buildingDef = SelUnderConstructionBuilding.TargetDef;
                    buildingLabel = SelUnderConstructionBuilding.TargetDef.label;
                    buildingDesc = SelUnderConstructionBuilding.TargetDef.description;
                    break;
                }
            default: return;
        }

        if (SelBuildingDefCache?.BuildingDef != buildingDef)
        {
            SelBuildingDefCache = new BranchBuildingDefSummaryUICache(buildingDef, Branch);
        }

        float inRectX = inRect.x;

        Rect reusedRect = new(inRectX, inRect.y + 32f, 105f, 96f);
        GUI.DrawTexture(reusedRect, buildingDef.iconTexture.ExpandedTexture, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        reusedRect = new(reusedRect.xMax + 4f, reusedRect.y, 195f, 48f);
        Widgets.Label(reusedRect, buildingLabel);

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Tiny;
        reusedRect = new(reusedRect.x, reusedRect.yMax, 195f, 48f);
        Widgets.LabelScrollable(reusedRect, buildingDesc, ref scrollPosition_BuildingDesc);

        float commonWidth = 298f;

        Text.Font = GameFont.Small;
        Rect descRect = DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 32f), "OARO_BranchWin_BuildingBaseEffect".Translate(), SelBuildingDefCache.BaseEffectDesc, ref scrollPosition_BuildingBaseEffect);

        if (buildingDef.IsUpgradable)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX, descRect.yMax + 8f, commonWidth, 48f);
            Widgets.Label(reusedRect, "OARO_BranchWin_BuildingUpgradePopGE".Translate(buildingDef.advancedProperties.advancedPopulation.ToString()));

            descRect = DrawEffectDescriptions(new Vector2(inRectX, reusedRect.yMax + 8f), "OARO_BranchWin_BuildingAdvancedEffect".Translate(), SelBuildingDefCache.AdvancedEffectDesc, ref scrollPosition_BuildingAdvancedEffect);
        }

        if (CurSelectType == SelectType.ConstructingBuilding)
        {
            DrawRight_ConstructingBuildingBottom(new(inRectX, descRect.yMax + 16f));
        }
    }

    /// <summary>
    /// 长：298f, 宽：xx
    /// </summary>
    private void DrawRight_ConstructingBuildingBottom(Vector2 position)
    {
        if (SelUnderConstructionBuilding is null) return;

        float inRectX = position.x;
        float inRectWidth = 298f;
        float inRectHeight = 24f + 24f + 2f + 28f;
        Rect inRect = new(position.x, position.y, inRectWidth, inRectHeight);

        Rect reusedRect = inRect;
        reusedRect.height = 24f;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_BranchWin_Constructing".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, SelUnderConstructionBuilding.DurationTicksLeft.ToStringTicksToPeriod());

        reusedRect = new(inRectX, reusedRect.yMax, inRectWidth, 24f);
        Widgets.FillableBar(reusedRect, SelUnderConstructionBuilding.Progress, BaseContent.WhiteTex, BaseContent.BlackTex, doBorder: true);

        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRect.yMax - 28f, 89f, 28f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_BranchWin_CancelConstruct".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
        {
            Dialog_NodeTree dialog_Node = OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(
                text: "OARO_BranchWin_CancelConstructWarnning".Translate(),
                acceptAction: () => BuildingHandler.CancelBuildingConstruction(SelUnderConstructionBuilding.TargetDef));
            Find.WindowStack.Add(dialog_Node);
        }
    }

    private void DrawRight_EmptyBuildingSlot(Rect inRect)
    {
        float inRectX = inRect.xMin;
        Rect optionalOutRect = new(inRectX, inRect.y + 75f, 295f, 372f);
        GUI.DrawTexture(optionalOutRect, optionalBuildingBackground);
        optionalOutRect = optionalOutRect.ContractedBy(2f);

        DrawOptionalBuildingList(optionalOutRect);

        if (SelBuildingDefCache is null)
        {
            return;
        }

        Rect detailRect = inRect;
        detailRect.yMin = optionalOutRect.yMax + 65f;
        DrawOptionalBuildingDetail(detailRect);
    }

    private void DrawOptionalBuildingList(Rect optionalOutRect)
    {
        Rect optionalViewRect = optionalOutRect;
        optionalViewRect.xMax -= 16f;

        float entryX = optionalViewRect.xMin;
        float entryY = optionalViewRect.yMin;
        float entryWidth = optionalViewRect.width;
        float entryHeight = 96f;
        Rect entryRect;

        IEnumerable<BranchBuildingDefSummaryUICache> optionalBuildingDefs;
        if (BuildingHandler.SpecialBuildingDef.Value is null)
        {
            optionalBuildingDefs = OptionalBuildingDefs.Value.Values;
            optionalViewRect.height = entryHeight * OptionalBuildingDefs.Value.Count;
        }
        else
        {
            optionalBuildingDefs = OptionalBuildingDefs.Value.Values.Where(v => !v.BuildingDef.isSpecial);
            optionalViewRect.height = entryHeight * (OptionalBuildingDefs.Value.Count - OptionalSpecialBuildingCount.Value);
        }
        Widgets.BeginScrollView(optionalOutRect, ref scrollPosition_OptionalBuildings, optionalViewRect);
        int index = 0;

        foreach (BranchBuildingDefSummaryUICache summaryUICache in optionalBuildingDefs)
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
                if (SelBuildingDefCache?.BuildingDef != summaryUICache.BuildingDef)
                {
                    SelBuildingDefCache = new BranchBuildingDefSummaryUICache(summaryUICache.BuildingDef, Branch);
                    BuildingConstructionAcceptance.MarkDirty();
                }
            }
        }
        optionalViewRect.yMax = entryY;
        Widgets.EndScrollView();
    }

    private void DrawOptionalBuildingDetail(Rect inRect)
    {
        Rect reusedRect = inRect;
        reusedRect.height = 24f;
        Widgets.Label(reusedRect, "Description".Translate());

        reusedRect.yMin = reusedRect.yMax;
        reusedRect.yMax += 2f;
        GUI.DrawTexture(reusedRect, optionalBuildingDescCuttingLine);

        reusedRect = new(reusedRect.x, reusedRect.yMax + 8f, reusedRect.width, 80f);
        Widgets.TextArea(reusedRect, SelBuildingDefCache.BuildingDef.description, readOnly: true);

        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, reusedRect.yMax + 2f, 88f, 29f);
        if (OARO_UIUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_BranchWin_StartConstruct".Translate(),
            acceptance: BuildingConstructionAcceptance.Value,
            baseTex: constructButton,
            downTex: constructButton_Down))
        {
            BranchBuildingConstructParms constructParameter = new(Branch, SelBuildingDefCache.BuildingDef)
            {
                ByPlayer = true,
                Map = Map
            };
            AcceptanceReport acceptanceReport = BuildingHandler.CanConstructBuilding(constructParameter);
            if (acceptanceReport)
            {
                BuildingHandler.StartBuildingConstruction(constructParameter);
            }
            else
            {
                Messages.Message("OARO_CanNotStartBuildingConstruction".Translate(acceptanceReport.Reason), MessageTypeDefOf.RejectInput, historical: false);
            }
        }
    }

    /// <summary>
    /// 高96f
    /// </summary>
    private bool DrawOptionalBuildingEntry(Rect inRect, BranchBuildingDefSummaryUICache summaryUICache)
    {
        BranchBuildingDef buildingDef = summaryUICache.BuildingDef;

        Rect reusedRect = OARO_UIUtility.CenterRectOnY(inRect, inRect.xMin + 8f, 64f, 64f);
        GUI.DrawTexture(reusedRect, buildingDef.iconTexture.Texture, ScaleMode.ScaleToFit);

        float textXMin = reusedRect.xMax + 8f;
        float textHeight = inRect.height / 4f;
        float textWidth = inRect.xMax - textXMin - 2f;
        reusedRect = new(textXMin, inRect.y, textWidth, textHeight);

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, buildingDef.label.Colorize(ColorLibrary.Gold));

        Text.WordWrap = false;
        Text.Font = GameFont.Tiny;
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
            }
            TooltipHandler.TipRegion(reusedRect, () => summaryUICache.BaseEffectDescJoint ?? string.Empty, uniqueId: 64130862);
        }

        Text.WordWrap = true;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(textXMin, inRect.yMax - textHeight, textWidth, textHeight);
        reusedRect.width /= 2f;
        Widgets.Label(reusedRect, summaryUICache.TimeCost.TicksToDays().ToString("0.#") + "Day".Translate());

        float textSize = Text.CalcSize($"× {summaryUICache.SilverCost}").x;
        reusedRect = new(inRect.xMax - (textSize + 4f), reusedRect.y, textSize, textHeight);
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, $"× {summaryUICache.SilverCost}");
        TooltipHandler.TipRegion(reusedRect, () => summaryUICache.GetSilverCostExplanation(Branch) ?? string.Empty, uniqueId: 63327679);

        reusedRect = new(reusedRect.xMin - textHeight, reusedRect.y, textHeight, textHeight);
        reusedRect = reusedRect.ContractedBy(2f);
        Widgets.ThingIcon(reusedRect, ThingDefOf.Silver, graphicIndexOverride: 2);

        Text.Anchor = TextAnchor.UpperLeft;

        if (SelBuildingDefCache?.BuildingDef == buildingDef)
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
        viewRect.yMin += 2f;
        viewRect.yMax -= 2f;
        float entryX = viewRect.xMin + 2f;
        float entryY = viewRect.yMin;
        float entryWidth = viewRect.width - 5f;
        float entryHeight = 26f;
        int entryCount = stageEffectDesc.Count;
        int useCount = Mathf.Max(6, entryCount);
        viewRect.height = entryHeight * useCount;

        int column = 0;

        Text.WordWrap = false;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, showScrollbars: false);

        Rect reusedRect = new(entryX + 8f, entryY, entryWidth - 16f, entryHeight);
        entryY += entryHeight;
        if (((++column) & 1) == 0)
        {
            GUI.DrawTexture(reusedRect, effectDescEntry_Dark);
        }
        Widgets.Label(reusedRect, title.Colorize(ColorLibrary.Gold));

        for (int i = 0; i < entryCount; i++)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if (((++column) & 1) == 0)
            {
                GUI.DrawTexture(entryRect, effectDescEntry_Dark);
            }
            entryRect.xMin += 8f;
            entryRect.xMax -= 8f;
            Widgets.Label(entryRect, stageEffectDesc[i]);
        }

        if (useCount > entryCount)
        {
            for (int i = entryCount; i < useCount; i++)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if (((++column) & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, effectDescEntry_Dark);
                }
            }
        }
        Widgets.EndScrollView();

        OAFrame_UIUtility.ResetText();
        return rect;
    }

    private void SwitchTab(TabType tabType)
    {
        if (CurTab == tabType)
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
        CurTab = tabType;
    }

    private void DeselectConstruct()
    {
        SelectType oldSelectType = CurSelectType;
        CurSelectType = SelectType.None;
        switch (oldSelectType)
        {
            case SelectType.Facility:
                SelFacilityDef = null;
                CurFacilityStageCache = null;
                NextFacilityStageCache = null;
                FacilityConstructionAcceptance.MarkDirty();
                break;
            case SelectType.Building:
                SelBuilding = null;
                SelBuildingDefCache = null;
                break;
            case SelectType.ConstructingBuilding:
                SelUnderConstructionBuilding = null;
                SelBuildingDefCache = null;
                break;
            case SelectType.EmptyBuildingSlot:
                SelBuildingDefCache = null;
                break;
            default:
                break;
        }
    }

    private void ClearConstructCache()
    {
        CurSelectType = SelectType.None;

        SelBuilding = null;
        SelBuildingDefCache = null;

        SelFacilityDef = null;
        CurFacilityStageCache = null;
        NextFacilityStageCache = null;

        SelUnderConstructionBuilding = null;

        FacilityConstructionAcceptance.MarkDirty();
        BuildingConstructionAcceptance.MarkDirty();
    }

    private void ClearInteractionCache()
    {
        InteractionAcceptanceDirty = true;
        commonInteractionAcceptances.Clear();
        buildingInteractionAcceptances.Clear();
    }

    private void RecacheInteractionAcceptance()
    {
        InteractionAcceptanceDirty = false;
        commonInteractionAcceptances.Clear();
        foreach (BranchInteractionDef interactionDef in DefDatabase<BranchInteractionDef>.AllDefs.Where(d => !d.onlyBuildingInteraction && d.target == BranchInteractionDef.InteractionTarget.Caravan))
        {
            AcceptanceReport acceptanceReport;
            try
            {
                acceptanceReport = interactionDef.Worker.CanUseInteraction(new BranchInteractionParms(Branch, Caravan), resultOnly: false);
            }
            catch
            {
                acceptanceReport = false;
            }
            commonInteractionAcceptances.Add((interactionDef, acceptanceReport));
        }

        buildingInteractionAcceptances.Clear();
        foreach (BranchBuildingComp_Interaction interactionComp in BuildingHandler.InteractionComps.Where(c => c.Def.target == BranchInteractionDef.InteractionTarget.Caravan))
        {
            AcceptanceReport acceptanceReport;
            try
            {
                acceptanceReport = interactionComp.CanUseInteraction(Caravan, resultOnly: false);
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
        switch (CurSelectType)
        {
            case SelectType.ConstructingBuilding:
                {
                    DeselectConstruct();
                    break;
                }
            case SelectType.EmptyBuildingSlot:
                {
                    DeselectConstruct();
                    if (!added) break;

                    foreach (UnderConstructionRecord<BranchBuildingDef> constructionBuilding in BuildingHandler.UnderConstructionBuildings)
                    {
                        if (constructionBuilding.TargetDef == buildingDef)
                        {
                            SelUnderConstructionBuilding = constructionBuilding;
                            SelBuildingDefCache = new BranchBuildingDefSummaryUICache(buildingDef, Branch);
                            CurSelectType = SelectType.ConstructingBuilding;
                            break;
                        }
                    }
                    break;
                }
            default: return;
        }

        OptionalBuildingDefs.MarkDirty();
        OptionalSpecialBuildingCount.MarkDirty();
    }

    private void PostApplyBranchInteraction(BranchInteractionDef interactionDef, BranchInteractionParms parms, bool succeeded)
    {
        InteractionAcceptanceDirty = true;
        CachedBranchInfo.MarkDirty();
    }

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