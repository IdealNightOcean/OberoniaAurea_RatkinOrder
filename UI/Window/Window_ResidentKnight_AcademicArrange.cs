using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Window_ResidentKnight_AcademicArrange : OrderWindowBase
{
    private enum TabType
    {
        PawnInfo,
        AcademicProgress,
        VirtueInfo,
    }

    public override Vector2 InitialSize => new(1736f, 926f);

    public Action PostArrangeNewAcademic { get; set; }

    private ResidentKnight Knight { get; }
    public AcademicHandler AcademicHandler { get; }
    private int NoAdditionalCostAcademicCeiling { get; }

    private AcceptanceReport CheckAcademicStageAcceptance { get; set; }

    /*
     * 新字段
     */
    private List<UIData_KnightAcademic> AvailableAcademics { get; } = [];
    private List<UIData_KnightVirtue> AvailableVirtues { get; } = [];

    private UIDataDrawer_SelectableList<UIData_KnightAcademic, UIDataDrawer_KnightAcademic> AcademicListDrawer { get; }
    private UIDataDrawer_KnightAcademicProgress AcademicProgressDrawer { get; }
    private UIDataDrawer_SelectableList<UIData_KnightVirtue, UIDataDrawer_KnightVirtueProgressBar> VirtueProgressListDrawer { get; }

    private UIDrawer_KnightVirtueTable VirtueTableDrawer { get; }
    private UIData_KnightAcademicWithStage SelAcademicData { get; set; }

    private TabType CurTab { get; set; } = TabType.PawnInfo;
    private List<TabRecord> Tabs { get; } = new(3);

    public Window_ResidentKnight_AcademicArrange(ResidentKnight record) : base()
    {
        Knight = record ?? throw new ArgumentNullException(nameof(record));
        AcademicHandler = record.AcademicHandler ?? throw new ArgumentNullException(nameof(AcademicHandler));

        NoAdditionalCostAcademicCeiling = AcademicUtility.GetNoAdditionalCostAcademicCeiling(Knight.CurRank);

        AvailableAcademics.Capacity = AcademicHandler.Academics.Count;
        HashSet<KnightAcademicDef> academicHash = new(AcademicHandler.Academics.Count);

        foreach (KnightAcademicDef academicDef in AcademicUtility.GetAllActivateAcademicsBySelf(Knight))
        {
            academicHash.Add(academicDef);
            AvailableAcademics.Add(new UIData_KnightAcademic(this.Knight, academicDef));
        }

        foreach (KnightAcademicDef academicDef in AcademicHandler.Academics.Keys)
        {
            if (academicHash.Add(academicDef))
            {
                AvailableAcademics.Add(new UIData_KnightAcademic(this.Knight, academicDef));
            }
        }

        AvailableVirtues.Capacity = record.VirtueHandler.Virtues.Count;
        foreach (KnightVirtue virtue in record.VirtueHandler.Virtues)
        {
            AvailableVirtues.Add(new UIData_KnightVirtue(this.Knight, virtue));
        }

        UIDataDrawer_KnightAcademic academicEntryDrawer = new();
        academicEntryDrawer.SetDrawSize(new(260f, 94f));
        AcademicListDrawer = new(academicEntryDrawer, AvailableAcademics)
        {
            RowLimit = 5,
            ColumnLimit = 1,
            HorizontalScroll = false,
            LayoutStrategy = ScrollLayoutStrategy.ViewDerivedByRowCol
        };
        AcademicListDrawer.SetDrawSize(new(280f, 560f));

        UIDataDrawer_KnightVirtueProgressBar virtueProgressDrawer = new();
        VirtueProgressListDrawer = new(virtueProgressDrawer, AvailableVirtues)
        {
            RowLimit = 4,
            ColumnLimit = 1,
            HorizontalScroll = false,
            LayoutStrategy = ScrollLayoutStrategy.ViewDerivedByRowCol
        };

        VirtueTableDrawer = new();
        VirtueTableDrawer.SetKnight(Knight);

        Tabs =
            [
                new TabRecord(label: "OARO_BranchSquad_All".Translate().CapitalizeFirst(),
                              clickedAction: () => CurTab = TabType.PawnInfo,
                              selected: () => CurTab == TabType.PawnInfo),
                new TabRecord(label: "OARO_BranchSquad_All".Translate().CapitalizeFirst(),
                              clickedAction: () => CurTab = TabType.AcademicProgress,
                              selected: () => CurTab == TabType.AcademicProgress),
                new TabRecord(label: "OARO_BranchSquad_All".Translate().CapitalizeFirst(),
                              clickedAction: () => CurTab = TabType.VirtueInfo,
                              selected: () => CurTab == TabType.VirtueInfo),
            ];

        AcademicProgressDrawer = new();
    }

    public override void PreOpen()
    {
        base.PreOpen();
        AcademicListDrawer.OnSelectedItem.Register(SwitchAcademic);
        AcademicListDrawer.SelectItem(0);
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        PostArrangeNewAcademic = null;
    }

    public override void DoWindowContents(Rect inRect)
    {
        //Rect mainRect = OARO_UIUtility.CenterRect(inRect, 1308f, 695f);
        Rect mainRect = inRect;
        GUI.DrawTexture(mainRect, mainBackground, ScaleMode.StretchToFill);

        Rect mainInnerRect = GenUI.ContractedBy(inRect, 3f); // (1730f,920f)

        if (OARO_UIUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }

        Rect pawnRect = new(mainInnerRect.xMin, mainInnerRect.yMin, 198f, 272f);
        DrawPawnSummary(pawnRect);

        Rect academicRect = new(mainInnerRect.xMin, mainInnerRect.yMax - 560f, 280f, 560f);
        AcademicListDrawer.Draw(academicRect.position);

        Rect rightRect = mainInnerRect;
        rightRect.xMin = academicRect.xMax;

        Rect tabInfoRect = new(0f, 0f, mainInnerRect.width * 0.8f, mainInnerRect.height * 0.8f);
        tabInfoRect = tabInfoRect.CenteredIn(rightRect);

        Rect tabRect = new(tabInfoRect.xMin, tabInfoRect.yMin - 32f, tabInfoRect.width, 32f);
        TabDrawer.DrawTabs(tabRect, Tabs, maxTabWidth: 140f);

        switch (CurTab)
        {
            case TabType.PawnInfo:
                {
                    DrawPawnInfo(tabInfoRect);
                    break;
                }
            case TabType.AcademicProgress:
                {
                    AcademicProgressDrawer.SetDrawSizeAspectFit(tabInfoRect.size);
                    AcademicProgressDrawer.Draw(tabInfoRect.position);
                    break;
                }
            case TabType.VirtueInfo:
                {
                    DrawVirtueInfo(tabInfoRect);
                    break;
                }
            default: break;
        }

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawPawnSummary(Rect inRect)
    {
        Rect innerRect = GenUI.ContractedBy(inRect, 2f);
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;
        float innerWidth = innerRect.width;
        Rect reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 20f, 75f, 75f);
        GUI.DrawTexture(reusedRect, PortraitsCache.Get(Knight.Pawn, reusedRect.size, Rot4.South));

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, reusedRect.yMax + 8f, innerWidth, 20f);
        Widgets.Label(reusedRect, Knight.Pawn.NameShortColored);

        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 125f, 114f, 41f);
        DrawRankBackGround(reusedRect);
        Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{Knight.CurRank}Knight".Translate());

        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 190f, innerWidth, 24f);
        Widgets.Label(reusedRect, "OARO_MeditationPoints".Translate(Knight.MeditationPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count)));
        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, reusedRect.yMax, innerWidth, 24f);
        Widgets.Label(reusedRect, "OARO_NoAdditionalCostAcademicCeilingInfo".Translate(AcademicHandler.TotalAcademicLevel.Value, NoAdditionalCostAcademicCeiling));

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawPawnInfo(Rect inRect)
    {
        Rect tableRect = inRect.RightPart(0.65f);
        tableRect = tableRect.CenterSegmentOnY(0.75f);
        VirtueProgressListDrawer.SetDrawSize(tableRect.size);
        VirtueProgressListDrawer.Draw(tableRect.position);
    }

    private void DrawAcademicInfo(Rect inRect)
    {
        AcademicProgressDrawer.Draw(inRect.position);

        /*
        Rect innerRect = GenUI.ContractedBy(inRect, 2f);
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(innerX + 24f, innerY, innerRect.width, 32f);
        Widgets.Label(reusedRect, SelAcademicDef.LabelCap.Colorize(CheckAcademicColor));

        reusedRect = OARO_UIUtility.CenterRectOnX(innerRect, innerY + 180f, 968f, 198f);
        GUI.DrawTexture(reusedRect, stageBackground);

        Rect stageOutRect = GenUI.ContractedBy(reusedRect, 2f);
        Rect stageViewRect = stageOutRect;
        stageViewRect.height -= 16f;

        float entryX = stageViewRect.xMin;
        float entryY = stageViewRect.yMin;
        float entryWidth = 260f;
        float entryHeight = stageViewRect.height;

        stageViewRect.width = entryWidth * SelAcademicDef.academicStages.Count;

        Widgets.BeginScrollView(stageOutRect, ref scrollPosition_AcademicStage, stageViewRect);
        for (int i = 0; i < SelAcademicDef.academicStages.Count; i++)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryX += entryWidth;
            // DrawAcademicStage(entryRect, i);
        }
        Widgets.EndScrollView();

        if (CheckAcademicStage is null)
        {
            OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
            return;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(innerX + 220f, innerY + 430f, 240f, 64f);
        Widgets.Label(reusedRect, $"{SelAcademicDef.LabelCap} - {CheckAcademicStage.label.CapitalizeFirst()}");

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperCenter;
        reusedRect = new(innerX + 220f, reusedRect.yMax + 24f, 240f, 152f);
        Widgets.LabelScrollable(reusedRect, CheckAcademicStage.description, ref scrollPosition_AcademicDesc);


        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        if (CheckAcademicStageLevel == SelAcademicStageLevel + 1)
        {
            reusedRect = new(innerX + 660f, innerY + 430f, 160f, 64f);
            Widgets.Label(
                rect: reusedRect,
                label: "OARO_MeditationPointsSuffix".Translate(MeditationPointForSelAcademicUpgrade.ToString("F0").Named(KeyLibrary_FormatArgName.Count))
                                                    .Colorize(NoAdditionalCostAcademicCeiling < AcademicHandler.TotalAcademicLevel.Value ? ColorLibrary.RedReadable : Color.white));

            reusedRect = OARO_UIUtility.CenterRectOnX(reusedRect, reusedRect.yMax + 32f, 196f, 54f);
            if (OARO_UIUtility.TextButtonImageDisableable(
                 butRect: reusedRect,
                 label: "OARO_Unlock".Translate(),
                 acceptance: CheckAcademicStageAcceptance,
                 baseTex: unlockButton,
                 downTex: unlockButton_Down,
                 doMouseoverSound: true))
            {
                AcceptanceReport acceptance = AcademicHandler.CanUpgradeAcademic(SelAcademicDef, directly: false, resultOnly: false);
                if (acceptance)
                {
                    AcademicHandler.UpgradeAcademic(SelAcademicDef);
                    RefreshSelStageLevel();
                    PostArrangeNewAcademic?.Invoke();
                }
                else
                {
                    Messages.Message(
                        text: "OARO_CanUpgradeAcademicLevelWithReason".Translate(acceptance.Reason.Named(KeyLibrary_FormatArgName.Reason)),
                        def: MessageTypeDefOf.RejectInput,
                        historical: false);
                }
            }

            if (DebugSettings.godMode)
            {
                Text.Font = GameFont.Tiny;
                reusedRect = new(reusedRect.xMax + 20f, reusedRect.yMax - 20f, 40f, 20f);
                if (Widgets.ButtonText(reusedRect, "Dev"))
                {
                    AcademicHandler.UpgradeAcademic(SelAcademicDef, directly: true);
                    PostArrangeNewAcademic?.Invoke();
                    RefreshSelStageLevel();
                }
            }
        }
        else
        {
            reusedRect = new(innerX + 660f, innerY + 480f, 160f, 64f);
            GUI.DrawTexture(reusedRect, textLace, ScaleMode.ScaleToFit);
            TaggedString stageUnlockLabel;
            if (CheckAcademicStageLevel <= SelAcademicStageLevel)
            {
                stageUnlockLabel = SelAcademicStageLevel == SelAcademicDef.MaxStageLevel
                    ? "OARO_AlreadyAtMaxAcademicLevel".Translate()
                    : "OARO_Unlocked".Translate();
            }
            else
            {
                stageUnlockLabel = "OARO_NeedPreAcademicLevel".Translate();
            }
            Widgets.Label(reusedRect, stageUnlockLabel);
        }

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
        */
    }

    private void DrawVirtueInfo(Rect inRect)
    {
        Rect tableRect = inRect.RightPart(0.65f);
        tableRect = tableRect.CenterSegmentOnY(0.75f);
        VirtueTableDrawer.SetDrawSize(tableRect.size);
        VirtueTableDrawer.Draw(tableRect.position);
    }

    private void SwitchAcademic(int index, bool selectedIndexChanged)
    {
        if (!selectedIndexChanged)
            return;

        if (index == -1)
        {
            ClearSelection();
        }
        else
        {
            UIData_KnightAcademic selAcademicBaseData = AcademicListDrawer.SelectedItem;
            selAcademicBaseData?.Refresh();
            if (selAcademicBaseData is null || !selAcademicBaseData.IsDataValid)
            {
                ClearSelection();
            }
            else
            {
                SelAcademicData = new(selAcademicBaseData.Knight, selAcademicBaseData.Academic);
            }
        }

        AcademicProgressDrawer.SetDrawData(SelAcademicData);
    }

    private void ClearSelection()
    {
        SelAcademicData = UIData_KnightAcademicWithStage.EmptyData;
        VirtueProgressListDrawer.SetDrawDatas([]);
    }

    private void DrawRankBackGround(Rect inRect)
    {
        switch (Knight.CurRank)
        {
            case ResidentKnightRank.Regular:
                {
                    GUI.DrawTexture(inRect, rankBackground_Regular, ScaleMode.StretchToFill);
                    return;
                }
            case ResidentKnightRank.Elite:
                {
                    GUI.DrawTexture(inRect, rankBackground_Elite, ScaleMode.StretchToFill);
                    return;
                }
            case ResidentKnightRank.Honor:
                {
                    GUI.DrawTexture(inRect, rankBackground_Honor, ScaleMode.StretchToFill);
                    return;
                }
            case ResidentKnightRank.Crown:
                {
                    GUI.DrawTexture(inRect, rankBackground_Crown, ScaleMode.StretchToFill);
                    return;
                }
            default: return;
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_MainBackground");
    private static readonly Texture2D academicBackground = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_AcademicBackground");
    private static readonly Texture2D stageBackground = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_StageBackground");

    private static readonly Texture2D textLace = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_TextLace");

    private static readonly Texture2D unlockButton = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_UnlockButton");
    private static readonly Texture2D unlockButton_Down = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_UnlockButton_Down");

    private static readonly Texture2D academicCuttingLine = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_AcademicCuttingLine");

    private static readonly Texture2D rankBackground_Regular = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Regular");
    private static readonly Texture2D rankBackground_Elite = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Elite");
    private static readonly Texture2D rankBackground_Honor = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Honor");
    private static readonly Texture2D rankBackground_Crown = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Crown");
}