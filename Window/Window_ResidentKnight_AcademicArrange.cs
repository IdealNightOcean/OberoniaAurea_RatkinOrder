using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Window_ResidentKnight_AcademicArrange : OrderWindowBase
{
    public override Vector2 InitialSize => new(1402f, 789f);

    private Vector2 scrollPosition_AcademicStage;

    public Action PostArrangeNewAcademic { get; set; }

    private ResidentKnightRecord Record { get; }
    private ResidentKnightAcademicDef SelAcademicDef { get; set; }
    private int SelStageLevel { get; set; }
    private ResidentKnightAcademicStage CheckAcademicStage { get; set; }
    private int CheckAcademicStageLevel { get; set; }
    private AcceptanceReport CheckAcademicStageAcceptance { get; set; }
    private List<TabRecord> GenealAcademicTabs { get; } = new(5);
    private List<TabRecord> HonorAcademicTab { get; } = new(1);

    public Window_ResidentKnight_AcademicArrange(ResidentKnightRecord record) : base()
    {
        Record = record;
        if (Record.GenealAcademicDefs.Count > 0)
        {
            SwitchAcademic(Record.GenealAcademicDefs.First().Key);
        }
        else
        {
            SwitchAcademic(DefDatabase<ResidentKnightAcademicDef>.AllDefs.Where(d => !d.isHonorAcademic).First());
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

        Rect reusedRect = mainRect;
        reusedRect.width *= 0.6f;
        GenealAcademicTabs.Clear();
        foreach (ResidentKnightAcademicDef academicDef in DefDatabase<ResidentKnightAcademicDef>.AllDefs.Where(d => !d.isHonorAcademic))
        {
            GenealAcademicTabs.Add(new TabRecord(academicDef.LabelCap, delegate
            {
                SwitchAcademic(academicDef);
            }, SelAcademicDef == academicDef));
        }
        TabDrawer.DrawTabs(reusedRect, GenealAcademicTabs, maxTabWidth: 140f);

        reusedRect = mainRect;
        reusedRect.xMin = reusedRect.xMax - 140f;
        HonorAcademicTab.Clear();
        if (Record.HonorAcademicDef is not null)
        {
            HonorAcademicTab.Add(new TabRecord(Record.HonorAcademicDef.LabelCap, delegate
            {
                SwitchAcademic(Record.HonorAcademicDef);
            }, SelAcademicDef == Record.HonorAcademicDef));
        }
        TabDrawer.DrawTabs(reusedRect, HonorAcademicTab, maxTabWidth: 140f);

        Rect mainInnerRect = mainRect.ContractedBy(2f);
        float mainInnerX = mainInnerRect.xMin;
        float mainInnerY = mainInnerRect.yMin;
        float mainInnerWidth = mainInnerRect.width;

        if (OARO_WindowUtility.DrawCloseX(mainInnerRect))
        {
            Close();
            return;
        }

        if (SelAcademicDef is not null)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(mainInnerX, mainInnerY + 96f, mainInnerWidth, 40f);
            Widgets.Label(reusedRect, SelAcademicDef.LabelCap);

            Rect academicStageOutRect = new(mainInnerX + 170f, mainInnerY + 210f, mainInnerWidth - 340f, 166f);
            Rect academicStageViewRect = academicStageOutRect;
            academicStageViewRect.height -= 16f;

            float entryX = academicStageViewRect.xMin;
            float entryY = academicStageViewRect.yMin;
            float entryWidth = 183f;
            float entryXInterval = 80f;
            float entryHeight = 96f;

            int totalStageCount = SelAcademicDef.MaxStageLevel;
            academicStageViewRect.width = totalStageCount * (entryWidth + entryXInterval);
            Widgets.BeginScrollView(academicStageOutRect, ref scrollPosition_AcademicStage, academicStageViewRect);
            for (int i = 0; i < totalStageCount; i++)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                Rect lineRect = OARO_WindowUtility.CenterRectOnY(entryRect, entryRect.xMin - entryXInterval, entryXInterval, 6f);
                if (i > 0)
                {
                    GUI.DrawTexture(lineRect, (SelStageLevel >= i + 1) ? activeStageLinkLine : stageLinkLine);
                }

                entryX += (entryWidth + entryXInterval);
                DrawAcademicStage(entryRect, stageIndex: i);
            }
            Widgets.EndScrollView();
        }

        reusedRect = new(mainInnerX + 150f, mainInnerY + 450f, 75f, 95f);
        GUI.DrawTexture(reusedRect, PortraitsCache.Get(Record.Knight, reusedRect.size, Rot4.South));

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(mainInnerX + 170f, reusedRect.yMax + 16f, 75f, 20f);
        Widgets.Label(reusedRect, Record.Knight.NameShortColored);

        reusedRect = new(mainInnerX + 270f, mainInnerY + 475f, 114f, 41f);
        DrawRankBackGround(reusedRect);
        Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{Record.CurRank}Knight".Translate());

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(mainInnerX + 420f, mainInnerY + 480f, 196f, 20f);
        Widgets.Label(reusedRect, "OARO_MeditationPoints".Translate(Record.MeditationPoints.ToString("F0").Named(KeyLibrary_FormatArgName.Count)));
        reusedRect = new(mainInnerX + 420f, mainInnerY + 500f, 196f, 20f);
        Widgets.Label(reusedRect, "OARO_NoAdditionalCostAcademicCeilingInfo".Translate(
            Record.TotalAcademicLevel.Value,
            ResidentKnightRecord.GetNoAdditionalCostAcademicCeiling(Record.CurRank)));

        if (CheckAcademicStage is not null)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(mainInnerX + 600f, mainInnerY + 440f, 280f, 32f);
            Widgets.Label(reusedRect, $"{SelAcademicDef.LabelCap} - {CheckAcademicStage.label.CapitalizeFirst()}");

            Text.Font = GameFont.Small;
            reusedRect = new(mainInnerX + 640f, mainInnerY + 488f, 200f, 80f);
            Widgets.Label(reusedRect, CheckAcademicStage.description);

            if (CheckAcademicStageLevel <= SelStageLevel)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;

                reusedRect = new(mainInnerX + 935f, mainInnerY + 464f, 164f, 64f);
                Widgets.Label(reusedRect, "OARO_Unlocked".Translate());
                GUI.DrawTexture(reusedRect, textLace, ScaleMode.ScaleToFit);
            }
            else if (CheckAcademicStageLevel == SelStageLevel + 1)
            {
                Text.Font = GameFont.Medium;
                reusedRect = new(mainInnerX + 935f, mainInnerY + 440f, 196f, 32f);
                float meditationPointsNeeded = ResidentKnightRecord.GetMeditationPointsNeeded(SelAcademicDef, Record.Personality, CheckAcademicStageLevel);
                Widgets.Label(reusedRect, "OARO_MeditationPointsSuffix".Translate(meditationPointsNeeded.ToString("F0").Named(KeyLibrary_FormatArgName.Count)));

                reusedRect = new(mainInnerX + 935f, mainInnerY + 490f, 196f, 54f);
                if (Widgets.ButtonText(reusedRect, "OARO_Unlock".Translate()))
                {
                    AcceptanceReport acceptance = Record.CanUpgradeAcademicLevel(SelAcademicDef, ignorePoints: false, resultOnly: false);
                    if (acceptance)
                    {
                        Record.UpgradeAcademicLevel(SelAcademicDef, usePoints: true);
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
                    reusedRect = new(reusedRect.xMax + 20f, reusedRect.yMax - 20f, 20f, 20f);
                    if (Widgets.ButtonText(reusedRect, "DEV"))
                    {
                        Record.UpgradeAcademicLevel(SelAcademicDef, usePoints: false);
                        PostArrangeNewAcademic?.Invoke();
                        RefreshSelStageLevel();
                    }
                }
            }
            else
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;

                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;

                reusedRect = new(mainInnerX + 935f, mainInnerY + 464f, 164f, 64f);
                Widgets.Label(reusedRect, "OARO_NeedPreAcademicLevel".Translate());
                GUI.DrawTexture(reusedRect, textLace, ScaleMode.ScaleToFit);
            }
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawAcademicStage(Rect inRect, int stageIndex)
    {
        int stageLevel = stageIndex + 1;
        ResidentKnightAcademicStage academicStage = SelAcademicDef.academicStages[stageIndex];
        bool active = SelStageLevel >= stageLevel;

        GUI.DrawTexture(inRect, active ? activeStageBackGround : stageBackGround);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(inRect.x, inRect.y + 16f, inRect.width, 20f);
        Widgets.Label(reusedRect, academicStage.label.CapitalizeFirst().Colorize(active ? Color.white : Color.gray));

        reusedRect = inRect;
        reusedRect.yMin = inRect.y + 36f;
        reusedRect.yMax -= 16f;
        Widgets.Label(reusedRect, academicStage.shortDescription.Colorize(active ? Color.green : Color.gray));

        if (CheckAcademicStage == academicStage)
        {
            Widgets.DrawBox(inRect);
        }
        if (Widgets.ButtonInvisible(inRect))
        {
            if (CheckAcademicStage != academicStage)
            {
                CheckAcademicStage = academicStage;
                CheckAcademicStageLevel = stageLevel;
                if (active)
                {
                    CheckAcademicStageAcceptance = false;
                }
                else
                {
                    CheckAcademicStageAcceptance = Record.CanUpgradeAcademicLevel(academicDef: SelAcademicDef,
                                                                                  ignorePoints: false,
                                                                                  resultOnly: false);
                }
            }
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawRankBackGround(Rect inRect)
    {
        switch (Record.CurRank)
        {
            case ResidentKnightRecord.Rank.Regular:
                {
                    GUI.DrawTexture(inRect, rankBackground_Regular, ScaleMode.StretchToFill);
                    return;
                }
            case ResidentKnightRecord.Rank.Elite:
                {
                    GUI.DrawTexture(inRect, rankBackground_Elite, ScaleMode.StretchToFill);
                    return;
                }
            case ResidentKnightRecord.Rank.Honor:
                {
                    GUI.DrawTexture(inRect, rankBackground_Honor, ScaleMode.StretchToFill);
                    return;
                }
            case ResidentKnightRecord.Rank.Crown:
                {
                    GUI.DrawTexture(inRect, rankBackground_Crown, ScaleMode.StretchToFill);
                    return;
                }
            default: return;
        }
    }

    private void SwitchAcademic(ResidentKnightAcademicDef academicDef)
    {
        if (SelAcademicDef == academicDef)
        {
            return;
        }

        SelAcademicDef = academicDef;
        CheckAcademicStage = null;
        RefreshSelStageLevel();
    }

    private void RefreshSelStageLevel()
    {
        if (SelAcademicDef is null)
        {
            SelStageLevel = -1;
        }

        if (SelAcademicDef.isHonorAcademic)
        {
            SelStageLevel = Record.HonorAcademicLevel;
        }
        else if (Record.GenealAcademicDefs.TryGetValue(SelAcademicDef, out int selStageLevel))
        {
            SelStageLevel = selStageLevel;
        }
        else
        {
            SelStageLevel = 0;
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_MainBackground");

    private static readonly Texture2D stageBackGround = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_StageBackground");
    private static readonly Texture2D activeStageBackGround = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_ActiveStageBackground");

    private static readonly Texture2D stageLinkLine = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_StageLinkLine");
    private static readonly Texture2D activeStageLinkLine = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_ActiveStageLinkLine");

    private static readonly Texture2D textLace = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_TextLace");

    private static readonly Texture2D rankBackground_Regular = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Regular");
    private static readonly Texture2D rankBackground_Elite = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Elite");
    private static readonly Texture2D rankBackground_Honor = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Honor");
    private static readonly Texture2D rankBackground_Crown = ContentFinder<Texture2D>.Get("UI/ResidentKnight/AcademicArrange/OARO_RankBackground_Crown");

}
