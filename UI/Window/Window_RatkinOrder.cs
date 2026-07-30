using NightOcean;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OberoniaAurea.RatkinOrder;

public class MainButtonWorker_RatkinOrderWin : MainButtonWorker_ToggleTab
{
    public override bool Disabled
    {
        get
        {
            if (RatkinOrderManager.Instance.AllRatkinOrders.Count <= 0)
            {
                return true;
            }
            return base.Disabled;
        }
    }
}


[StaticConstructorOnStartup]
public class Window_RatkinOrder : MainTabWindow
{
    protected override float Margin => 0f;
    public override Vector2 InitialSize => new(1337f, 944f);
    public override Vector2 RequestedTabSize => new(1337f, 944f);
    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((Verse.UI.screenWidth - initialSize.x) / 2f, (Verse.UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    private Vector2 scrollPosition_Orders;
    private Vector2 scrollPosition_NormalInteractions;
    private Vector2 scrollPosition_FollowedBranches;
    private CachedTexture esteemTexture;

    private Map Map { get; }
    private LazyMutable<int> MapRecommendationCount { get; }
    private LazyMutable<string> FundChangeDetail { get; }
    private RatkinOrder SelectedOrder { get; set; }

    private int TotalKnightsCount => SelectedOrder.BranchManager.TotalKnightsCount.Value;

    private LazyMutable<int> TotalPopulation { get; }
    private LazyMutable<float> AverageSupply { get; }
    private LazyMutable<int> NotIdleBranchCount { get; }
    private LazyMutable<int> ConstructionBusyBarnchesCount { get; }
    private LazyMutable<(int frienly, int honor)> BranchesTypeCache { get; }
    private LazyMutable<(int urgency, int supplementary, int acceptable)> NormalDemandsCache { get; }
    private LazyMutable<(int friendly, int acceptable)> CriticalDemandsCache { get; }

    private LazyMutable<string> AutoUpgradeRelationshipDesc { get; }
    private LazyMutable<List<Branch>> FollowedBranches { get; }
    private Dictionary<OrderInteractionDef, AcceptanceReport> SpecialInteractionAcceptances { get; } = [];
    private List<KeyValuePair<OrderInteractionDef, AcceptanceReport>> NormalInteractionAcceptances { get; } = [];

    private List<KeyValuePair<Branch, BranchStoresReserveHandler.ReserveRecord>> ReserveRecordShow { get; set; } = [];
    private (Branch, UnderConstructionRecord<BranchBuildingDef>) UnderConstructionBuilding { get; set; }
    private (Branch, UnderConstructionRecord<BranchFacilityDef>) UnderConstructionFacility { get; set; }

    public Window_RatkinOrder()
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

        Map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true) ?? Find.CurrentMap;
        if (Map is null)
        {
            throw new ArgumentNullException(nameof(Map));
        }

        SelectedOrder = RatkinOrderManager.Instance.AllRatkinOrders.FirstOrFallback(fallback: null)
                   ?? throw new InvalidOperationException($"Failed to init {nameof(Window_RatkinOrder)}: No valid {nameof(RatkinOrder)} found. "
                                                          + $"Context: Total orders = {RatkinOrderManager.Instance.AllRatkinOrders.Count()}, Source = {nameof(RatkinOrderManager)}.{nameof(RatkinOrderManager.Instance.AllRatkinOrders)}");

        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationCount(Map));
        FundChangeDetail = new(refreshFunc: () => SelectedOrder?.FundHandler.GetFundChangeDetail() ?? string.Empty);

        TotalPopulation = new(refreshFunc: RefreshTotalPopulation);
        AverageSupply = new(refreshFunc: RefreshAverageSupply);
        NotIdleBranchCount = new(refreshFunc: RefreshNotIdleBranchCount);
        ConstructionBusyBarnchesCount = new(refreshFunc: RefreshConstructionBusyBranchesCount);
        BranchesTypeCache = new(refreshFunc: RefreshBranchesTypeCache);
        NormalDemandsCache = new(refreshFunc: RefreshNormalDemandsCache);
        CriticalDemandsCache = new(refreshFunc: RefreshCriticalDemandsCache);

        AutoUpgradeRelationshipDesc = new(refreshFunc: RefreshAutoUpgradeRelationshipDesc);
        FollowedBranches = new(refreshFunc: RefreshFollowerBranches);
    }
    public Window_RatkinOrder(Map map)
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

        def = OARO_ModDefOf.OARO_KnightOrdersOverview;

        Map = map ?? OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true) ?? Find.CurrentMap;
        if (Map is null)
        {
            throw new ArgumentNullException(nameof(Map));
        }

        SelectedOrder = RatkinOrderManager.Instance.AllRatkinOrders.FirstOrFallback(fallback: null)
                   ?? throw new InvalidOperationException($"Failed to init {nameof(Window_RatkinOrder)}: No valid {nameof(RatkinOrder)} found. "
                                                          + $"Context: Total orders = {RatkinOrderManager.Instance.AllRatkinOrders.Count()}, Source = {nameof(RatkinOrderManager)}.{nameof(RatkinOrderManager.Instance.AllRatkinOrders)}");

        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationCount(Map));
        FundChangeDetail = new(refreshFunc: () => SelectedOrder?.FundHandler.GetFundChangeDetail() ?? string.Empty);

        TotalPopulation = new(refreshFunc: RefreshTotalPopulation);
        AverageSupply = new(refreshFunc: RefreshAverageSupply);
        NotIdleBranchCount = new(refreshFunc: RefreshNotIdleBranchCount);
        ConstructionBusyBarnchesCount = new(refreshFunc: RefreshConstructionBusyBranchesCount);
        BranchesTypeCache = new(refreshFunc: RefreshBranchesTypeCache);
        NormalDemandsCache = new(refreshFunc: RefreshNormalDemandsCache);
        CriticalDemandsCache = new(refreshFunc: RefreshCriticalDemandsCache);

        AutoUpgradeRelationshipDesc = new(refreshFunc: RefreshAutoUpgradeRelationshipDesc);
        FollowedBranches = new(refreshFunc: RefreshFollowerBranches);
    }

    public override void PreOpen()
    {
        base.PreOpen();
        RefreshRatkinOrderCache();
    }
    public override void PostClose()
    {
        base.PostClose();
        ClearRatkinOrderCache();
        UnbindCallbacks();
    }

    private void BindCallbacks()
    {
        SelectedOrder?.PostApplyOrderInteraction.Register(RefreshRatkinInteractionCache);
    }

    private void UnbindCallbacks()
    {
        SelectedOrder?.PostApplyOrderInteraction.Deregister(RefreshRatkinInteractionCache);
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect ratkinOrderRect = new(inRect.x, inRect.y, inRect.width, 37f);
        DrawRatkinOrder(ratkinOrderRect);

        Rect mainRect = new(inRect.x, ratkinOrderRect.yMax, inRect.width, inRect.height - ratkinOrderRect.height);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(3f);
        float mainInnerRectX = mainInnerRect.xMin;
        float mainInnerRectY = mainInnerRect.yMin;

        if (OARO_UIUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }

        //左侧区域
        Rect reusedRect = new(mainInnerRectX, mainInnerRectY, 455f, mainInnerRect.height);
        DrawLeftRect(reusedRect);

        //中部区域
        reusedRect = Rect.MinMaxRect(mainInnerRectX + 456f, mainInnerRectY, mainInnerRectX + 869f, mainInnerRect.yMax);
        DrawMiddleRect(reusedRect);

        //右侧区域
        reusedRect = new(mainInnerRect.xMax - 461f, mainInnerRectY, 461f, mainInnerRect.height);
        DrawRightRect(reusedRect);
    }

    private void DrawRatkinOrder(Rect inRect)
    {
        float entryX = inRect.x;
        float entryY = inRect.y;
        float entryWidth = 125f;
        float entryHeight = inRect.height;

        Rect viewRect = inRect;
        viewRect.width = entryWidth * RatkinOrderManager.Instance.RatkinOrdersCount;

        Widgets.BeginScrollView(inRect, ref scrollPosition_Orders, viewRect, showScrollbars: false);
        Rect entryRect;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            entryRect = new Rect(entryX, entryY, entryWidth, entryHeight);
            entryX += entryWidth;
            if (OARO_UIUtility.TextButtonImage(entryRect, ratkinOrder.Name, orderSelButton, orderSelButton_Down, doMouseoverSound: true))
            {
                SelectRatkinOrder(ratkinOrder);
            }
        }
        Widgets.EndScrollView();

        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    public void SelectRatkinOrder(RatkinOrder ratkinOrder)
    {
        if (!ratkinOrder.IsValid() || SelectedOrder == ratkinOrder)
        {
            return;
        }
        SelectedOrder = ratkinOrder;
        RefreshRatkinOrderCache();
    }

    private void DrawLeftRect(Rect inRect)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin - 80f;

        Rect reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 206f, 400f, 32f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, SelectedOrder.NameColored);

        Text.Font = GameFont.Small;
        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, reusedRect.yMax, 400f, 20f);
        Widgets.Label(reusedRect, SelectedOrder.Faction.NameColored);

        Rect relationLabelRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 407f, 100f, 20f);
        Widgets.Label(relationLabelRect, "OARO_OrderWin_Relationship".Translate());

        Text.Font = GameFont.Medium;
        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 428f, 100f, 28f);
        Widgets.Label(reusedRect, SelectedOrder.Relationship.GetLabel());
        if (Mouse.IsOver(reusedRect))
        {
            string relationChangeReason = SelectedOrder.EsteemHandler.LastRelationshipChangeReason;
            if (!String.IsNullOrEmpty(relationChangeReason))
            {
                TooltipHandler.TipRegion(reusedRect, relationChangeReason);
            }
            if (!String.IsNullOrEmpty(AutoUpgradeRelationshipDesc.Value))
            {
                TooltipHandler.TipRegion(reusedRect, AutoUpgradeRelationshipDesc.Value);
            }
        }

        reusedRect = new(inRectX + 294f, inRectY + 405f, 14f, 23f);
        TooltipHandler.TipRegion(reusedRect, () => "OARO_OrderWin_RelationshipTip".Translate(), uniqueId: 96946587);

        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, relationLabelRect.yMin - esteemTexture.Texture.height - 10f, esteemTexture.Texture.width, esteemTexture.Texture.height);
        GUI.DrawTexture(reusedRect, esteemTexture.Texture, ScaleMode.ScaleToFit);

        Text.Font = GameFont.Small;
        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 461f, 149f, 59f);
        if (OARO_UIUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: OrderInteractionDefOf.OARO_EnhanceRelationship.LabelCap,
            acceptance: SpecialInteractionAcceptances.TryGetValue(OrderInteractionDefOf.OARO_EnhanceRelationship, fallback: false),
            baseTex: leftBigButton,
            downTex: leftBigButton_Down,
            doMouseoverSound: true,
            tooltip: OrderInteractionDefOf.OARO_EnhanceRelationship.description))
        {
            OrderInteractionDefOf.OARO_EnhanceRelationship.TryApplyInteraction(SelectedOrder, Map);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 86f, inRectY + 540f, 128f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_OrderFund".Translate());
        reusedRect = new(inRectX + 64f, inRectY + 560f, 65f, 50f);
        TooltipHandler.TipRegion(reusedRect, () => "OARO_OrderWin_OrderFundTip".Translate(), uniqueId: 32864398);

        reusedRect = new(inRectX + 303f, inRectY + 540f, 128f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_CurRecommendationLetter".Translate());
        reusedRect = new(inRectX + 290f, inRectY + 560f, 65f, 50f);
        TooltipHandler.TipRegion(reusedRect, () => "OARO_OrderWin_CurRecommendationLetterTip".Translate(), uniqueId: 39400977);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (220f - 100f), inRectY + 572f, 90f, 32f);
        Widgets.Label(reusedRect, SelectedOrder.Funds.ToStringPercent());
        if (Mouse.IsOver(reusedRect))
        {
            string fundChangeDetailStr = FundChangeDetail.Value;
            if (!String.IsNullOrEmpty(fundChangeDetailStr))
            {
                TooltipHandler.TipRegion(reusedRect, () => fundChangeDetailStr, uniqueId: 19754361);
            }
        }

        reusedRect = new(inRectX + (420f - 100f), inRectY + 572f, 90f, 32f);
        Widgets.Label(reusedRect, $"× {MapRecommendationCount.Value}");

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX + 63f, inRectY + 620f, 149f, 59f);
        if (SelectedOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AnnualFirstSponsor))
        {
            if (OARO_UIUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: OrderInteractionDefOf.OARO_SponsorOrder.LabelCap,
                acceptance: SpecialInteractionAcceptances.TryGetValue(OrderInteractionDefOf.OARO_SponsorOrder, fallback: false),
                baseTex: leftBigButton,
                downTex: leftBigButton_Down,
                doMouseoverSound: true,
                tooltip: OrderInteractionDefOf.OARO_SponsorOrder.description))
            {
                OrderInteractionDefOf.OARO_SponsorOrder.TryApplyInteraction(SelectedOrder, Map);
            }
        }
        else
        {
            if (OARO_UIUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: OrderInteractionDefOf.OARO_SponsorOrder.label,
                acceptance: SpecialInteractionAcceptances.TryGetValue(OrderInteractionDefOf.OARO_SponsorOrder, fallback: false),
                baseTex: annualFirstSponsorButton,
                downTex: annualFirstSponsorButton_Down,
                doMouseoverSound: true,
                tooltip: OrderInteractionDefOf.OARO_SponsorOrder.description))
            {
                OrderInteractionDefOf.OARO_SponsorOrder.TryApplyInteraction(SelectedOrder, Map);
            }
        }

        reusedRect = new(inRectX + 270f, inRectY + 620f, 149f, 59f);
        if (OARO_UIUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: OrderInteractionDefOf.OARO_ExchangeSupply.LabelCap,
            acceptance: SpecialInteractionAcceptances.TryGetValue(OrderInteractionDefOf.OARO_ExchangeSupply, fallback: false),
            baseTex: leftBigButton,
            downTex: leftBigButton_Down,
            doMouseoverSound: true,
            tooltip: OrderInteractionDefOf.OARO_ExchangeSupply.description))
        {
            OrderInteractionDefOf.OARO_ExchangeSupply.TryApplyInteraction(SelectedOrder, Map);
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 64f, inRectY + 698f, 256f, 28f);
        Widgets.Label(reusedRect, "OARO_OrderWin_Esteem".Translate());
        TooltipHandler.TipRegion(reusedRect, () => "OARO_OrderWin_EsteemTip".Translate(), uniqueId: 54429128);

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (420f - 110f), inRectY + 698f, 100f, 28f);
        Widgets.Label(reusedRect, SelectedOrder.Esteem.ToString());
        if (Mouse.IsOver(reusedRect))
        {
            string lastEsteemChangeTip = $"{SelectedOrder.EsteemHandler.LastEsteemChangeReason} ({SelectedOrder.EsteemHandler.LastEsteemChange.ToStringWithSign()})";
            if (String.IsNullOrEmpty(lastEsteemChangeTip))
            {
                TooltipHandler.TipRegion(reusedRect, () => lastEsteemChangeTip, uniqueId: 47525641);
            }
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 64f, inRectY + 745f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_TotalKnightsCount".Translate());

        reusedRect = new(inRectX + 64f, inRectY + 770f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_TotalPopulation".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (420f - 110f), inRectY + 745f, 100f, 18f);
        Widgets.Label(reusedRect, TotalKnightsCount.ToString());

        reusedRect = new(inRectX + (420f - 110f), inRectY + 770f, 100f, 18f);
        Widgets.Label(reusedRect, TotalPopulation.Value.ToString());

        reusedRect = new(inRectX + 63f, inRectY + 796f, 373f, 75f);
        DrawNormalInteraction(reusedRect);

        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawNormalInteraction(Rect inRect)
    {
        Rect viewRect = inRect;
        viewRect.width -= 16f;

        float inRectXmin = inRect.xMin;
        float entryX = inRectXmin;
        float entryY = inRect.yMin;
        float entryWidth = inRect.width / 3f - 0.01f;
        float entryHeight = 24f;
        viewRect.height = (NormalInteractionAcceptances.Count / 3 + 1) * entryHeight;

        Widgets.BeginScrollView(inRect, ref scrollPosition_NormalInteractions, viewRect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        int column = 0;
        foreach (KeyValuePair<OrderInteractionDef, AcceptanceReport> kv in NormalInteractionAcceptances)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            if ((++column) >= 3)
            {
                column = 0;
                entryX = inRectXmin;
                entryY += entryHeight;
            }
            else
            {
                entryX += entryWidth;
            }
            if (OARO_UIUtility.TextButtonImageDisableable(
                butRect: entryRect,
                label: kv.Key.label,
                acceptance: kv.Value,
                baseTex: normalInteractionButton,
                downTex: normalInteractionButton_Down,
                doMouseoverSound: true,
                tooltip: kv.Key.description))
            {
                kv.Key.Worker.TryApplyInteraction(SelectedOrder, Map);
                break;
            }
        }

        Widgets.EndScrollView();
        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawMiddleRect(Rect inRect)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin;
        float inRectWidth = inRect.width;

        Rect reusedRect = new(inRectX, inRectY + 96f, inRectWidth, 190f);
        DrawTaskSummary(reusedRect);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX, inRectY + 311f, inRectWidth, 28f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchInfo".Translate());

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 36f, inRectY + 359f, 96f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchCount".Translate());

        reusedRect = new(inRectX + 200f, inRectY + 359f, 42f, 20f);
        Widgets.Label(reusedRect, SelectedOrder.BranchManager.AllBranchesCount.ToString());

        reusedRect.xMax += 80f;
        reusedRect.xMin += 80f;
        Widgets.Label(reusedRect, BranchesTypeCache.Value.honor.ToString());

        reusedRect.xMax += 80f;
        reusedRect.xMin += 80f;
        Widgets.Label(reusedRect, BranchesTypeCache.Value.frienly.ToString());

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 385f, 346f, 25f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenBranchWindow".Translate(), squadButton, squadButton_Down, doMouseoverSound: true))
        {
            Window_BranchList branchListWin = new(SelectedOrder, Map, initWithConstructTab: false);
            Find.WindowStack.Add(branchListWin);
            Close();
            return;
        }
        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 410f, 346f, 25f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenSquadWindow".Translate(), squadButton, squadButton_Down, doMouseoverSound: true))
        {
            Window_BranchSquad branchSquadWin = new(SelectedOrder, Map);
            Find.WindowStack.Add(branchSquadWin);
            Close();
            return;
        }

        reusedRect = new(inRectX, inRectY + 440f, inRectWidth, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_AverageSupply".Translate(AverageSupply.Value.ToStringPercent()));

        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 470f, 346f, 140f);
        DrawFollowedBranchList(reusedRect);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX, inRectY + 621f, inRectWidth, 28f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchDemand".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(inRectX + 246f, inRectY + 654f, 134f, 25f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenDemandWindow".Translate(), windowButton, windowButton_Down, doMouseoverSound: true))
        {
            Window_BranchDemand branchDemandWin = new(SelectedOrder, Map);
            Find.WindowStack.Add(branchDemandWin);
            Close();
            return;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 35f, inRectY + 684f, inRectWidth - 35f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_AcceptedDemands".Translate());

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderWin_NormalDemands".Translate());

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderWin_CriticalDemands".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 684f, inRectWidth - 35f, 20f);
        Widgets.Label(reusedRect, $"{AcceptedBranchDemandHandler.Instance.AcceptanceCount}/{RatkinOrderSettings.MaxConcurrentAcceptedDemand}");

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderWin_NormalDemandsInfo".Translate(NormalDemandsCache.Value.urgency.ToString(), NormalDemandsCache.Value.supplementary.ToString(), NormalDemandsCache.Value.acceptable.ToString()));

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderWin_CriticalDemandsInfo".Translate(CriticalDemandsCache.Value.friendly.ToString(), CriticalDemandsCache.Value.acceptable.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 35f, inRectY + 801f, inRectWidth - 35f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_CompletedDemands".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 801f, inRectWidth - 35f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_CompletedDemandsInfo".Translate(SelectedOrder.BranchManager.CriticalDemandFulfillCount.ToString(), SelectedOrder.BranchManager.NormalDemandFulfillCount.ToString()));

        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawTaskSummary(Rect inRect)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin;
        float inRectWidth = inRect.width;

        Rect reusedRect = new(inRectX, inRectY, inRectWidth, 28f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_OrderWin_TaskInfoStr".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(inRectX, inRectY + 35f, inRectWidth, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_OrderTaskInfo".Translate(NotIdleBranchCount.Value.ToString()));

        reusedRect = new(inRectX + 246f, inRectY + 80f, 134f, 25f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenTaskWindow".Translate(), windowButton, windowButton_Down, doMouseoverSound: true))
        {
            Window_BranchTask taskWin = new(SelectedOrder, Map);
            Find.WindowStack.Add(taskWin);
            Close();
            return;
        }

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 110f, inRectWidth - 36f, 20f);
        Widgets.Label(reusedRect, SelectedOrder.JointPatrolManager.TickToNextStage.ToStringTicksToPeriod());

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 36f, inRectY + 136f, inRectWidth - 36f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_JointPatrolExpectedResult".Translate());

        switch (SelectedOrder.JointPatrolManager.CurState)
        {
            case JointPatrolManager.PatrolState.Prepare:
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    reusedRect = new(inRectX + 36f, inRectY + 110f, 256f, 20f);
                    Widgets.Label(reusedRect, "OARO_OrderWin_DayToJointPatrolStart".Translate());

                    Text.Anchor = TextAnchor.MiddleRight;
                    reusedRect = new(inRectX, inRectY + 136f, inRectWidth - 36f, 20f);
                    Widgets.Label(reusedRect, "OARO_OrderWin_JointPatrolNotStartNow".Translate());
                    break;
                }
            case JointPatrolManager.PatrolState.Ongoing:
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    reusedRect = new(inRectX + 36f, inRectY + 110f, 256f, 20f);
                    Widgets.Label(reusedRect, "OARO_OrderWin_DayToJointPatrolEnd".Translate());



                    break;
                }
            default:
                {
                    Text.Anchor = TextAnchor.MiddleLeft;
                    reusedRect = new(inRectX + 36f, inRectY + 110f, 256f, 20f);
                    Widgets.Label(reusedRect, "OARO_OrderWin_DayToNextJointPatrol".Translate());

                    Text.Anchor = TextAnchor.MiddleRight;
                    reusedRect = new(inRectX, inRectY + 136f, inRectWidth - 36f, 20f);
                    Widgets.Label(reusedRect, "OARO_OrderWin_JointPatrolNotStartNow".Translate());
                    break;
                }
        }

        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawFollowedBranchList(Rect inRect)
    {
        float inRectX = inRect.x;
        float inRectY = inRect.y;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Rect reusedRect = new(inRectX + 16f, inRectY + 2.5f, inRect.width - 16f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchNameLabel".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + 16f, inRectY + 2.5f, inRect.width - 16f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchDemandLabel".Translate());

        Rect outRect = Rect.MinMaxRect(inRectX, inRectY + 25f, inRect.xMax, inRect.yMax - 25f);
        float entryX = outRect.xMin;
        float entryY = outRect.yMin;
        float entryWidth = outRect.width;
        float entryHeight = 25f;

        Rect viewRect = outRect;
        List<Branch> followedBranches = FollowedBranches.Value;
        viewRect.height = (followedBranches.Count + 1) * entryHeight;

        Widgets.BeginScrollView(outRect, ref scrollPosition_FollowedBranches, viewRect, showScrollbars: false);
        Rect entryRect;
        foreach (Branch branch in followedBranches)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DrawFollowedBranch(entryRect, branch);
        }

        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX, inRect.yMax - 25f, inRect.width, 25f);
        reusedRect = OARO_UIUtility.CenterRectOnX(reusedRect, reusedRect.y, 120f, 25f);
        if (OARO_UIUtility.TextButtonImage(
            butRect: reusedRect,
            label: "OARO_OrderWin_ChangeFollowedBranches".Translate(),
            baseTex: changeFollowedBranchesButton,
            downTex: changeFollowedBranchesButton_Down,
            doMouseoverSound: true))
        {
            FollowedBranchesFloatMenu();
        }
        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawFollowedBranch(Rect inRect, Branch branch)
    {
        float inRectX = inRect.x;

        Rect reusedRect = new(inRectX, inRect.y + 1f, 5f, inRect.height - 2f);
        GUI.DrawTexture(reusedRect, branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        float labelWidth = Text.CalcSize(branch.NameColored).x;
        labelWidth = labelWidth > 128f ? 128f : labelWidth;
        reusedRect = OARO_UIUtility.CenterRectOnY(inRect, reusedRect.xMax + 3f, labelWidth, 20f);
        Widgets.Label(reusedRect, branch.NameColored);
        if (Widgets.ButtonInvisible(reusedRect.ContractedBy(2f)))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            branch.RatkinOrder.BranchManager.FollowedBranches.Remove(branch);
            FollowedBranches.MarkDirty();
            return;
        }

        reusedRect = OARO_UIUtility.CenterRectOnY(inRect, reusedRect.xMax + 3f, 40f, 20f);
        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            Widgets.Label(reusedRect, "OARO_Friendly".Translate().Colorize(Color.green));
        }
        else if (branch.IsBranchOfType(Branch.BranchType.Honor))
        {
            Widgets.Label(reusedRect, "OARO_Honor".Translate().Colorize(Color.yellow));
        }

        if (branch.CurWorkState == Branch.WorkStateType.Idle)
        {
            reusedRect = OARO_UIUtility.CenterRectOnY(inRect, reusedRect.xMax + 12f, inRect.height - 2f, inRect.height - 2f);
            GUI.DrawTexture(reusedRect, OARO_IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
        }
        reusedRect = OARO_UIUtility.CenterRectOnY(inRect, reusedRect.xMax + 12f, inRect.height - 2f, inRect.height - 2f);
        if (OARO_UIUtility.TextButtonImage(
            reusedRect,
            string.Empty,
            OARO_IconLibrary.ellipsisButton,
            OARO_IconLibrary.ellipsisButton_Down,
            doMouseoverSound: true))
        {
            Window_Branch branchWin = new(branch, map: Map);
            Find.WindowStack.Add(branchWin);
            Close();
            return;
        }

        if (branch.DemandHandler.NormalDemand is not null)
        {
            reusedRect = OARO_UIUtility.CenterRectOnY(inRect, inRect.xMax - 35f, 35f, 25f);
            GUI.DrawTexture(reusedRect, normalDemandFlag);
        }

        if (branch.DemandHandler.CriticalDemand is not null)
        {
            reusedRect = OARO_UIUtility.CenterRectOnY(inRect, inRect.xMax - (35f + 25f), 35f, 25f);
            GUI.DrawTexture(reusedRect, criticalDemandFlag);
        }
    }

    private void DrawRightRect(Rect inRect)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin;
        float inRectWidth = inRect.width;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(inRectX, inRectY + 99f, inRectWidth, 28f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchConstruction".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(inRectX, inRectY + 133f, inRectWidth, 18f);
        Widgets.Label(reusedRect, "OARO_OrderWin_ConstructionBusyBarnchesCount".Translate(ConstructionBusyBarnchesCount.Value.ToString()));

        /*
        reusedRect = new(inRectX + 269f, inRectY + 178f, 134f, 25f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenBranchWindow".Translate(), windowButton, windowButton_Down, doMouseoverSound: true))
        {

        }
        */

        float entryX = inRectX + 30f;
        float entryY = inRectY + 204f;
        if (ReserveRecordShow.Count > 0)
        {
            foreach (KeyValuePair<Branch, BranchStoresReserveHandler.ReserveRecord> kv in ReserveRecordShow)
            {
                Rect entryRect = new(entryX, entryY, 373f, 54f);
                entryY += 54f;
                DrawStoresReserveRect(entryRect, kv.Key, kv.Value);
            }
        }
        else
        {
            Rect entryRect = new(entryX, entryY, 373f, 54f);
            GUI.DrawTexture(entryRect, rightUpFrame);
            GUI.DrawTexture(entryRect, rightUpFrameShade);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(entryRect, "OARO_OrderWin_NoReserveRecordCanShow".Translate());
        }

        reusedRect = new(inRectX + 36f, inRectY + 337f, 134f, 52f);
        if (OARO_UIUtility.TextButtonImage(reusedRect, "OARO_OrderWin_BranchConstructionButton".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
        {
            Window_BranchList branchListWin = new(SelectedOrder, Map, initWithConstructTab: true);
            Find.WindowStack.Add(branchListWin);
            Close();
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        if (UnderConstructionBuilding.Item1 is not null)
        {
            reusedRect = new(inRectX + 173f, inRectY + 339f, 55f, 48f);
            GUI.DrawTexture(reusedRect, UnderConstructionBuilding.Item2.TargetDef.iconTexture.Texture, ScaleMode.ScaleToFit);
            reusedRect = new(inRectX + 250f, inRectY + 341f, 128f, 20f);
            Widgets.Label(reusedRect, UnderConstructionBuilding.Item1.Name);
            reusedRect = new(inRectX + 250f, inRectY + 366f, 128f, 20f);
            Widgets.Label(reusedRect, "WaitTime".Translate(UnderConstructionBuilding.Item2.DurationTicksLeft.ToStringTicksToPeriod()));

        }
        else if (UnderConstructionFacility.Item1 is not null)
        {
            reusedRect = new(inRectX + 173f, inRectY + 339f, 55f, 48f);
            GUI.DrawTexture(reusedRect, UnderConstructionFacility.Item2.TargetDef.iconTexture.Texture, ScaleMode.ScaleToFit);
            reusedRect = new(inRectX + 250f, inRectY + 341f, 128f, 20f);
            Widgets.Label(reusedRect, UnderConstructionFacility.Item1.Name);
            reusedRect = new(inRectX + 250f, inRectY + 366f, 128f, 20f);
            Widgets.Label(reusedRect, "WaitTime".Translate(UnderConstructionFacility.Item2.DurationTicksLeft.ToStringTicksToPeriod()));
        }
        else
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX + 166f, inRectY + 337f, 235f, 52f);
            Widgets.Label(reusedRect, "OARO_OrderWin_NoConstruction".Translate());
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX + 36f, inRectY + 391f, 368f, 50f);
        if (OARO_UIUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: string.Empty,
            acceptance: SpecialInteractionAcceptances.TryGetValue(OrderInteractionDefOf.OARO_InviteBranchCreation, fallback: false),
            baseTex: inviteBranchCreationButton,
            downTex: inviteBranchCreationButton_Down,
            doMouseoverSound: true,
            tooltip: OrderInteractionDefOf.OARO_InviteBranchCreation.description))
        {
            Close();
            OrderInteractionDefOf.OARO_InviteBranchCreation.TryApplyInteraction(SelectedOrder, Map);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = OARO_UIUtility.CenterRectOnY(reusedRect, inRectX + 45f, 192f, 20f);
        Widgets.Label(reusedRect, OrderInteractionDefOf.OARO_InviteBranchCreation.label);

        reusedRect = new(inRectX + 298f, inRectY + 396f, 100f, 20f);
        Widgets.DefIcon(new(reusedRect.x, reusedRect.y, 20f, 20f), ThingDefOf.Silver, graphicIndexOverride: 2);
        reusedRect.xMin += 22f;
        Widgets.Label(reusedRect, $"× {SelectedOrder.BranchManager.SilverNeededForNextBranchCreation}");

        reusedRect = new(inRectX + 298f, inRectY + 416f, 100f, 20f);
        OARO_UIUtility.DrawRecommendationInfo(reusedRect, 1, textOffset: 2f);

        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRectY + 505f, 372f, 320f);
        DrawReformation(reusedRect);

        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawStoresReserveRect(Rect inRect, Branch branch, BranchStoresReserveHandler.ReserveRecord reserveRecord)
    {
        GUI.DrawTexture(inRect, rightUpFrame);

        Rect reusedRect = new(inRect.x, inRect.y + 2f, 2f, inRect.height - 4f);
        GUI.DrawTexture(reusedRect, branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);

        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(innerRectX + 4f, innerRectY, 252f - 8f, 24f);
        Widgets.Label(reusedRect, branch.Name);

        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchFacilityLevel".Translate(branch.FacilityHandler.TotalFacilityLevel.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(innerRectX + 4f, innerRectY + 25f, 252f - 8f, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchPopulation".Translate(branch.PopulationHandler.Population.ToString()));

        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchBuildingLimit".Translate(branch.BuildingHandler.AllBuildingsCount.ToString(), (branch.BuildingHandler.BuildingCeiling + 1).ToString()));

        Rect progressRect = new(innerRectX + 254f, innerRectY, 115f, innerRect.height);

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(progressRect.x + 2f, progressRect.y + 2f, progressRect.width - 2f, 12f);
        Widgets.Label(reusedRect, "OARO_OrderWin_StoresReserve".Translate());
        reusedRect = new(progressRect.x + 2f, progressRect.yMax - (2f + 12f), progressRect.width - 2f, 12f);
        Widgets.Label(reusedRect, "OARO_OrderWin_StoresReserveReduce".Translate(reserveRecord.CostRateReduce.ToStringPercent()));
        reusedRect = OARO_UIUtility.CenterRectOnY(progressRect, progressRect.xMin + 50f, 50f, 48f);
        GUI.DrawTexture(reusedRect, reserveRecord.Target.iconTexture.Texture, ScaleMode.ScaleToFit);

        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawReformation(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(inRect, "OARO_ReformationNotFinished".Translate().Colorize(Color.gray));
        OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void FollowedBranchesFloatMenu()
    {
        if (SelectedOrder is null)
        {
            return;
        }

        List<FloatMenuOption> options = [];
        BranchManager branchManager = SelectedOrder.BranchManager;
        foreach (Branch branch in branchManager.AllBranches)
        {
            if (branchManager.FollowedBranches.Contains(branch))
            {
                continue;
            }

            options.Add(new FloatMenuOption(branch.Name, action: delegate
            {
                branchManager.FollowedBranches.AddDistinct(branch);
                FollowedBranches.MarkDirty();
            }));
        }

        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("OARO_NoAvailableBranch".Translate(), action: null));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void RefreshRatkinOrderCache()
    {
        ClearRatkinOrderCache();
        if (!SelectedOrder.IsValid())
        {
            return;
        }

        esteemTexture = new CachedTexture($"UI/RatkinOrder/OARO_EsteemTexture_{EsteemUtility.GetIndex(SelectedOrder.Esteem)}");

        UnderConstructionFacility = SelectedOrder.BranchManager.AllBranches.Where(b => b.FacilityHandler.IsBusy).Select(b => (b, b.FacilityHandler.UnderConstructionFacilities.Values.RandomElement())).FirstOrFallback();
        UnderConstructionBuilding = SelectedOrder.BranchManager.AllBranches.Where(b => b.BuildingHandler.IsBusy).Select(b => (b, b.BuildingHandler.UnderConstructionBuildings.RandomElement())).FirstOrFallback();
        ReserveRecordShow = SelectedOrder.BranchManager.AllPrimaryReserves.Take(2).ToList();

        RefreshRatkinInteractionCache(interactionDef: null, map: Map, succeeded: false);

        UnbindCallbacks();
        BindCallbacks();
    }

    private int RefreshTotalPopulation()
    {
        int total = 0;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            total += allBranches[i].PopulationHandler.Population;
        }
        return total;
    }

    private float RefreshAverageSupply()
    {
        float total = 0f;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            total += allBranches[i].Supply;
        }
        return allBranches.Count > 0 ? total / allBranches.Count : 0f;
    }

    private int RefreshNotIdleBranchCount()
    {
        int count = 0;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            if (allBranches[i].CurWorkState != Branch.WorkStateType.Idle)
            {
                count++;
            }
        }
        return count;
    }

    private int RefreshConstructionBusyBranchesCount()
    {
        int count = 0;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            if (allBranches[i].IsConstructionBusy)
            {
                count++;
            }
        }
        return count;
    }

    private (int frienly, int honor) RefreshBranchesTypeCache()
    {
        int frienly = 0, honor = 0;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            Branch branch = allBranches[i];
            if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            {
                frienly++;
            }
            if (branch.IsBranchOfType(Branch.BranchType.Honor))
            {
                honor++;
            }
        }
        return (frienly, honor);
    }

    private (int urgency, int supplementary, int acceptable) RefreshNormalDemandsCache()
    {
        int urgency = 0, supplementary = 0, acceptable = 0;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            Branch branch = allBranches[i];
            try
            {
                BranchDemand demand = branch.DemandHandler.NormalDemand;
                if (demand is not null)
                {
                    switch (demand.DemandTypeValue)
                    {
                        case BranchDemand.DemandType.Urgency: urgency++; break;
                        case BranchDemand.DemandType.Supplementary: supplementary++; break;
                    }
                    if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: false, resultOnly: true))
                    {
                        acceptable++;
                    }
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "refresh normal demands cache",
                    typeName: nameof(Window_RatkinOrder),
                    methodName: nameof(RefreshNormalDemandsCache),
                    needStackTrace: true);
            }
        }
        return (urgency, supplementary, acceptable);
    }

    private (int friendly, int acceptable) RefreshCriticalDemandsCache()
    {
        int friendly = 0, acceptable = 0;
        IReadOnlyList<Branch> allBranches = SelectedOrder.BranchManager.AllBranches;
        for (int i = 0; i < allBranches.Count; i++)
        {
            Branch branch = allBranches[i];
            try
            {
                BranchDemand demand = branch.DemandHandler.CriticalDemand;
                if (demand is not null)
                {
                    if (branch.IsBranchOfType(Branch.BranchType.Friendly))
                    {
                        friendly++;
                    }
                    if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: true, resultOnly: true))
                    {
                        acceptable++;
                    }
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "refresh critical demands cache",
                    typeName: nameof(Window_RatkinOrder),
                    methodName: nameof(RefreshCriticalDemandsCache),
                    needStackTrace: true);
            }
        }
        return (friendly, acceptable);
    }

    private string RefreshAutoUpgradeRelationshipDesc()
    {
        if (SelectedOrder is null)
        {
            return string.Empty;
        }

        SelectedOrder.GetChanceOfAutoUpgradeRelationship(resultOnly: false, out string explanation);
        return explanation;
    }

    private List<Branch> RefreshFollowerBranches()
    {
        if (SelectedOrder is null)
        {
            return [];
        }

        List<Branch> branches = [.. SelectedOrder.BranchManager.FollowedBranches.Where(b => b.IsValid())];
        return branches;
    }

    private void RefreshRatkinInteractionCache(OrderInteractionDef interactionDef, Map map, bool succeeded)
    {
        SpecialInteractionAcceptances.Clear();
        NormalInteractionAcceptances.Clear();
        if (!SelectedOrder.IsValid())
        {
            return;
        }

        // 交互可能影响分部统计数据，标记所有统计缓存脏
        TotalPopulation.MarkDirty();
        AverageSupply.MarkDirty();
        NotIdleBranchCount.MarkDirty();
        ConstructionBusyBarnchesCount.MarkDirty();
        BranchesTypeCache.MarkDirty();
        NormalDemandsCache.MarkDirty();
        CriticalDemandsCache.MarkDirty();
        MapRecommendationCount.MarkDirty();
        FundChangeDetail.MarkDirty();
        FollowedBranches.MarkDirty();
        AutoUpgradeRelationshipDesc.MarkDirty();

        foreach (OrderInteractionDef def in DefDatabase<OrderInteractionDef>.AllDefsListForReading)
        {
            if (!def.displayOnUI)
            {
                continue;
            }

            AcceptanceReport acceptanceReport = false;
            try
            {
                acceptanceReport = def.CanUseInteraction(SelectedOrder, Map, resultOnly: false);
            }
            catch (Exception ex)
            {
                acceptanceReport = false;
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"try get acceptance report of {def}",
                    typeName: nameof(Window_RatkinOrder),
                    methodName: nameof(RefreshRatkinInteractionCache),
                    needStackTrace: true);
            }
            finally
            {
                if (def.specialDisplayOnUI)
                {
                    SpecialInteractionAcceptances[def] = acceptanceReport;
                }
                else
                {
                    NormalInteractionAcceptances.Add(new KeyValuePair<OrderInteractionDef, AcceptanceReport>(def, acceptanceReport));
                }
            }
        }
    }

    private void ClearRatkinOrderCache()
    {
        UnbindCallbacks();

        MapRecommendationCount.MarkDirty();
        FundChangeDetail.MarkDirty();
        esteemTexture = new CachedTexture("UI/RatkinOrder/OARO_EsteemTexture_0");

        TotalPopulation.MarkDirty();
        AverageSupply.MarkDirty();
        NotIdleBranchCount.MarkDirty();
        ConstructionBusyBarnchesCount.MarkDirty();
        BranchesTypeCache.MarkDirty();
        NormalDemandsCache.MarkDirty();
        CriticalDemandsCache.MarkDirty();

        FollowedBranches.MarkDirty();
        SpecialInteractionAcceptances.Clear();
        NormalInteractionAcceptances.Clear();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_MainBackground");

    private static readonly Texture2D orderSelButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_OrderSelButton");
    private static readonly Texture2D orderSelButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_OrderSelButton_Down");
    private static readonly Texture2D leftBigButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_LeftBigButton");
    private static readonly Texture2D leftBigButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_LeftBigButton_Down");
    private static readonly Texture2D annualFirstSponsorButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_AnnualFirstSponsorButton");
    private static readonly Texture2D annualFirstSponsorButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_AnnualFirstSponsorButton_Down");
    private static readonly Texture2D normalInteractionButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_NormalInteractionButton");
    private static readonly Texture2D normalInteractionButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_NormalInteractionButton_Down");

    private static readonly Texture2D squadButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_SquadButton");
    private static readonly Texture2D squadButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_SquadButton_Down");
    private static readonly Texture2D constructButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ConstructButton");
    private static readonly Texture2D constructButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ConstructButton_Down");
    private static readonly Texture2D inviteBranchCreationButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_InviteBranchCreationButton");
    private static readonly Texture2D inviteBranchCreationButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_InviteBranchCreationButton_Down");
    private static readonly Texture2D changeFollowedBranchesButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ChangeFollowedBranchesButton");
    private static readonly Texture2D changeFollowedBranchesButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ChangeFollowedBranchesButton_Down");

    private static readonly Texture2D rightUpFrame = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_RightUpFrame");
    private static readonly Texture2D rightUpFrameShade = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_RightUpFrameShade");

    private static readonly Texture2D windowButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_WindowButton");
    private static readonly Texture2D windowButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_WindowButton_Down");

    private static readonly Texture2D normalDemandFlag = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_NormalDemandFlag");
    private static readonly Texture2D criticalDemandFlag = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_CriticalDemandFlag");
}