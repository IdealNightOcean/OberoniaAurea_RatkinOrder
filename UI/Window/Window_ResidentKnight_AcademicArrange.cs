using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using static KnightAcademicDef;

public class Window_ResidentKnight_AcademicArrange : OrderWindowBase
{
    public override Vector2 InitialSize => new(1402f, 789f);
    private Vector2 scrollPosition_Academic;
    private Vector2 scrollPosition_AcademicStage;
    private Vector2 scrollPosition_AcademicDesc;

    public Action PostArrangeNewAcademic { get; set; }

    private ResidentKnight Knight { get; }
    public AcademicHandler AcademicHandler { get; }
    private BranchHonorDef BranchHonor { get; }
    private int NoAdditionalCostAcademicCeiling { get; }



    private KnightAcademicDef SelAcademicDef { get; set; }
    private int SelAcademicStageLevel { get; set; }
    private float MeditationPointForSelAcademicUpgrade { get; set; }

    private ResidentKnightAcademicStage CheckAcademicStage { get; set; }
    private int CheckAcademicStageLevel { get; set; }
    private Color CheckAcademicColor { get; set; }
    private AcceptanceReport CheckAcademicStageAcceptance { get; set; }

    /*
     * 新字段
     */
    private UIDataDrawer_SelectableList<UIData_KnightAcademic, UIDataDrawer_KnightAcademic> AcademicListDrawer { get; }
    private UIDataDrawer_KnightAcademic AcademicEntryDrawer { get; } = new();
    private List<UIData_KnightAcademic> AvailableAcademics { get; } = [];

    private UIData_KnightAcademic SelAcademicData => AcademicListDrawer.SelectedItem;


    public Window_ResidentKnight_AcademicArrange(ResidentKnight record) : base()
    {
        Knight = record;
        AcademicHandler = record.AcademicHandler;
        BranchHonor = record.Branch.HonorDef;


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

        UIDataDrawer_KnightAcademic academicEntryDrawer = new();
        AcademicListDrawer = new(academicEntryDrawer, AvailableAcademics, rowLimit: 5, columnLimit: 1, horizontalWarp: true);
        AcademicListDrawer.UseRecommendOutRectSize();
        academicHash = null;
        if (AvailableAcademics.Count > 0)
        {
            SwitchAcademic(AvailableAcademics.First().Academic);
        }
        else
        {
            SwitchAcademic(DefDatabase<KnightAcademicDef>.AllDefs.First(d => d.academicType == AcademicType.Geneal));
        }
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        PostArrangeNewAcademic = null;
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);

        Rect mainRect = OARO_UIUtility.CenterRect(inRect, 1308f, 695f);
        Rect mainInnerRect = GenUI.ContractedBy(mainRect, 2f);
        float mainInnerX = mainInnerRect.xMin;
        float mainInnerY = mainInnerRect.yMin;

        if (OARO_UIUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }

        Rect pawnRect = new(mainRect.xMin, mainRect.yMin, 198f, 272f);
        DarwPawnInfo(pawnRect);

        Rect academicRect = Rect.MinMaxRect(mainRect.xMin, pawnRect.yMax, mainRect.xMin + 198f, mainRect.yMax);
        AcademicListDrawer.Draw(inRect.TopLeftCorner());

        Rect academicInfoRect = Rect.MinMaxRect(pawnRect.xMax, mainRect.yMin, mainRect.xMax, mainRect.yMax);
        DarwAcademicInfo(academicInfoRect);

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DarwPawnInfo(Rect inRect)
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

    private void DarwAcademicInfo(Rect inRect)
    {
        if (SelAcademicDef is null)
        {
            return;
        }

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
    }

    private void SwitchAcademic(KnightAcademicDef academicDef)
    {
        if (SelAcademicDef == academicDef)
            return;

        SelAcademicDef = academicDef;
        CheckAcademicColor = (academicDef.academicType == AcademicType.Honor) ? BranchHonor.color : SelAcademicDef.chivalry.color;


        CheckAcademicStage = null;
        CheckAcademicStageLevel = -1;

        RefreshSelStageLevel();
    }

    private void SwithAcademicStage(ResidentKnightAcademicStage stage, int stageIndex)
    {
        if (CheckAcademicStage == stage)
            return;

        CheckAcademicStage = stage;
        CheckAcademicStageLevel = stageIndex + 1;
        if (CheckAcademicStageLevel == SelAcademicStageLevel + 1)
        {
            CheckAcademicStageAcceptance = AcademicHandler.CanUpgradeAcademic(academicDef: SelAcademicDef, directly: false, resultOnly: false);
        }
        else
        {
            CheckAcademicStageAcceptance = false;
        }
    }

    private void RefreshSelStageLevel()
    {
        if (SelAcademicDef is null)
        {
            SelAcademicStageLevel = -1;
        }

        SelAcademicStageLevel = AcademicHandler.GetAcademicLevel(SelAcademicDef);

        MeditationPointForSelAcademicUpgrade = AcademicUtility.GetAcademicPointsCost(residentPawn: Knight,
                                                                                     academicDef: SelAcademicDef,
                                                                                     targetLevel: SelAcademicStageLevel + 1,
                                                                                     sourceLevel: SelAcademicStageLevel,
                                                                                     resultOnly: true,
                                                                                     explanation: out _);
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