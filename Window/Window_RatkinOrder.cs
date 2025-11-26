using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_RatkinOrder : MainTabWindow
{
    protected override float Margin => 0f;
    public override Vector2 InitialSize => new(1337f, 944f);
    public override Vector2 RequestedTabSize => new(1337f, 944f);
    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    private Vector2 scrollPosition_Orders;
    private Vector2 scrollPosition_FollowedBranches;
    private CachedTexture esteemTexture;

    private readonly Map map;
    private readonly LazyMutable<int> mapRecommendationCount;
    private RatkinOrder selectedOrder;

    private int totalKnightsCount;
    private int totalPopulation;
    private float averageSupply;
    private int notIdleBranchCount;
    private int constructionBusyBarnchesCount;
    private (int frienly, int honor) branchesTypeCache;
    private (int urgency, int supplementary, int acceptable) normalDemandsCache;
    private (int friendly, int acceptable) criticalDemandsCache;

    private readonly Dictionary<OrderInteractionDef, AcceptanceReport> independentInteractionAcceptances = [];
    private readonly List<KeyValuePair<OrderInteractionDef, AcceptanceReport>> otherInteractionAcceptances = [];

    private List<KeyValuePair<Branch, BranchStoresReserveHandler.ReserveRecord>> reserveRecordShow = [];
    private (Branch, UnderConstructionRecord<BranchBuildingDef>) underConstructionBuilding;
    private (Branch, UnderConstructionRecord<BranchFacilityDef>) underConstructionFacility;

    public Window_RatkinOrder()
    {
        map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true) ?? throw new ArgumentNullException(nameof(map));
        mapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationOfMap(selectedOrder, map));
        selectedOrder = RatkinOrderManager.Instance.AllRatkinOrders.FirstOrFallback(fallback: null)
                   ?? throw new InvalidOperationException($"Failed to init {nameof(Window_RatkinOrder)}: No valid {nameof(RatkinOrder)} found. "
                                                          + $"Context: Total orders = {RatkinOrderManager.Instance.AllRatkinOrders.Count()}, Source = {nameof(RatkinOrderManager)}.{nameof(RatkinOrderManager.Instance.AllRatkinOrders)}");


    }
    public Window_RatkinOrder(Map map)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        mapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationOfMap(selectedOrder, this.map));
        selectedOrder = RatkinOrderManager.Instance.AllRatkinOrders.FirstOrFallback(fallback: null)
            ?? throw new InvalidOperationException($"Failed to init {nameof(Window_RatkinOrder)}: No valid {nameof(RatkinOrder)} found. "
                                                   + $"Context: Total orders = {RatkinOrderManager.Instance.AllRatkinOrders.Count()}, Source = {nameof(RatkinOrderManager)}.{nameof(RatkinOrderManager.Instance.AllRatkinOrders)}");

        RefreshRatkinOrderCache();
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
        foreach (RatkinOrder order in RatkinOrderManager.Instance.AllRatkinOrders)
        {
            order.PostApplyOrderInteraction -= RefreshRatkinInteractionCache;
        }
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

        Rect reusedRect = new(mainInnerRect.xMax - 21f, mainInnerRectY + 1f, 20f, 20f);
        if (Widgets.ButtonImage(reusedRect, IconLibrary.colseX, doMouseoverSound: true))
        {
            Close();
            return;
        }

        //左侧区域
        reusedRect = new(mainInnerRectX, mainInnerRectY, 455f, mainInnerRect.height);
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
            if (OARO_WindowUtility.TextButtonImage(entryRect, ratkinOrder.Name, orderSelButton, orderSelButton_Down, doMouseoverSound: true))
            {
                OnOrderButtonDown(ratkinOrder);
            }
        }
        Widgets.EndScrollView();

        OARO_WindowUtility.ResetText();
    }
    private void OnOrderButtonDown(RatkinOrder ratkinOrder)
    {
        if (ratkinOrder is null || selectedOrder == ratkinOrder)
        {
            return;
        }
        selectedOrder = ratkinOrder;
        RefreshRatkinOrderCache();
    }

    private void DrawLeftRect(Rect inRect)
    {
        float inRectX = inRect.xMin;
        float inRectY = inRect.yMin;

        Rect reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 206f, 400f, 32f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, selectedOrder.Name);

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + (407f - esteemTexture.Texture.height - 12f), esteemTexture.Texture.width, esteemTexture.Texture.height);
        GUI.DrawTexture(reusedRect, esteemTexture.Texture);

        Text.Font = GameFont.Small;
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 407f, 100f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_Relationship".Translate());

        Text.Font = GameFont.Medium;
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 430f, 100f, 24f);
        Widgets.Label(reusedRect, selectedOrder.Relationship.GetLabel().Colorize(selectedOrder.Relationship.GetColor()));

        Text.Font = GameFont.Small;
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 461f, 149f, 59f);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: OrderInteractionDefOf.OARO_EnhanceRelationship.LabelCap,
            acceptance: GetIndependentAcceptanceReport(OrderInteractionDefOf.OARO_EnhanceRelationship),
            baseTex: leftBigButton,
            downTex: leftBigButton_Down,
            doMouseoverSound: true))
        {
            OrderInteractionDefOf.OARO_EnhanceRelationship.TryApplyInteraction(selectedOrder, map);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 86f, inRectY + 540f, 128f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_OrderFund".Translate());

        reusedRect = new(inRectX + 303f, inRectY + 540f, 128f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_CurRecommendationLetter".Translate());

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (420f - 100f), inRectY + 572f, 90f, 32f);
        Widgets.Label(reusedRect, $"× {mapRecommendationCount.Value}");

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX + 63f, inRectY + 620f, 149f, 59f);
        if (selectedOrder.CooldownManager.IsInCooldown(KeyLibrary_CDRecord.AnnualFirstSponsor))
        {
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: OrderInteractionDefOf.OARO_SponsorOrder.LabelCap,
                acceptance: GetIndependentAcceptanceReport(OrderInteractionDefOf.OARO_SponsorOrder),
                baseTex: leftBigButton,
                downTex: leftBigButton_Down,
                doMouseoverSound: true))
            {
                OrderInteractionDefOf.OARO_SponsorOrder.TryApplyInteraction(selectedOrder, map);
            }
        }
        else
        {
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: OrderInteractionDefOf.OARO_SponsorOrder.label,
                acceptance: GetIndependentAcceptanceReport(OrderInteractionDefOf.OARO_SponsorOrder),
                baseTex: annualFirstSponsorButton,
                downTex: annualFirstSponsorButton_Down,
                doMouseoverSound: true))
            {
                OrderInteractionDefOf.OARO_SponsorOrder.TryApplyInteraction(selectedOrder, map);
            }
        }

        reusedRect = new(inRectX + 270f, inRectY + 620f, 149f, 59f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_Open".Translate(), leftBigButton, leftBigButton_Down, doMouseoverSound: true))
        {

        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 64f, inRectY + 698f, 256f, 28f);
        Widgets.Label(reusedRect, "OARO_OrderWin_Esteem".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (420f - 110f), inRectY + 698f, 100f, 28f);
        Widgets.Label(reusedRect, selectedOrder.Esteem.ToString());

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 64f, inRectY + 745f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_TotalKnightsCount".Translate());

        reusedRect = new(inRectX + 64f, inRectY + 770f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_TotalPopulation".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (420f - 110f), inRectY + 745f, 100f, 18f);
        Widgets.Label(reusedRect, totalKnightsCount.ToString());

        reusedRect = new(inRectX + (420f - 110f), inRectY + 770f, 100f, 18f);
        Widgets.Label(reusedRect, totalPopulation.ToString());

        OARO_WindowUtility.ResetText();
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
        Widgets.Label(reusedRect, selectedOrder.BranchManager.AllBranches.Count.ToString());

        reusedRect.xMax += 80f;
        reusedRect.xMin += 80f;
        Widgets.Label(reusedRect, branchesTypeCache.frienly.ToString());

        reusedRect.xMax += 80f;
        reusedRect.xMin += 80f;
        Widgets.Label(reusedRect, branchesTypeCache.honor.ToString());

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 385f, 346f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenBranchWindow".Translate(), squadButton, squadButton_Down, doMouseoverSound: true))
        {
            return;
        }
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 410f, 346f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenSquadWindow".Translate(), squadButton, squadButton_Down, doMouseoverSound: true))
        {
            Window_BranchSquad branchSquadWin = new(selectedOrder, map);
            Find.WindowStack.Add(branchSquadWin);
            Close();
            return;
        }

        reusedRect = new(inRectX, inRectY + 440f, inRectWidth, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_AverageSupply".Translate(averageSupply.ToStringPercent()));

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 470f, 346f, 140f);
        DrawFollowedBranchList(reusedRect);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX, inRectY + 621f, inRectWidth, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchDemand".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(inRectX + 246f, inRectY + 654f, 134f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenDemandWindow".Translate(), windowButton, windowButton_Down, doMouseoverSound: true))
        {
            Window_BranchDemand branchDemandWin = new(selectedOrder, map);
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
        Widgets.Label(reusedRect, "OARO_OrderWin_NormalDemandsInfo".Translate(normalDemandsCache.urgency.ToString(), normalDemandsCache.supplementary.ToString(), normalDemandsCache.acceptable.ToString()));

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderWin_CriticalDemandsInfo".Translate(criticalDemandsCache.friendly.ToString(), criticalDemandsCache.acceptable.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 35f, inRectY + 801f, inRectWidth - 35f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_CompletedDemands".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 801f, inRectWidth - 35f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_CompletedDemandsInfo".Translate(selectedOrder.BranchManager.CriticalDemandFulfillCount.ToString(), selectedOrder.BranchManager.NormalDemandFulfillCount.ToString()));

        OARO_WindowUtility.ResetText();
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
        Widgets.Label(reusedRect, "OARO_OrderWin_OrderTaskInfo".Translate(notIdleBranchCount.ToString()));

        reusedRect = new(inRectX + 246f, inRectY + 80f, 134f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenTaskWindow".Translate(), windowButton, windowButton_Down, doMouseoverSound: true))
        {
            // 尚未实现 //
        }

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 110f, inRectWidth - 36f, 20f);
        Widgets.Label(reusedRect, selectedOrder.JointPatrolManager.TickToNextStage.ToStringTicksToPeriod());

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 36f, inRectY + 136f, inRectWidth - 36f, 20f);
        Widgets.Label(reusedRect, "OARO_OrderWin_JointPatrolExpectedResult".Translate());

        switch (selectedOrder.JointPatrolManager.CurState)
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

        OARO_WindowUtility.ResetText();
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
        IReadOnlyList<Branch> followedBranches = selectedOrder.BranchManager.FollowedBranches;
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
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, 120f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_ChangeFollowedBranches".Translate(), changeFollowedBranchesButton, changeFollowedBranchesButton_Down, doMouseoverSound: true))
        {

        }
        OARO_WindowUtility.ResetText();
    }

    private void DrawFollowedBranch(Rect inRect, Branch branch)
    {
        float inRectX = inRect.x;

        Rect reusedRect = new(inRectX, inRect.y + 1f, 5f, inRect.height - 2f);
        GUI.DrawTexture(reusedRect, branch.HonorDef?.HonorBarTexture ?? IconLibrary.BarTex_White);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, reusedRect.xMax + 3f, 128f, 20f);
        Widgets.LabelEllipses(reusedRect, branch.Name);

        reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, reusedRect.xMax + 3f, 40f, 20f);
        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            Widgets.LabelEllipses(reusedRect, "OARO_OrderWin_FollowedBranchFriendly".Translate().Colorize(Color.green));
        }
        else if (branch.IsBranchOfType(Branch.BranchType.Honor))
        {
            Widgets.LabelEllipses(reusedRect, "OARO_OrderWin_FollowedBranchHonor".Translate().Colorize(Color.yellow));
        }

        if (branch.IsIdleNow)
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, reusedRect.xMax + 12f, inRect.height - 2f, inRect.height - 2f);
            GUI.DrawTexture(reusedRect, IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
        }

        if (branch.DemandHandler.NormalDemand is not null)
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.xMax - 35f, 35f, 25f);
            GUI.DrawTexture(reusedRect, normalDemandFlag);
        }

        if (branch.DemandHandler.CriticalDemand is not null)
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.xMax - (35f + 25f), 35f, 25f);
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
        Rect reusedRect = new(inRectX, inRectY + 101f, inRectWidth, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchConstruction".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(inRectX, inRectY + 133f, inRectWidth, 18f);
        Widgets.Label(reusedRect, "OARO_OrderWin_ConstructionBusyBarnchesCount".Translate(constructionBusyBarnchesCount.ToString()));


        reusedRect = new(inRectX + 269f, inRectY + 178f, 134f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_OpenBranchWindow".Translate(), windowButton, windowButton_Down, doMouseoverSound: true))
        {

        }

        float entryX = inRectX + 30f;
        float entryY = inRectY + 204f;
        foreach (KeyValuePair<Branch, BranchStoresReserveHandler.ReserveRecord> kv in reserveRecordShow)
        {
            Rect entryRect = new(entryX, entryY, 373f, 54f);
            entryY += 54f;
            DrawStoresReserveRect(entryRect, kv.Key, kv.Value);
        }

        reusedRect = new(inRectX + 36f, inRectY + 337f, 134f, 52f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OrderWin_BranchConstructionButton".Translate(), constructButton, constructButton_Down, doMouseoverSound: true))
        {

        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        if (underConstructionBuilding.Item1 is not null)
        {
            reusedRect = new(inRectX + 173f, inRectY + 339f, 55f, 48f);
            GUI.DrawTexture(reusedRect, underConstructionBuilding.Item2.TargetDef.IconTexture, ScaleMode.ScaleToFit);
            reusedRect = new(inRectX + 250f, inRectY + 341f, 128f, 20f);
            Widgets.Label(reusedRect, underConstructionBuilding.Item1.Name);
            reusedRect = new(inRectX + 250f, inRectY + 366f, 128f, 20f);
            Widgets.Label(reusedRect, "WaitTime".Translate(underConstructionBuilding.Item2.DurationTicksLeft.ToStringTicksToPeriod()));

        }
        else if (underConstructionFacility.Item1 is not null)
        {
            reusedRect = new(inRectX + 173f, inRectY + 339f, 55f, 48f);
            GUI.DrawTexture(reusedRect, underConstructionFacility.Item2.TargetDef.IconTexture, ScaleMode.ScaleToFit);
            reusedRect = new(inRectX + 250f, inRectY + 341f, 128f, 20f);
            Widgets.Label(reusedRect, underConstructionFacility.Item1.Name);
            reusedRect = new(inRectX + 250f, inRectY + 366f, 128f, 20f);
            Widgets.Label(reusedRect, "WaitTime".Translate(underConstructionFacility.Item2.DurationTicksLeft.ToStringTicksToPeriod()));
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
        reusedRect = new(inRectX + 36f, inRectY + 391f, 369f, 50f);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: string.Empty,
            acceptance: GetIndependentAcceptanceReport(OrderInteractionDefOf.OARO_InviteBranchCreation),
            baseTex: inviteBranchCreationButton,
            downTex: inviteBranchCreationButton_Down,
            doMouseoverSound: true))
        {
            Close();
            OrderInteractionDefOf.OARO_InviteBranchCreation.TryApplyInteraction(selectedOrder, map);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRectX + 45f, 192f, 20f);
        Widgets.Label(reusedRect, OrderInteractionDefOf.OARO_InviteBranchCreation.label);

        reusedRect = new(inRectX + 298f, inRectY + 396f, 100f, 20f);
        Widgets.DefIcon(new(reusedRect.x, reusedRect.y, 20f, 20f), ThingDefOf.Silver, graphicIndexOverride: 2);
        reusedRect.xMin += 22f;
        Widgets.Label(reusedRect, $"× {selectedOrder.BranchManager.SilverNeededForNextBranchCreation}");

        reusedRect = new(inRectX + 298f, inRectY + 416f, 100f, 20f);
        OARO_WindowUtility.DrawRecommendationInfo(reusedRect, 1, textOffset: 2f);

        OARO_WindowUtility.ResetText();
    }

    private void DrawStoresReserveRect(Rect inRect, Branch branch, BranchStoresReserveHandler.ReserveRecord reserveRecord)
    {
        GUI.DrawTexture(inRect, rightUpFrame);

        Rect reusedRect = new(inRect.x, inRect.y + 2f, 2f, inRect.height - 4f);
        GUI.DrawTexture(reusedRect, branch.HonorDef?.HonorBarTexture ?? IconLibrary.BarTex_White);

        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(innerRectX + 4f, innerRectY, 252f - 8f, 24f);
        Widgets.Label(reusedRect, branch.Name);

        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchFacilityLevel".Translate(branch.FacilityHandler.TotalFacilityLevel.Value.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(innerRectX + 4f, innerRectY + 25f, 252f - 8f, 24f);
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchPopulation".Translate(branch.PopulationHandler.Population.ToString()));

        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, "OARO_OrderWin_BranchBuildingLimit".Translate(branch.BuildingHandler.AllBuldingsCount.ToString(), (branch.BuildingHandler.BuildingCeiling + 1).ToString()));


        Rect progressRect = new(innerRectX + 254f, innerRectY, 115f, innerRect.height);

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(progressRect.x + 2f, progressRect.y + 2f, progressRect.width - 2f, 12f);
        Widgets.Label(reusedRect, "OARO_OrderWin_StoresReserve".Translate());
        reusedRect = new(progressRect.x + 2f, progressRect.yMax - (2f + 12f), progressRect.width - 2f, 12f);
        Widgets.Label(reusedRect, "OARO_OrderWin_StoresReserveReduce".Translate(reserveRecord.CostRateReduce.ToStringPercent()));
        reusedRect = OARO_WindowUtility.CenterRectOnY(progressRect, progressRect.xMin + 50f, 50f, 48f);
        GUI.DrawTexture(reusedRect, reserveRecord.Target.IconTexture, ScaleMode.ScaleToFit);

        OARO_WindowUtility.ResetText();
    }

    private void RefreshRatkinOrderCache()
    {
        ClearRatkinOrderCache();
        if (selectedOrder is null)
        {
            return;
        }

        esteemTexture = new CachedTexture($"UI/RatkinOrder/OARO_EsteemTexture_{EsteemUtility.GetIndex(selectedOrder.Esteem)}");
        IReadOnlyList<Branch> allBranches = selectedOrder.BranchManager.AllBranches;
        foreach (Branch branch in allBranches)
        {
            totalKnightsCount += branch.Squad.AllCrewCountInt;
            totalPopulation += branch.PopulationHandler.Population;
            averageSupply += branch.Supply;
            if (!branch.IsIdleNow)
            {
                notIdleBranchCount++;
            }
            if (branch.IsConstructionBusy)
            {
                constructionBusyBarnchesCount++;
            }

            if (branch.IsBranchOfType(Branch.BranchType.Friendly))
            {
                branchesTypeCache.frienly++;
            }
            if (branch.IsBranchOfType(Branch.BranchType.Honor))
            {
                branchesTypeCache.honor++;
            }

            try
            {
                BranchDemand demand = branch.DemandHandler.NormalDemand;
                if (demand is not null)
                {
                    switch (demand.DemandTypeValue)
                    {
                        case BranchDemand.DemandType.Urgency: normalDemandsCache.urgency++; break;
                        case BranchDemand.DemandType.Supplementary: normalDemandsCache.supplementary++; break;
                        default: break;
                    }
                    if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: false, resultOnly: true))
                    {
                        normalDemandsCache.acceptable++;
                    }
                }
                demand = branch.DemandHandler.CriticalDemand;
                if (demand is not null)
                {
                    if (branch.IsBranchOfType(Branch.BranchType.Friendly))
                    {
                        criticalDemandsCache.friendly++;
                    }
                    if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: true, resultOnly: true))
                    {
                        criticalDemandsCache.acceptable++;
                    }
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "refresh order demands cache",
                    typeName: nameof(Window_RatkinOrder),
                    methodName: nameof(RefreshRatkinOrderCache),
                    needStackTrace: true);
            }
        }

        averageSupply /= allBranches.Count;

        underConstructionFacility = allBranches.Where(b => b.FacilityHandler.IsBusy).Select(b => (b, b.FacilityHandler.UnderConstructionFacility)).FirstOrFallback();
        underConstructionBuilding = allBranches.Where(b => b.BuildingHandler.IsBusy).Select(b => (b, b.BuildingHandler.UnderConstructionBuilding)).FirstOrFallback();
        reserveRecordShow = selectedOrder.BranchManager.AllPrimaryReserves.Take(2).ToList();

        RefreshRatkinInteractionCache(null, selectedOrder, map, succeeded: false);

        selectedOrder.PostApplyOrderInteraction -= RefreshRatkinInteractionCache;
        selectedOrder.PostApplyOrderInteraction += RefreshRatkinInteractionCache;
    }

    private void RefreshRatkinInteractionCache(OrderInteractionDef interactionDef, RatkinOrder ratkinOrder, Map map, bool succeeded)
    {
        independentInteractionAcceptances.Clear();
        otherInteractionAcceptances.Clear();
        if (selectedOrder is null)
        {
            return;
        }

        HashSet<OrderInteractionDef> independentInteractions =
            [
                OrderInteractionDefOf.OARO_EnhanceRelationship,
                OrderInteractionDefOf.OARO_SponsorOrder,
                OrderInteractionDefOf.OARO_InviteBranchCreation
            ];

        foreach (OrderInteractionDef def in independentInteractions)
        {
            try
            {
                AcceptanceReport acceptanceReport = def.CanUseInteraction(selectedOrder, this.map, resultOnly: false);
                independentInteractionAcceptances.Add(def, acceptanceReport);
            }
            catch
            {
                Log.ErrorOnce($"[OARO] An exception occurred in {nameof(Window_RatkinOrder)}.{nameof(RefreshRatkinInteractionCache)}", 8645612);
            }
        }

        foreach (OrderInteractionDef def in DefDatabase<OrderInteractionDef>.AllDefsListForReading)
        {
            if (!independentInteractions.Contains(def))
            {
                try
                {
                    AcceptanceReport acceptanceReport = def.CanUseInteraction(selectedOrder, this.map, resultOnly: false);
                    otherInteractionAcceptances.Add(new KeyValuePair<OrderInteractionDef, AcceptanceReport>(def, acceptanceReport));
                }
                catch
                {
                    Log.ErrorOnce($"[OARO] An exception occurred in {nameof(Window_RatkinOrder)}.{nameof(RefreshRatkinInteractionCache)}", 8645613);
                }
            }
        }
    }

    private AcceptanceReport GetIndependentAcceptanceReport(OrderInteractionDef def)
    {
        if (independentInteractionAcceptances.TryGetValue(def, out AcceptanceReport acceptance))
        {
            return acceptance;
        }
        return false;
    }

    private void ClearRatkinOrderCache()
    {
        if (selectedOrder is not null)
        {
            selectedOrder.PostApplyOrderInteraction -= RefreshRatkinInteractionCache;
        }

        mapRecommendationCount.MarkDirty();
        esteemTexture = new CachedTexture("UI/RatkinOrder/OARO_EsteemTexture_0");

        totalKnightsCount = 0;
        totalPopulation = 0;
        averageSupply = 0f;
        notIdleBranchCount = 0;
        constructionBusyBarnchesCount = 0;

        branchesTypeCache = default;
        normalDemandsCache = default;
        criticalDemandsCache = default;

        independentInteractionAcceptances.Clear();
        otherInteractionAcceptances.Clear();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_MainBackground");

    private static readonly Texture2D orderSelButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_OrderSelButton");
    private static readonly Texture2D orderSelButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_OrderSelButton_Down");
    private static readonly Texture2D leftBigButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_LeftBigButton");
    private static readonly Texture2D leftBigButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_LeftBigButton_Down");
    private static readonly Texture2D annualFirstSponsorButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_AnnualFirstSponsorButton");
    private static readonly Texture2D annualFirstSponsorButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_AnnualFirstSponsorButton_Down");
    private static readonly Texture2D squadButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_SquadButton");
    private static readonly Texture2D squadButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_SquadButton_Down");
    private static readonly Texture2D constructButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ConstructButton");
    private static readonly Texture2D constructButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ConstructButton_Down");
    private static readonly Texture2D inviteBranchCreationButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_InviteBranchCreationButton");
    private static readonly Texture2D inviteBranchCreationButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_InviteBranchCreationButton_Down");
    private static readonly Texture2D changeFollowedBranchesButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ChangeFollowedBranchesButton");
    private static readonly Texture2D changeFollowedBranchesButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_ChangeFollowedBranchesButton_Down");

    private static readonly Texture2D rightUpFrame = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_RightUpFrame");

    private static readonly Texture2D windowButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_WindowButton");
    private static readonly Texture2D windowButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_WindowButton_Down");

    private static readonly Texture2D normalDemandFlag = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_NormalDemandFlag");
    private static readonly Texture2D criticalDemandFlag = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_CriticalDemandFlag");
}