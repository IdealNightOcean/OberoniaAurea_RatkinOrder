using OberoniaAurea_Frame;
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

    private ResidentKnight Record { get; }
    public AcademicHandler AcademicHandler { get; }
    private BranchHonorDef BranchHonor { get; }
    private int NoAdditionalCostAcademicCeiling { get; }

    private KnightAcademicDef SelAcademicDef { get; set; }
    private int SelAcademicStageLevel { get; set; }
    private float MeditationPointForSelAcademicUpgrade { get; set; }

    private ResidentKnightAcademicStage CheckAcademicStage { get; set; }
    private int CheckAcademicStageLevel { get; set; }
    private Color CheckAcademicColor { get; set; }
    private Texture2D CheckAcademicColorTex { get; set; }
    private AcceptanceReport CheckAcademicStageAcceptance { get; set; }

    private IReadOnlyList<(KnightAcademicDef, bool)> AllAvailableAcademics { get; set; }

    public Window_ResidentKnight_AcademicArrange(ResidentKnight record) : base()
    {
        Record = record;
        AcademicHandler = record.AcademicHandler;
        BranchHonor = record.Branch.HonorDef;
        NoAdditionalCostAcademicCeiling = AcademicUtility.GetNoAdditionalCostAcademicCeiling(Record.CurRank);

        HashSet<KnightAcademicDef> academicHash = new(AcademicHandler.Academics.Count);
        List<(KnightAcademicDef, bool)> allAvailableAcademics = new(AcademicHandler.Academics.Count);
        foreach (KnightAcademicDef academicDef in AcademicUtility.GetAllActivateAcademicsBySelf(Record))
        {
            academicHash.Add(academicDef);
            allAvailableAcademics.Add((academicDef, true));
        }
        foreach (KnightAcademicDef academicDef in AcademicHandler.Academics.Keys)
        {
            if (academicHash.Add(academicDef))
            {
                allAvailableAcademics.Add((academicDef, false));
            }
        }

        AllAvailableAcademics = allAvailableAcademics;
        academicHash = null;
        if (AllAvailableAcademics.Count > 0)
        {
            SwitchAcademic(AcademicHandler.Academics.First().Key);
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

        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1308f, 695f);
        Rect mainInnerRect = mainRect.ContractedBy(2f);
        float mainInnerX = mainInnerRect.xMin;
        float mainInnerY = mainInnerRect.yMin;

        if (OARO_WindowUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }

        Rect pawnRect = new(mainRect.xMin, mainRect.yMin, 198f, 272f);
        DarwPawnInfo(pawnRect);

        Rect academicRect = Rect.MinMaxRect(mainRect.xMin, pawnRect.yMax, mainRect.xMin + 198f, mainRect.yMax);
        DarwAcademicList(academicRect);

        Rect academicInfoRect = Rect.MinMaxRect(pawnRect.xMax, mainRect.yMin, mainRect.xMax, mainRect.yMax);
        DarwAcademicInfo(academicInfoRect);

        OARO_WindowUtility.ResetText();
    }

    private void DarwPawnInfo(Rect inRect)
    {
        Rect innerRect = inRect.ContractedBy(2f);
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;
        float innerWidth = innerRect.width;
        Rect reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 20f, 75f, 75f);
        GUI.DrawTexture(reusedRect, PortraitsCache.Get(Record.Pawn, reusedRect.size, Rot4.South));

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, reusedRect.yMax + 8f, innerWidth, 20f);
        Widgets.Label(reusedRect, Record.Pawn.NameShortColored);

        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 125f, 114f, 41f);
        DrawRankBackGround(reusedRect);
        Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{Record.CurRank}Knight".Translate());

        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 190f, innerWidth, 24f);
        Widgets.Label(reusedRect, "OARO_MeditationPoints".Translate(Record.MeditationPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count)));
        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, reusedRect.yMax, innerWidth, 24f);
        Widgets.Label(reusedRect, "OARO_NoAdditionalCostAcademicCeilingInfo".Translate(AcademicHandler.TotalAcademicLevel.Value, NoAdditionalCostAcademicCeiling));

        OARO_WindowUtility.ResetText();
    }

    private void DarwAcademicList(Rect inRect)
    {
        Rect viewRect = inRect;
        viewRect.yMin += 2f;
        viewRect.yMax -= 2f;

        float entryX = viewRect.xMin;
        float entryY = viewRect.yMin;
        float entryWidth = 198f;
        float entryHeight = 70f;
        viewRect.height = (AllAvailableAcademics.Count + 1) * entryHeight;

        Widgets.BeginScrollView(inRect, ref scrollPosition_Academic, viewRect, showScrollbars: false);
        foreach ((KnightAcademicDef def, bool activateBySelf) in AllAvailableAcademics)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DarwAcademic(entryRect, def, activateBySelf);
        }
        Widgets.EndScrollView();
        OARO_WindowUtility.ResetText();
    }

    private void DarwAcademic(Rect inRect, KnightAcademicDef def, bool activateBySelf)
    {
        GUI.DrawTexture(inRect, academicBackground);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(inRect.x, inRect.y + 14f, inRect.width, 20f);
        if (def.academicType == AcademicType.Honor)
        {
            Texture2D honorDecorationTexture = BranchHonor?.medalDef?.honorDecorationTexture.Texture;
            if (honorDecorationTexture is not null)
            {
                GUI.DrawTexture(inRect.ContractedBy(3f), honorDecorationTexture, ScaleMode.ScaleToFit);
            }
            Widgets.Label(reusedRect, def.LabelCap.Colorize(BranchHonor.color));
        }
        else
        {
            Widgets.Label(reusedRect, def.LabelCap);
        }

        int academicLevel = AcademicHandler.GetAcademicLevel(def);
        reusedRect = new(inRect.x, reusedRect.yMax + 10f, inRect.width, 20f);
        Widgets.Label(reusedRect, "OARO_AcademicArrange_UnlockNum".Translate(academicLevel, def.MaxStageLevel));

        if (def.chivalry.IsSameDefNonNullable(Record.Chivalry))
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.xMin + 4f, 20f, 20f);
            GUI.DrawTexture(reusedRect, IconLibrary.StarWhite);
            TooltipHandler.TipRegion(inRect, () => "OARO_ResidentAcademic_ResonateChivalry".Translate(), uniqueId: 3256725);
        }
        if (SelAcademicDef == def)
        {
            Widgets.DrawBox(inRect);
            Widgets.DrawHighlightSelected(inRect);
        }
        if (Widgets.ButtonInvisible(inRect))
        {
            SwitchAcademic(def);
        }
    }

    private void DarwAcademicInfo(Rect inRect)
    {
        if (SelAcademicDef is null)
        {
            return;
        }

        Rect innerRect = inRect.ContractedBy(2f);
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(innerX + 24f, innerY, innerRect.width, 32f);
        Widgets.Label(reusedRect, SelAcademicDef.LabelCap.Colorize(CheckAcademicColor));

        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 180f, 968f, 198f);
        GUI.DrawTexture(reusedRect, stageBackground);

        Rect stageOutRect = reusedRect.ContractedBy(2f);
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
            DrawAcademicStage(entryRect, i);
        }
        Widgets.EndScrollView();

        if (CheckAcademicStage is null)
        {
            OARO_WindowUtility.ResetText();
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

            reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax + 32f, 196f, 54f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                 butRect: reusedRect,
                 label: "OARO_Unlock".Translate(),
                 acceptance: CheckAcademicStageAcceptance,
                 baseTex: unlockButton,
                 downTex: unlockButton_Down,
                 doMouseoverSound: true))
            {
                AcceptanceReport acceptance = AcademicHandler.CanUpgradeAcademic(SelAcademicDef, Record.Chivalry, directly: false, resultOnly: false);
                if (acceptance)
                {
                    AcademicHandler.UpgradeAcademic(SelAcademicDef, Record.Pawn, Record.Chivalry, directly: false);
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
                    AcademicHandler.UpgradeAcademic(SelAcademicDef, Record.Pawn, Record.Chivalry, directly: true);
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

        OARO_WindowUtility.ResetText();
    }

    private void DrawAcademicStage(Rect inRect, int stageIndex)
    {
        int stageLevel = stageIndex + 1;
        bool active = stageLevel <= SelAcademicStageLevel;
        ResidentKnightAcademicStage stage = SelAcademicDef.academicStages[stageIndex];

        Rect innerRect = inRect;
        innerRect.xMax -= 2f;
        float innerX = innerRect.xMin;
        float innerY = innerRect.yMin;
        float innerWidth = innerRect.width;

        Rect reusedRect;

        if (stageLevel < SelAcademicDef.MaxStageLevel)
        {
            reusedRect = inRect;
            reusedRect.xMin = reusedRect.xMax - 2f;
            reusedRect.yMin += 4f;
            reusedRect.yMax -= 4f;
            GUI.DrawTexture(reusedRect, academicCuttingLine);
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(innerX, innerY + 32f, innerWidth, 20f);
        Widgets.Label(reusedRect, stage.label.CapitalizeFirst().Colorize(active ? Color.white : Color.gray));

        Text.Anchor = TextAnchor.UpperCenter;
        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 65f, 190f, 45f);
        Widgets.Label(reusedRect, stage.shortDescription.CapitalizeFirst().Colorize(active ? Color.green : Color.gray));


        reusedRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 120f, 30f, 25f);

        Rect selectBoxRect = OARO_WindowUtility.CenterRectOnX(innerRect, innerY + 155f, 20f, 20f);

        reusedRect = selectBoxRect.ContractedBy(2f);
        GUI.DrawTexture(reusedRect, BaseContent.BlackTex);
        Rect selectBoxActiveRect = reusedRect.ContractedBy(2f);
        if (active)
        {
            GUI.DrawTexture(selectBoxActiveRect, CheckAcademicColorTex);
        }

        if (stageLevel > 1)
        {
            Rect leftLineRect = new(inRect.xMin, OARO_WindowUtility.CenterMinCoords(selectBoxActiveRect.yMin, selectBoxActiveRect.height, 6f), selectBoxActiveRect.xMin - inRect.xMin, 6f);
            GUI.DrawTexture(leftLineRect, BaseContent.BlackTex);
            if (active)
            {
                leftLineRect.yMin += 2f;
                leftLineRect.yMax -= 2f;
                GUI.DrawTexture(leftLineRect, CheckAcademicColorTex);
            }
        }

        if (stageLevel < SelAcademicDef.MaxStageLevel)
        {
            Rect rightLineRect = new(selectBoxActiveRect.xMax, OARO_WindowUtility.CenterMinCoords(selectBoxActiveRect.yMin, selectBoxActiveRect.height, 6f), inRect.xMax - selectBoxActiveRect.xMax, 6f);
            GUI.DrawTexture(rightLineRect, BaseContent.BlackTex);
            if (stageLevel < SelAcademicStageLevel)
            {
                rightLineRect.yMin += 2f;
                rightLineRect.yMax -= 2f;
                GUI.DrawTexture(rightLineRect, CheckAcademicColorTex);
            }
        }

        if (CheckAcademicStage == stage)
        {
            Widgets.DrawBox(selectBoxRect, 2);
            Widgets.DrawHighlightSelected(innerRect);
        }

        if (Widgets.ButtonInvisible(innerRect))
        {
            SwithAcademicStage(stage, stageIndex);
        }
    }

    private void SwitchAcademic(KnightAcademicDef academicDef)
    {
        if (SelAcademicDef == academicDef)
        {
            return;
        }

        SelAcademicDef = academicDef;
        CheckAcademicColor = (academicDef.academicType == AcademicType.Honor) ? BranchHonor.color : SelAcademicDef.chivalry.color;
        CheckAcademicColorTex = (academicDef.academicType == AcademicType.Honor) ? BranchHonor.HonorColorTex : SelAcademicDef.chivalry.ColorTex;


        CheckAcademicStage = null;
        CheckAcademicStageLevel = -1;

        RefreshSelStageLevel();
    }

    private void SwithAcademicStage(ResidentKnightAcademicStage stage, int stageIndex)
    {
        if (CheckAcademicStage == stage)
        {
            return;
        }

        CheckAcademicStage = stage;
        CheckAcademicStageLevel = stageIndex + 1;
        if (CheckAcademicStageLevel == SelAcademicStageLevel + 1)
        {
            CheckAcademicStageAcceptance = AcademicHandler.CanUpgradeAcademic(academicDef: SelAcademicDef,
                                                                            Record.Chivalry,
                                                                          directly: false,
                                                                          resultOnly: false);
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

        MeditationPointForSelAcademicUpgrade = AcademicUtility.GetMeditationPointsNeeded(SelAcademicDef, Record.Chivalry, SelAcademicStageLevel + 1);
    }

    private void DrawRankBackGround(Rect inRect)
    {
        switch (Record.CurRank)
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