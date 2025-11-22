using NightOcean;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

internal class Window_RatkinOrder : MainTabWindow
{
    public override Vector2 InitialSize => new(1337f, 944f);
    public override Vector2 RequestedTabSize => new(1337f, 944f);

    private readonly Map map;
    private readonly LazyMutable<int> mapRecommendationCount;
    private RatkinOrder selectedOrder;
    private Vector2 scrollPosition_Orders;

    private int totalKnightsCount;
    private int totalPopulation;
    private float averageSupply;
    private float notIdleBranchCount;
    private (int frienly, int honor) branchesTypeCache;
    private (int normal, int urgency, int supplementary, int acceptable) normalDemandsCache;
    private (int critical, int acceptable) criticalDemandsCache;

    public Window_RatkinOrder(Map map)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        mapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationOfMap(selectedOrder, this.map));
    }

    ~Window_RatkinOrder()
    {

    }


    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect ratkinOrderRect = new(inRect.x, inRect.y, inRect.width, 37f);
        DrawRatkinOrder(ratkinOrderRect);

        Rect mainRect = new(inRect.x, ratkinOrderRect.yMax, inRect.width, inRect.height - ratkinOrderRect.height);
        Rect mainInnerRect = mainRect.ContractedBy(3f);
        float mainInnerRectX = mainInnerRect.xMin;
        float mainInnerRectY = mainInnerRect.yMin;
        GUI.DrawTexture(mainRect, mainBackground);

        Rect leftRect = new(mainInnerRectX, mainInnerRectY, 455f, mainInnerRect.height);
        DrawLeftRect(leftRect);

        Rect middleRect = Rect.MinMaxRect(mainInnerRectX + 456f, mainInnerRectY, mainInnerRectX + 869f, mainInnerRect.yMax);
        DrawMiddleRect(middleRect);



    }

    private void DrawRatkinOrder(Rect inRect)
    {
        float entryX = inRect.x;
        float entryY = inRect.y;
        float entryWidth = 125f;
        float entryHeight = inRect.height;

        Rect viewRect = inRect;
        viewRect.width = entryWidth * RatkinOrderManager.AllRatkinOrders.Count;

        Widgets.BeginScrollView(inRect, ref scrollPosition_Orders, viewRect, showScrollbars: false);
        Rect entryRect;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        foreach (RatkinOrder ratkinOrder in RatkinOrderManager.AllRatkinOrders)
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

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 408f, 100f, 18f);
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, "OARO_CurOrderRelationship".Translate());

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 430f, 100f, 24f);
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, selectedOrder.Relationship.GetLabel().Colorize(selectedOrder.Relationship.GetColor()));

        Text.Font = GameFont.Small;
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 461f, 149f, 59f);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: OrderInteractionDefOf.OARO_EnhanceRelationship.LabelCap,
            acceptance: OrderInteractionDefOf.OARO_EnhanceRelationship.CanUseInteraction(selectedOrder, map, resultOnly: false),
            baseTex: leftBigButton,
            downTex: leftBigButton_Down,
            doMouseoverSound: true))
        {
            OrderInteractionDefOf.OARO_EnhanceRelationship.TryApplyInteraction(selectedOrder, map);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 86f, inRectY + 542f, 128f, 16f);
        Widgets.Label(reusedRect, "OARO_CurOrderFund".Translate());

        reusedRect = new(inRectX + 303f, inRectY + 542f, 128f, 16f);
        Widgets.Label(reusedRect, "OARO_CurRecommendationLetter".Translate());

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
                acceptance: OrderInteractionDefOf.OARO_SponsorOrder.CanUseInteraction(selectedOrder, map, resultOnly: false),
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
                acceptance: OrderInteractionDefOf.OARO_SponsorOrder.CanUseInteraction(selectedOrder, map, resultOnly: false),
                baseTex: annualFirstSponsorButton,
                downTex: annualFirstSponsorButton_Down,
                doMouseoverSound: true))
            {
                OrderInteractionDefOf.OARO_SponsorOrder.TryApplyInteraction(selectedOrder, map);
            }
        }

        reusedRect = new(inRectX + 270f, inRectY + 620f, 149f, 59f);
        if (false)
        {

        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 64f, inRectY + 700f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_CurOrderEsteem".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX + (420f - 110f), inRectY + 700f, 100f, 24f);
        Widgets.Label(reusedRect, selectedOrder.Esteem.ToString());

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 64f, inRectY + 745f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_CurTotalKnightsCount".Translate());

        reusedRect = new(inRectX + 64f, inRectY + 770f, 256f, 24f);
        Widgets.Label(reusedRect, "OARO_CurTotalPopulation".Translate());

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

        Rect reusedRect = new(inRectX, inRectY + 98f, inRectWidth, 24f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_OrderTaskInfoStr".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(inRectX, inRectY + 132f, inRectWidth, 18f);
        Widgets.Label(reusedRect, "OARO_OrderTaskInfo".Translate(notIdleBranchCount.ToString()));

        reusedRect = new(inRectX + 246f, inRectY + 176f, 134f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OpenTaskWindow".Translate(), middleButton, middleButton_Down, doMouseoverSound: true))
        {

        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 36f, inRectY + 207f, 256f, 18f);
        Widgets.Label(reusedRect, "OARO_DayToNextJointPatrol".Translate());

        reusedRect = new(inRectX + 36f, inRectY + 234f, 256f, 18f);
        Widgets.Label(reusedRect, "OARO_JointPatrolExpectedResult".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 207f, inRectWidth - 35f, 18f);
        Widgets.Label(reusedRect, "xxxxxxxxxxxxxxxxxxxxxxxxxxxx");

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(inRectX, inRectY + 313f, inRectWidth, 24f);
        Widgets.Label(reusedRect, "OARO_OrderBranchInfoStr".Translate());

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 36f, inRectY + 360f, 96f, 18f);
        Widgets.Label(reusedRect, "OARO_OrderBranchCountStr".Translate());

        reusedRect = new(inRectX + 200f, inRectY + 360f, 42f, 18f);
        Widgets.Label(reusedRect, selectedOrder.BranchManager.AllBranches.Count.ToString());

        reusedRect.xMax += 80f;
        reusedRect.xMin += 80f;
        Widgets.Label(reusedRect, branchesTypeCache.frienly.ToString());

        reusedRect.xMax += 80f;
        reusedRect.xMin += 80f;
        Widgets.Label(reusedRect, branchesTypeCache.honor.ToString());

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 385f, 346f, 25f);
        Text.Anchor = TextAnchor.MiddleCenter;
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OpenSquadWindow", squadButton, squadButton_Down, doMouseoverSound: true))
        {
            Window_BranchSquad branchSquadWin = new(selectedOrder, map);
            Find.WindowStack.Add(branchSquadWin);
            Close();
            return;
        }

        reusedRect = new(inRectX, inRectY + 415f, inRectWidth, 18f);
        Widgets.Label(reusedRect, "OARO_OrderAverageSupply".Translate(averageSupply.ToStringPercent()));

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 470f, 346f, 140f);
        DrawFollowedBranch(reusedRect);

        reusedRect = new(inRectX, inRectY + 622f, inRectWidth, 18f);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_OrderBranchDemands".Translate());

        reusedRect = new(inRectX + 246f, inRectY + 654f, 134f, 25f);
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_OpenDemandWindow".Translate(), middleButton, middleButton_Down, doMouseoverSound: true))
        {
            Window_BranchDemand branchDemandWin = new(selectedOrder, map);
            Find.WindowStack.Add(branchDemandWin);
            Close();
            return;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 35f, inRectY + 685f, inRectWidth - 35f, 18f);
        Widgets.Label(reusedRect, "OARO_OrderAcceptedDemands".Translate());

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderNormalDemands".Translate());

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderCriticalDemands".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 685f, inRectWidth - 35f, 18f);
        Widgets.Label(reusedRect, $"{AcceptedBranchDemandHandler.Instance.AcceptanceCount}/{RatkinOrderSettings.MaxConcurrentAcceptedDemand}");

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderNormalDemandsInfo".Translate(normalDemandsCache.normal.ToString(), normalDemandsCache.urgency.ToString(), normalDemandsCache.supplementary.ToString(), normalDemandsCache.acceptable.ToString()));

        reusedRect.yMax += 26f;
        reusedRect.yMin += 26f;
        Widgets.Label(reusedRect, "OARO_OrderCriticalDemandsInfo".Translate(criticalDemandsCache.critical.ToString(), criticalDemandsCache.acceptable.ToString()));

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(inRectX + 35f, inRectY + 802f, inRectWidth - 35f, 18f);
        Widgets.Label(reusedRect, "OARO_OrderCompletedDemands".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = new(inRectX, inRectY + 802f, inRectWidth - 35f, 18f);
        Widgets.Label(reusedRect, "OARO_OrderCompletedDemandsInfo".Translate(selectedOrder.BranchManager.NormalDemandFulfillCount.ToString(), selectedOrder.BranchManager.NormalDemandFulfillCount.ToString()));

        OARO_WindowUtility.ResetText();
    }

    private void DrawFollowedBranch(Rect inRect)
    {

    }

    private void RefreshRatkinOrderCache()
    {
        ClearRatkinOrderCache();
        if (selectedOrder is null)
        {
            return;
        }

        foreach (Branch branch in selectedOrder.BranchManager.AllBranches)
        {
            totalKnightsCount += branch.Squad.AllCrewCountInt;
            totalPopulation += branch.PopulationHandler.Population;
            averageSupply += branch.Supply;
            if (!branch.IsIdleNow)
            {
                notIdleBranchCount++;
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
                        case BranchDemand.DemandType.Normal: normalDemandsCache.normal++; break;
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
                    criticalDemandsCache.critical++;
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

        averageSupply /= selectedOrder.BranchManager.AllBranches.Count;
    }

    private void RefreshRatkinOrderCache(Branch branchUseless, bool isCritical)
    {
        if (selectedOrder is null)
        {
            return;
        }

        foreach (Branch branch in selectedOrder.BranchManager.AllBranches)
        {
            BranchDemand demand = branch.DemandHandler.NormalDemand;
            if (demand is not null)
            {
                switch (demand.DemandTypeValue)
                {
                    case BranchDemand.DemandType.Normal: normalDemandsCache.normal++; break;
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
                criticalDemandsCache.critical++;
                if (BranchDemandUtility.CanAcceptDemand(branch, isCritical: true, resultOnly: true))
                {
                    criticalDemandsCache.acceptable++;
                }
            }
        }
    }

    private void ClearRatkinOrderCache()
    {
        mapRecommendationCount.MarkDirty();
        totalKnightsCount = 0;
        totalPopulation = 0;
        averageSupply = 0f;
        notIdleBranchCount = 0;

        branchesTypeCache = default;
        normalDemandsCache = default;
        criticalDemandsCache = default;
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_MainBackground");

    private static readonly Texture2D orderSelButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_OrderSelButton");
    private static readonly Texture2D orderSelButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_OrderSelButton_Down");
    private static readonly Texture2D leftBigButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_LeftBigButtonButton");
    private static readonly Texture2D leftBigButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_LeftBigButton_Down");
    private static readonly Texture2D annualFirstSponsorButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_AnnualFirstSponsorButton");
    private static readonly Texture2D annualFirstSponsorButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_AnnualFirstSponsorButton_Down");
    private static readonly Texture2D middleButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_MiddleButton");
    private static readonly Texture2D middleButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_MiddleButton_Down");
    private static readonly Texture2D squadButton = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_SquadButton");
    private static readonly Texture2D squadButton_Down = ContentFinder<Texture2D>.Get("UI/RatkinOrder/OARO_SquadButton_Down");
}
