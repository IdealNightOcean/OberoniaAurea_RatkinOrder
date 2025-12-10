using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_BranchSquad : OrderWindowBase
{
    private enum TabType
    {
        All,
        Near,
        Friendly
    }
    private static readonly List<BranchInteractionDef> SquadNeedBranchInteractions =
    [
        BranchInteractionDefOf.OARO_RequestCombatReadiness,
        BranchInteractionDefOf.OARO_MapRecommendationToKnight,
        BranchInteractionDefOf.OARO_MapSilverToSupply,
        BranchInteractionDefOf.OARO_UnlockSupportAuthority
    ];
    public override Vector2 InitialSize => new(1627f, 944f);

    private Vector2 scrollPosition_Squads;
    private Vector2 scrollPosition_Medals;

    private RatkinOrder RatkinOrder { get; }
    private Map Map { get; }
    private LazyMutable<int> MapRecommendationCount { get; }

    private int SelBranchIndex { get; set; }
    private SquadInfoUICache SelSquadInfo { get; set; }
    private Branch SelBranch => SelSquadInfo?.Branch;

    private TabType CurTab { get; set; } = TabType.All;
    private List<TabRecord> Tabs { get; } = new(3);

    private List<BranchSummaryUICache> BranchSummaryCaches { get; }
    private List<BranchSummaryUICache> TabSummaryCaches { get; }

    private LazyMutable<string> SupplyRecoveryRateExplanation { get; }
    private LazyMutable<int> BombardSupportCeiling { get; }
    private LazyMutable<string> BombardSupportCeilingExplanation { get; }
    public LazyMutable<AcceptanceReport> SupportAcceptance { get; }
    public LazyMutable<AcceptanceReport> BombardAcceptance { get; }
    private LazyMutable<Dictionary<BranchInteractionDef, AcceptanceReport>> InteractionAcceptances { get; }

    public Window_BranchSquad(RatkinOrder ratkinOrder, Map map) : base()
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        Map = map ?? throw new ArgumentNullException(nameof(map));

        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationOfMap(RatkinOrder, Map));

        SupplyRecoveryRateExplanation = new(refreshFunc: () => BranchStatUtility.GetStatModifyExplanationStr(SelBranch, BranchStatDefOf.OARO_SupplyRecoveryRate, showResultValue: true));
        BombardSupportCeiling = new(refreshFunc: () => (int)(SelBranch?.GetStatValue(BranchStatDefOf.OARO_BombardSupportCeiling, immediateUpdate: true) ?? -1f));
        BombardSupportCeilingExplanation = new(refreshFunc: () => BranchStatUtility.GetStatModifyExplanationStr(SelBranch, BranchStatDefOf.OARO_BombardSupportCeiling, showResultValue: true));

        SupportAcceptance = new(refreshFunc: () => BranchSupportUtility.CanCombatKnightSupport(SelBranch, Map, BranchSupportUtility.DeploymentLevel.Quarter, resultOnly: false));
        BombardAcceptance = new(refreshFunc: () => BranchSupportUtility.CanBombard(SelBranch, Map, resultOnly: false));
        InteractionAcceptances = new(refreshFunc: RefreshInteractionAcceptances);

        BranchSummaryCaches = new(ratkinOrder.BranchManager.AllBranches.Count);
        TabSummaryCaches = new(ratkinOrder.BranchManager.AllBranches.Count);
        RecacheBranchSummary();
    }

    public override void PostClose()
    {
        base.PostClose();
        DeselectSquad();
        BranchSummaryCaches.Clear();
        TabSummaryCaches.Clear();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1544f, 901f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(3f);

        float mainInnerRectY = mainInnerRect.yMin;
        float areaRectY = mainInnerRectY + 205f;
        float areaRectHeight = 657f;

        Rect reusedRect = new(mainInnerRect.xMax - 21f, mainInnerRectY + 1f, 20f, 20f);
        if (Widgets.ButtonImage(reusedRect, IconLibrary.colseX, doMouseoverSound: true))
        {
            Close();
            return;
        }

        //顶部缎带
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 4f, 1567f, 136f);
        GUI.DrawTexture(reusedRect, topRibbon);

        //顶部标题框
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 12f, 322f, 90f);
        GUI.DrawTexture(reusedRect, topTitleground);

        reusedRect = OARO_WindowUtility.CenterRect(reusedRect, 128f, 64f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_SquadWin_SquadWindowTitle".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        //分部名称
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 150f, 322f, 48f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, SelSquadInfo.SquadName);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        Rect middleRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, areaRectY, 583f, areaRectHeight);
        if (DrawMiddleRect(middleRect))
        {
            Close();
            return;
        }

        //左|中分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(middleRect, middleRect.x - (32f + 2f), 2f, 717f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);

        Rect leftRect = new(reusedRect.xMin - (12f + 415f), areaRectY, 415f, areaRectHeight);
        DrawLeftRect(leftRect);

        //中|右分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(middleRect, middleRect.xMax + 32f, 2f, 717f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);


        Rect rightRect = new(reusedRect.xMax + 32f, areaRectY, 352f, areaRectHeight);
        //小队科技
        reusedRect = OARO_WindowUtility.CenterRectOnX(rightRect, mainInnerRectY + 150f, 322f, 48f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_SquadWin_SquadTech".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        DrawRightRect(rightRect);


        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private bool DrawMiddleRect(Rect inRect)
    {
        float inRectX = inRect.x;
        float inRectY = inRect.y;
        float frameWidth = 2f;
        float rectWidth = inRect.width;
        float rectHeight = 150f;

        Rect reusedRect;

        //名称|内容分割线
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY, 579f, 2f);
        GUI.DrawTexture(reusedRect, middleCuttingLine);

        reusedRect = new(inRectX, reusedRect.yMax + 10f, 23f, 32f);
        GUI.DrawTexture(reusedRect, branchBaseSiteIcon);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 12f, 88f, 48f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, SelSquadInfo.BaseSiteName);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 24f, 60f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        if (OARO_WindowUtility.TextButtonImageDisableable(butRect: reusedRect,
                                                          label: "OAFrame_LookOver".Translate(),
                                                          baseTex: middleCheckButton,
                                                          downTex: middleCheckButton_Down,
                                                          acceptance: SelBranch.IsValid(),
                                                          doMouseoverSound: true))
        {
            CameraJumper.TryJumpAndSelect(SelBranch.BaseSite);
            return true;
        }

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRect.xMax - 192f, 192f, 32f);
        if (SelBranch.IsValid())
        {
            Widgets.Label(reusedRect, "OARO_SquadWin_SiteDistance".Translate(SelSquadInfo.Distance.ToString("F0"))
                                                                  .Colorize(SelSquadInfo.IsInAffectedRange ? Color.green : Color.white));
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_SquadWin_SiteDistanceN".Translate());
        }

        Text.Anchor = TextAnchor.MiddleCenter;

        Rect upRect = new(inRectX, inRectY + 80f, rectWidth, rectHeight);
        GUI.DrawTexture(upRect, middleUpBackground, ScaleMode.ScaleToFit);
        Rect upInnerRect = upRect.ContractedBy(frameWidth);

        BranchHonorDef honorDef = SelBranch?.HonorDef;
        bool selIsHonor = honorDef is not null && SelBranch.IsBranchOfType(BranchType.Honor);

        //上部左侧颜色条
        reusedRect = OARO_WindowUtility.CenterRectOnY(upInnerRect, upInnerRect.x, 6f, 144f);
        if (selIsHonor)
        {
            GUI.DrawTexture(reusedRect, honorDef.HonorColorTex);
        }

        //上部左侧部分
        Rect areaRect = new(reusedRect.xMax, upInnerRect.y, 245f, upInnerRect.height);
        reusedRect = OARO_WindowUtility.CenterRectOnY(areaRect, areaRect.x, 240f, areaRect.height - 5f);
        if (selIsHonor)
        {
            GUI.DrawTexture(reusedRect, honorDef.backgroundTexture.Texture);

            reusedRect = OARO_WindowUtility.CenterRect(areaRect, 230f, 130f);
            GUI.DrawTexture(reusedRect, honorDef.decorationTexture.ExpandedTexture, ScaleMode.ScaleToFit);

            reusedRect = OARO_WindowUtility.CenterRect(areaRect, 190f, 107f);
            GUI.DrawTexture(reusedRect, honorDef.iconTexture.ExpandedTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            reusedRect = OARO_WindowUtility.CenterRect(areaRect, 62f, 68f);
            GUI.DrawTexture(reusedRect, IconLibrary.BigGeneralBranchIcon, ScaleMode.ScaleToFit);
        }

        //上部右侧部分
        areaRect = Rect.MinMaxRect(areaRect.xMax + 2f, upInnerRect.yMin, upInnerRect.xMax, upInnerRect.yMax);
        DrawMedalRect(areaRect);

        //上部丝带
        areaRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 70f, 629f, 101f);
        GUI.DrawTexture(areaRect, middleUpRibbon);

        //中部
        Rect middleRect = new(inRectX, upRect.yMax + 32f, rectWidth, rectHeight);
        GUI.DrawTexture(middleRect, middleMiddleBackground, ScaleMode.ScaleToFit);
        Rect middleInnerRect = middleRect.ContractedBy(frameWidth);

        float middleUpHeight = 106f;
        float middleBottomHeight = 38f;
        //中部左上区域
        areaRect = new(middleInnerRect.x, middleInnerRect.y, 260f, middleUpHeight);
        reusedRect = new(areaRect.x + 8f, areaRect.y + 6f, 72f, 24f);
        Widgets.Label(reusedRect, "OARO_SquadWin_Relation".Translate());

        TaggedString relation;
        float friendlyProcess;
        TaggedString friendlyExpireDate;

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, areaRect.y + 40f, 43f, 51f);
        if (SelBranch?.IsBranchOfType(BranchType.Friendly) ?? false)
        {
            GUI.DrawTexture(reusedRect, IconLibrary.BigFriendlyIcon, ScaleMode.ScaleToFit);

            relation = "OARO_Friendly".Translate().Colorize(Color.green);
            friendlyProcess = SelSquadInfo.FriendlyProcess;
            friendlyExpireDate = "OARO_SquadWin_UntilDate".Translate() + $"   {SelSquadInfo.FriendlyExpireDateStr}";
        }
        else
        {
            GUI.DrawTexture(reusedRect, IconLibrary.BigStrangeIcon, ScaleMode.ScaleToFit);

            relation = "OARO_Strange".Translate();
            friendlyProcess = 0f;
            friendlyExpireDate = "OARO_SquadWin_UntilDate".Translate() + "   ---";
        }

        reusedRect = new(middleInnerRect.x + 136f, middleInnerRect.y + 4f, 54f, 32f);
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, relation);
        Text.Font = GameFont.Small;

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, areaRect.y + 40f, 120f, 24f);
        Widgets.FillableBar(reusedRect, friendlyProcess, IconLibrary.GreenTex, BaseContent.BlackTex, doBorder: true);

        reusedRect = new(reusedRect.x, reusedRect.yMax + 8f, 120f, 24f);
        Widgets.Label(reusedRect, friendlyExpireDate);

        //中部左下区域
        areaRect = new(areaRect.x, middleInnerRect.yMax - middleBottomHeight, 260f, middleBottomHeight);
        reusedRect = areaRect;
        reusedRect.xMax = areaRect.x + 100f;
        Widgets.Label(reusedRect, "OARO_SquadWin_ColonistsMember".Translate());

        reusedRect.xMin = reusedRect.xMax + 8f;
        reusedRect.xMax = areaRect.xMax - 2f;
        if (OARO_WindowUtility.TextButtonImageDisableable(butRect: reusedRect,
                                                          label: "OARO_SquadWin_ClickToAdd".Translate(),
                                                          baseTex: middleClickToAddButton,
                                                          downTex: middleClickToAddButton_Down,
                                                          acceptance: SelBranch.IsValid(),
                                                          doMouseoverSound: true))
        {

        }

        //中部中上区域
        areaRect = new(areaRect.xMax + 2f, middleInnerRect.y, 128f, middleUpHeight);
        reusedRect = new(areaRect.x, areaRect.y + 4f, 128f, 24f);
        Widgets.Label(reusedRect, "OARO_BranchSupplyState".Translate());

        if (SelBranch.IsValid())
        {
            switch (SelBranch.Supply)
            {
                case < 0.2f:
                    reusedRect = OARO_WindowUtility.CenterRectOnX(areaRect, reusedRect.yMax + 26f, 20f, 19f);
                    GUI.DrawTexture(reusedRect, branchSupplyLack);
                    break;

                case < 0.8f:
                    reusedRect = OARO_WindowUtility.CenterRectOnX(areaRect, reusedRect.yMax + 6f, 62f, 39f);
                    GUI.DrawTexture(reusedRect, branchSupplyJust);
                    break;

                default:
                    reusedRect = OARO_WindowUtility.CenterRectOnX(areaRect, reusedRect.yMax + 6f, 104f, 39f);
                    GUI.DrawTexture(reusedRect, branchSupplyEnough);
                    break;
            }
            reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, areaRect.yMax - (24f + 2f), 128f, 24f);
            Widgets.Label(reusedRect, (SelBranch.SupplyState + "   " + SelBranch.Supply.ToStringPercent("F0")));
            TooltipHandler.TipRegion(reusedRect, () => SupplyRecoveryRateExplanation.Value, uniqueId: 61486431);
        }
        else
        {
            reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, areaRect.yMax - (24f + 2f), 128f, 24f);
            Widgets.Label(reusedRect, "--   --%");
        }

        //中部中下区域
        areaRect = new(areaRect.x, middleInnerRect.yMax - middleBottomHeight, 128f, middleBottomHeight);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: areaRect,
            label: string.Empty,
            acceptance: InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_MapSilverToSupply),
            baseTex: middleCombatReadinessButton,
            downTex: middleCombatReadinessButton_Down,
            doMouseoverSound: true))
        {
            BranchInteractionParms parms = new(SelBranch, Map);
            AcceptanceReport acceptanceReport = BranchInteractionDefOf.OARO_MapSilverToSupply.Worker.CanUseInteraction(parms, resultOnly: false);
            if (acceptanceReport)
            {
                BranchInteractionDefOf.OARO_MapSilverToSupply.Worker.TryApplyInteraction(parms);
            }
            else
            {
                InteractionAcceptances.MarkDirty();
                Messages.Message(
                    text: "OARO_CanApplyBranchInteractionWithReason".Translate(
                        parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName),
                        BranchInteractionDefOf.OARO_MapSilverToSupply.Named("INTERACTION"),
                        acceptanceReport.Reason.Named("Reason")),
                    def: MessageTypeDefOf.RejectInput,
                    historical: false);
            }
        }
        reusedRect = new(areaRect.x + 16f, areaRect.y + 8f, 40f, 24f);
        Widgets.ThingIcon(reusedRect, ThingDefOf.Silver, graphicIndexOverride: 2);

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(reusedRect.xMax + 8f, areaRect.y + 8f, 100f, 24f);
        Widgets.Label(reusedRect, BranchInteractionDefOf.OARO_MapSilverToSupply.label);

        //中部右上区域
        areaRect = new(areaRect.xMax + 2f, middleInnerRect.y, 185f, middleUpHeight);
        reusedRect = new(areaRect.x + 10f, areaRect.y + 10f, 21f, 26f);
        GUI.DrawTexture(reusedRect, middleMemberIcon);

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 10f, 135f, 24f);
        if (SelBranch.IsValid())
        {
            Widgets.Label(reusedRect, "OARO_SquadWin_MemberCountInfo".Translate(SelBranch.Squad.AllCrewCountInt, SelSquadInfo.CrewCeiling));
            reusedRect.yMin = reusedRect.yMax;
            reusedRect.yMax += 24f;
            Widgets.Label(reusedRect, "OARO_PeoplePreDay".Translate(SelSquadInfo.MemberRecoveryRate.ToStringWithSign("F1"))
                                                         .Colorize(SelSquadInfo.MemberRecoveryRate < 0 ? ColorLibrary.RedReadable : Color.green));

            TooltipHandler.TipRegion(reusedRect, () => SelSquadInfo.MemberRecoveryRateExplanation.Value, uniqueId: 5414190);
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_SquadWin_MemberCountInfoN".Translate());
            reusedRect.yMin = reusedRect.yMax;
            reusedRect.yMax += 24f;
            Widgets.Label(reusedRect, "OARO_PeoplePreDayN".Translate());
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(areaRect.x, areaRect.yMax - 39f, 186f, 39f);

        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: string.Empty,
            acceptance: InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_MapRecommendationToKnight, fallback: false),
            baseTex: middleCombatReadinessButton,
            downTex: middleCombatReadinessButton_Down,
            doMouseoverSound: true))
        {
            BranchInteractionParms parms = new(SelBranch, Map);
            AcceptanceReport acceptanceReport = BranchInteractionDefOf.OARO_MapRecommendationToKnight.Worker.CanUseInteraction(parms, resultOnly: false);
            if (acceptanceReport)
            {
                BranchInteractionDefOf.OARO_MapRecommendationToKnight.Worker.TryApplyInteraction(parms);
            }
            else
            {
                InteractionAcceptances.MarkDirty();
                Messages.Message(
                    text: "OARO_CanApplyBranchInteractionWithReason".Translate(
                        parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName),
                        BranchInteractionDefOf.OARO_MapRecommendationToKnight.Named("INTERACTION"),
                        acceptanceReport.Reason.Named("Reason")),
                    def: MessageTypeDefOf.RejectInput,
                    historical: false);
            }
        }
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, areaRect.x + 50f, 26f, 23f);
        GUI.DrawTexture(reusedRect, IconLibrary.RecommendationIcon);
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 6f, 64f, 24f);
        Widgets.Label(reusedRect, BranchInteractionDefOf.OARO_MapRecommendationToKnight.label);

        //中部右下区域
        areaRect = new(areaRect.x, middleInnerRect.yMax - middleBottomHeight, 186f, middleBottomHeight);
        reusedRect = new(areaRect.x + 4f, areaRect.y + 8f, 70f, 24f);
        Widgets.Label(reusedRect, "OARO_SquadWin_IncludeCommander".Translate());

        reusedRect = OARO_WindowUtility.CenterRectOnY(areaRect, reusedRect.xMax + 10f, 29f, 27f);
        GUI.DrawTexture(reusedRect, middleCommanderIcon);

        reusedRect = OARO_WindowUtility.CenterRectOnY(areaRect, areaRect.xMax - 54f, 50f, 24f);
        if (SelBranch.IsValid())
        {
            Widgets.Label(reusedRect, "OARO_FilledTotalFormatPeople".Translate(SelBranch.Squad.CommanderCountInt, SelSquadInfo.CommanderCeiling));
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_FilledTotalFormatPeopleN".Translate());
        }

        //下部区域
        Rect bottomRect = new(inRectX, middleRect.yMax + 32f, rectWidth, rectHeight);
        bool hasSupportAuthority = SelBranch?.HasSupportAuthority ?? false;

        //下部左侧区域
        areaRect = new(bottomRect.x, bottomRect.y, 200f, rectHeight);
        GUI.DrawTexture(areaRect, middleBottomBackground, ScaleMode.ScaleToFit);
        areaRect = areaRect.ContractedBy(2f);

        reusedRect = new(areaRect.x + 18f, areaRect.y + 14f, 60f, 65f);
        Color stateColor = Color.white;
        if (SelBranch.IsValid())
        {
            if (SelBranch.IsIdleNow)
            {
                GUI.DrawTexture(reusedRect, IconLibrary.BigIdleIcon, ScaleMode.ScaleToFit);
                stateColor = Color.cyan;
            }
            else if (SelBranch.IsOutdoorNow)
            {
                GUI.DrawTexture(reusedRect, IconLibrary.BigOutdoorIcon, ScaleMode.ScaleToFit);
                stateColor = ColorLibrary.Orange;
            }
            else
            {
                GUI.DrawTexture(reusedRect, IconLibrary.BigIndoorIcon, ScaleMode.ScaleToFit);
                stateColor = Color.yellow;
            }
        }

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax + 4f, 95f, 20f);
        Widgets.Label(reusedRect, "OARO_BranchWorkState".Translate());
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax + 16f, 95f, 20f);
        if (SelBranch.IsValid())
        {
            Widgets.Label(reusedRect, SelBranch.CurWorkState.Colorize(stateColor));
        }
        else
        {
            Widgets.Label(reusedRect, "----".Colorize(stateColor));
        }

        reusedRect = new(areaRect.x + 97f, areaRect.y + 14f, 98f, 20f);
        Widgets.Label(reusedRect, "OARO_AutoStartTaskChance".Translate());

        reusedRect = new(areaRect.x + 97f, reusedRect.yMax + 10f, 98f, 20f);
        if (SelBranch?.IsIdleNow ?? false)
        {
            Widgets.Label(reusedRect, SelBranch.TaskHandler.AutoStartTaskChance.ToStringPercent("F0"));
        }
        else
        {
            Widgets.Label(reusedRect, "--%");
        }

        reusedRect = new(areaRect.x + 97f, areaRect.y + 85f, 98f, 20f);
        Widgets.Label(reusedRect, BranchInteractionDefOf.OARO_RequestCombatReadiness.label);

        reusedRect = new(areaRect.x + 97f, areaRect.yMax - 37f, 98f, 37f);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: string.Empty,
            acceptance: InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_RequestCombatReadiness, fallback: false),
            baseTex: middleCombatReadinessButton,
            downTex: middleCombatReadinessButton_Down,
            doMouseoverSound: true))
        {
            BranchInteractionParms parms = new(SelBranch, Map);
            AcceptanceReport acceptanceReport = BranchInteractionDefOf.OARO_RequestCombatReadiness.Worker.CanUseInteraction(parms, resultOnly: false);
            if (acceptanceReport)
            {
                BranchInteractionDefOf.OARO_RequestCombatReadiness.Worker.TryApplyInteraction(parms);
            }
            else
            {
                InteractionAcceptances.MarkDirty();
                Messages.Message(
                    text: "OARO_CanApplyBranchInteractionWithReason".Translate(
                        parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName),
                        BranchInteractionDefOf.OARO_RequestCombatReadiness.Named("INTERACTION"),
                        acceptanceReport.Reason.Named("Reason")),
                    def: MessageTypeDefOf.RejectInput,
                    historical: false);
            }
        }

        reusedRect = OARO_WindowUtility.CenterRect(reusedRect, 29f, 29f);
        GUI.DrawTexture(reusedRect, middleCombatReadinessButton_Icon);

        areaRect = new(areaRect.xMax + 2f, areaRect.y, 380f, areaRect.height);
        Rect supportOptRect = areaRect;
        supportOptRect.yMax = areaRect.yMin + 40f;
        Text.Font = GameFont.Medium;
        Widgets.Label(supportOptRect, "OARO_SquadWin_SupportOption".Translate());
        Text.Font = GameFont.Small;

        reusedRect = new(areaRect.x, supportOptRect.yMax, areaRect.width, 38f);
        GUI.DrawTexture(reusedRect, middleBottom_Light, ScaleMode.ScaleToFit);

        reusedRect.xMax = reusedRect.xMin + 100f;
        Widgets.Label(reusedRect, "OARO_SquadWin_SupportAuthority".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = areaRect.xMax;
        if (hasSupportAuthority)
        {
            Widgets.Label(reusedRect, "OARO_Unlocked".Translate());
            reusedRect = OARO_WindowUtility.CenterRect(reusedRect, 227f, 11f);
            GUI.DrawTexture(reusedRect, middleSupportLockLine);
        }

        reusedRect = new(areaRect.x, areaRect.y + 78f, 100f, 38f);
        Widgets.Label(reusedRect, "OARO_SquadWin_UsableBombardCount".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = areaRect.xMax;
        DrawBombardCount(reusedRect);

        reusedRect = new(areaRect.x, reusedRect.yMax, areaRect.width, 38f);
        GUI.DrawTexture(reusedRect, middleBottom_Light, ScaleMode.ScaleToFit);

        reusedRect.xMax = reusedRect.xMin + 100f;
        Widgets.Label(reusedRect, "OARO_SquadWin_RequestSupport".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax += 140f;

        reusedRect = OARO_WindowUtility.CenterRect(reusedRect, 95f, 36f);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_SquadWin_BombardSupport".Translate(),
            baseTex: middleSupportButton,
            downTex: middleSupportButton_Down,
            acceptance: BombardAcceptance.Value,
            doMouseoverSound: true))
        {
            AcceptanceReport acceptance = BranchSupportUtility.CanBombard(SelBranch, Map, resultOnly: false);
            if (acceptance)
            {
                BranchSupportUtility.DoBombard(SelBranch, Map);
            }
            else
            {
                Messages.Message("OARO_CanNotDoBombardWithReason".Translate(SelBranch.Name.Named(KeyLibrary_FormatArgName.BranchName), acceptance.Reason.Named("Reason")), MessageTypeDefOf.RejectInput, historical: false);
            }
            RefreshBranchAcceptance();
        }

        reusedRect.xMax = areaRect.xMax;
        reusedRect.xMin = areaRect.xMax - 140f;
        reusedRect = OARO_WindowUtility.CenterRect(reusedRect, 95f, 36f);

        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_SquadWin_MilitarySupport".Translate(),
            baseTex: middleSupportButton,
            downTex: middleSupportButton_Down,
            acceptance: SupportAcceptance.Value,
            doMouseoverSound: true))
        {
            BranchSupportUtility.CombatKnightSupportFloatMenu(SelBranch, Map);
        }

        if (!hasSupportAuthority)
        {
            reusedRect = Rect.MinMaxRect(areaRect.xMin - 103f, supportOptRect.yMax, areaRect.xMax + 4f, areaRect.yMax + 8f);
            GUI.DrawTexture(reusedRect, middleLockShade, ScaleMode.StretchToFill, alphaBlend: true);

            reusedRect = new(areaRect.x, supportOptRect.yMax, areaRect.width, 38f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                reusedRect,
                string.Empty,
                InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_UnlockSupportAuthority),
                middleUnlockButton,
                middleUnlockButton_Down,
                doMouseoverSound: true))
            {
                BranchInteractionParms parms = new(SelBranch, Map);
                AcceptanceReport acceptanceReport = BranchInteractionDefOf.OARO_UnlockSupportAuthority.Worker.CanUseInteraction(parms, resultOnly: false);
                if (acceptanceReport)
                {
                    BranchInteractionDefOf.OARO_UnlockSupportAuthority.Worker.TryApplyInteraction(parms);
                }
                else
                {
                    InteractionAcceptances.MarkDirty();
                    Messages.Message(
                        text: "OARO_CanApplyBranchInteractionWithReason".Translate(
                            parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName),
                            BranchInteractionDefOf.OARO_UnlockSupportAuthority.Named("INTERACTION"),
                            acceptanceReport.Reason.Named("Reason")),
                        def: MessageTypeDefOf.RejectInput,
                        historical: false);
                }
            }

            reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, areaRect.x + 148f, 26f, 24f);
            GUI.DrawTexture(reusedRect, IconLibrary.RecommendationIcon);
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(reusedRect.xMax + 8f, reusedRect.y, 80f, 24f);
            Widgets.Label(reusedRect, "OARO_SquadWin_RequestUnlockSupport".Translate());
        }

        return false;
    }

    private void DrawMedalRect(Rect inRect)
    {
        if (!SelBranch.IsValid())
        {
            return;
        }

        BranchMedalHandler medalHandler = SelBranch.MedalHandler;
        if (medalHandler.PrimaryMedal is not null)
        {
            GUI.DrawTexture(inRect, medalHandler.PrimaryMedal.BackgroundTexture);
        }

        //上侧勋章柱框
        Rect reusedRect = OARO_WindowUtility.CenterRect(inRect, 300f, 112f);
        GUI.DrawTexture(reusedRect, middleUpPeristele);

        //分部勋章
        Rect medalOutRect = OARO_WindowUtility.CenterRect(inRect, 192f, 140f);
        float entryX = medalOutRect.x;
        float entryY = medalOutRect.y;
        float entryWidth = 80f;
        float entryHeight = 70f;
        float entryXInterval = 32f;
        int column = 0;

        Rect entryRect;

        Rect medalViewRect = medalOutRect;
        List<BranchMedalDef> allMedalDefs = DefDatabase<BranchMedalDef>.AllDefsListForReading;
        medalViewRect.height = Mathf.Ceil(allMedalDefs.Count / 2f) * entryHeight;

        Widgets.BeginScrollView(medalOutRect, ref scrollPosition_Medals, medalViewRect, showScrollbars: false);
        for (int i = 0; i < allMedalDefs.Count; i++)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            column++;
            if (column >= 2)
            {
                entryX = medalOutRect.x;
                entryY += entryHeight;
                column = 0;
            }
            else
            {
                entryX += (entryWidth + entryXInterval);
            }

            if (medalHandler.HasMedal(allMedalDefs[i]))
            {
                GUI.DrawTexture(entryRect, allMedalDefs[i].iconTexture.ExpandedTexture, ScaleMode.ScaleToFit);
            }
        }
        Widgets.EndScrollView();
    }

    private void DrawBombardCount(Rect inRect)
    {
        Rect areaRect = inRect;
        areaRect.xMin = areaRect.xMax - 80f;
        int bombardSupportCeiling = BombardSupportCeiling.Value;
        if (SelBranch.IsValid())
        {
            Widgets.Label(areaRect, $"× {bombardSupportCeiling}");
            TooltipHandler.TipRegion(areaRect, () => BombardSupportCeilingExplanation.Value, uniqueId: 9393957);
        }
        else
        {
            Widgets.Label(areaRect, "× --");
        }
        areaRect = inRect;
        areaRect.xMin += 8f;
        areaRect.xMax -= 80f;

        Rect iconRect = OARO_WindowUtility.CenterRect(areaRect, 28f * 6, 28f);

        float shellX = iconRect.x;
        float shellY = iconRect.y;
        Rect shellRect;
        int bombardDrawLeft = Mathf.Min(8, bombardSupportCeiling);
        while (bombardDrawLeft >= 2)
        {
            shellRect = new(shellX, shellY, 28f, 28f);
            GUI.DrawTexture(shellRect, middleShellIcon);
            bombardDrawLeft -= 2;
            shellX += 28f;

        }
        if (bombardDrawLeft > 0)
        {
            shellRect = new(shellX, shellY, 28f, 28f);
            GUI.DrawTexture(shellRect, middleShellIcon_Half);
        }
    }

    private void DrawLeftRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftBackground);
        Rect tabRect = new(inRect.x, inRect.y - 32f, inRect.width, 32f);

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
        TabDrawer.DrawTabs(tabRect, Tabs, maxTabWidth: 140f);

        inRect = inRect.ContractedBy(2f);

        float viewHeight = TabSummaryCaches.Count * 91f;
        Rect listRect = inRect;
        listRect.width = 393f;
        listRect.height = viewHeight;

        Rect scrollRect = Rect.MinMaxRect(listRect.xMax, inRect.yMin, inRect.xMax, inRect.yMax);
        GUI.DrawTexture(scrollRect, leftScroll, ScaleMode.ScaleToFit);

        Widgets.BeginScrollView(inRect, ref scrollPosition_Squads, listRect);
        float entryX = listRect.x;
        float entryY = listRect.y;
        int squadCount = TabSummaryCaches.Count;
        int usedCount = Mathf.Max(7, squadCount);
        Rect entryRect;

        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < squadCount; i++)
        {
            entryRect = new(entryX, entryY, 393f, 91f);
            entryY += 89f;

            DrawSquadEntry(entryRect, TabSummaryCaches[i], i);
        }

        if (usedCount > squadCount)
        {
            for (int i = squadCount; i < usedCount; i++)
            {
                entryRect = new(entryX, entryY, 393f, 91f);
                entryY += 89f;

                GUI.DrawTexture(entryRect, IconLibrary.BranchSummaryBackground);
            }
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndScrollView();
    }

    private void DrawRightRect(Rect inRect)
    {
        float inRectX = inRect.x;
        float inRectY = inRect.y;
        Rect reusedRect;

        //名称|内容分割线
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY, 347f, 3f);
        GUI.DrawTexture(reusedRect, rightCuttingLine);

        reusedRect = new(inRectX + 30f, reusedRect.yMax + 10f, 35f, 24f);
        GUI.DrawTexture(reusedRect, rightSupportSquadIcon);


        reusedRect.xMin = reusedRect.xMax + 2f;
        reusedRect.xMax = inRect.xMin + 180f;
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(reusedRect, "OARO_SquadWin_SupportSquadNum".Translate() + $" /");

        reusedRect = new(inRect.xMax - 64f, reusedRect.y, 58f, 24f);
        Text.Anchor = TextAnchor.LowerRight;
        OARO_WindowUtility.DrawRecommendationInfo(reusedRect, MapRecommendationCount.Value);

        Text.Anchor = TextAnchor.MiddleCenter;

        Rect mainRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 45f, 352f, 597f);
        GUI.DrawTexture(mainRect, rightBackground);
        mainRect = mainRect.ContractedBy(2f);


    }

    private void DrawSquadEntry(Rect inRect, BranchSummaryUICache entry, int index)
    {
        Rect summaryRect = inRect.ContractedBy(2f);
        if (Widgets.ButtonInvisible(summaryRect))
        {
            if (SelBranchIndex == index)
            {
                DeselectSquad();
            }
            else
            {
                SelectSquad(index);
            }
        }

        OARO_WindowUtility.DrawBranchSummary(new(inRect.x, inRect.y), entry);
        if (Mouse.IsOver(inRect))
        {
            Widgets.DrawHighlight(inRect);
        }
        if (SelBranchIndex == index)
        {
            Widgets.DrawHighlightSelected(inRect);
        }
    }

    private Dictionary<BranchInteractionDef, AcceptanceReport> RefreshInteractionAcceptances()
    {
        Branch selBranch = SelBranch;
        Dictionary<BranchInteractionDef, AcceptanceReport> pairs = [];
        foreach (BranchInteractionDef def in SquadNeedBranchInteractions)
        {
            AcceptanceReport acceptance = false;
            try
            {
                if (selBranch.IsValid())
                {
                    BranchInteractionParms parms = new(selBranch, Map);
                    acceptance = def.Worker.CanUseInteraction(parms, resultOnly: false);
                }

            }
            catch (Exception ex)
            {
                acceptance = false;
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"get {nameof(AcceptanceReport)} of {nameof(BranchInteractionDef)}",
                    typeName: nameof(RefreshInteractionAcceptances),
                    methodName: nameof(Window_BranchSquad),
                    needStackTrace: true);
            }

            pairs.Add(def, acceptance);
        }
        return pairs;
    }

    private void RecacheBranchSummary()
    {
        DeselectSquad();
        MapRecommendationCount.Reset();
        InteractionAcceptances.Reset();

        BranchSummaryCaches.Clear();
        foreach (Branch branch in RatkinOrder.BranchManager.AllBranches)
        {
            try
            {
                BranchSummaryUICache summaryUICache = new(branch, Map);
                BranchSummaryCaches.Add(new BranchSummaryUICache(branch, Map));
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: "recache branch summary",
                    typeName: nameof(Window_BranchSquad),
                    methodName: nameof(RecacheBranchSummary),
                    needStackTrace: true);
            }
        }
        if (BranchSummaryCaches.Count > 0)
        {
            BranchSummaryCaches.Sort(new BranchSummaryUICache.UIEntryComparer());
            GetCurTapBranchSummary();
        }
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
        DeselectSquad();
        TabSummaryCaches.Clear();
        if (BranchSummaryCaches.Count > 0)
        {
            switch (CurTab)
            {
                case TabType.All:
                    {
                        TabSummaryCaches.AddRange(BranchSummaryCaches);
                        break;
                    }
                case TabType.Near:
                    {
                        for (int i = 0; i < BranchSummaryCaches.Count; i++)
                        {
                            if (BranchSummaryCaches[i].IsInAffectedRange)
                            {
                                TabSummaryCaches.Add(BranchSummaryCaches[i]);
                            }
                        }
                        break;
                    }
                case TabType.Friendly:
                    {
                        for (int i = 0; i < BranchSummaryCaches.Count; i++)
                        {
                            if (BranchSummaryCaches[i].Branch.IsBranchOfType(BranchType.Friendly))
                            {
                                TabSummaryCaches.Add(BranchSummaryCaches[i]);
                            }
                        }
                        break;
                    }
            }

            if (TabSummaryCaches.Count > 0)
            {
                SelectSquad(0);
            }
        }
    }

    private void SelectSquad(int index)
    {
        if (index == SelBranchIndex)
        {
            return;
        }
        try
        {
            Branch branch = TabSummaryCaches[index].Branch;
            if (!branch.IsValid())
            {
                DeselectSquad();
                return;
            }
            SelBranchIndex = index;
            SelSquadInfo = new(branch, Map);

            RefreshBranchStatCache();
            RefreshBranchAcceptance();

            SelBranch.PostApplyBranchInteraction -= PostApplyBranchInteraction;
            SelBranch.PostApplyBranchInteraction += PostApplyBranchInteraction;

            return;
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "select squad",
                typeName: nameof(Window_BranchSquad),
                methodName: nameof(SelectSquad),
                needStackTrace: true);
            DeselectSquad();
            return;
        }
    }

    private void DeselectSquad()
    {
        if (SelBranch.IsValid())
        {
            SelBranch.PostApplyBranchInteraction -= PostApplyBranchInteraction;
        }

        RefreshBranchStatCache();
        RefreshBranchAcceptance();

        SelSquadInfo?.ClearCache();
        SelBranchIndex = -1;
        SelSquadInfo = new();
    }

    private void RefreshBranchStatCache()
    {
        SupplyRecoveryRateExplanation.Reset();
        BombardSupportCeiling.Reset();
        BombardSupportCeilingExplanation.Reset();
    }

    private void RefreshBranchAcceptance()
    {
        SupportAcceptance.Reset();
        BombardAcceptance.Reset();
        InteractionAcceptances.Reset();
    }

    private void PostApplyBranchInteraction(BranchInteractionDef def, BranchInteractionParms parms, bool succeeded)
    {
        RefreshBranchAcceptance();
        MapRecommendationCount.MarkDirty();
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MainBackground");

    private static readonly Texture2D topTitleground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_TopTitleground");
    private static readonly Texture2D topRibbon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_TopRibbon");

    private static readonly Texture2D middleCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCuttingLine");
    private static readonly Texture2D middleCheckButton = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCheckButton");
    private static readonly Texture2D middleCheckButton_Down = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCheckButton_Down");

    private static readonly Texture2D middleUpRibbon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleUpRibbon");
    private static readonly Texture2D middleUpBackground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleUpBackground");
    private static readonly Texture2D middleUpPeristele = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleUpPeristele");

    private static readonly Texture2D middleMiddleBackground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleMiddleBackground");
    private static readonly Texture2D middleClickToAddButton = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleClickToAddButton");
    private static readonly Texture2D middleClickToAddButton_Down = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleClickToAddButton_Down");
    private static readonly Texture2D middleMemberIcon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleMemberIcon");
    private static readonly Texture2D middleCommanderIcon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCommanderIcon");

    private static readonly Texture2D middleBottomBackground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleBottomBackground");
    private static readonly Texture2D middleBottom_Light = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleBottom_Light");
    private static readonly Texture2D middleCombatReadinessButton = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCombatReadinessButton");
    private static readonly Texture2D middleCombatReadinessButton_Down = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCombatReadinessButton_Down");
    private static readonly Texture2D middleCombatReadinessButton_Icon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleCombatReadinessButton_Icon");
    private static readonly Texture2D middleSupportLockLine = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleSupportLockLine");
    private static readonly Texture2D middleShellIcon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleShellIcon");
    private static readonly Texture2D middleShellIcon_Half = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleShellIcon_Half");
    private static readonly Texture2D middleSupportButton = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleSupportButton");
    private static readonly Texture2D middleSupportButton_Down = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleSupportButton_Down");
    private static readonly Texture2D middleLockShade = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleLockShade");
    private static readonly Texture2D middleUnlockButton = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleUnlockButton");
    private static readonly Texture2D middleUnlockButton_Down = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_MiddleUnlockButton_Down");

    private static readonly Texture2D leftBackground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_LeftBackground");
    private static readonly Texture2D leftScroll = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_LeftScroll");

    private static readonly Texture2D rightBackground = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_RightBackground");
    private static readonly Texture2D rightCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_RightCuttingLine");
    private static readonly Texture2D rightSupportSquadIcon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_RightSupportSquadIcon");

    private static readonly Texture2D branchSupplyLack = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_BranchSupply_Lack");
    private static readonly Texture2D branchSupplyJust = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_BranchSupply_Just");
    private static readonly Texture2D branchSupplyEnough = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_BranchSupply_Enough");

    private static readonly Texture2D branchBaseSiteIcon = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_BranchBaseIcon");
    private static readonly Texture2D verticalCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchSquad/OARO_VerticalCuttingLine");
}