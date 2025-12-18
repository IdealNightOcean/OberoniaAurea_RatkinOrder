using NightOcean;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public partial class Window_OrderHall
{
    [StaticConstructorOnStartup]
    private class ResidentKnightEntryDrawer
    {
        public const float Width = 426f;
        public const float SummaryHeight = 63f;
        public const float DetailHeight = 288f;

        private Vector2 scrollPosition_GenealAcademic;
        public Window_OrderHall Parent { get; }
        public ResidentKnightRecord Record { get; }
        public Map Map { get; }
        private float MeditationFactor { get; }
        public bool ShowDetail { get; set; }
        public LazyMutable<string> RoleExplanationStr { get; }
        public LazyMutable<string> ResonatePersonalitiesStr { get; }

        public LazyMutable<AcceptanceReport> RankUpgradeAcceptance { get; }
        public LazyMutable<AcceptanceReport> PostponeResignationAcceptance { get; }

        public LazyMutable<(int, int)> PreferredFurnitureCount { get; }
        public LazyMutable<string> PreferredFurnitureExplanation { get; }

        public ResidentKnightEntryDrawer(Window_OrderHall parent, ResidentKnightRecord record, Map map)
        {
            Parent = parent;
            Record = record;
            Map = map;
            MeditationFactor = record.Knight.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);

            RoleExplanationStr = new(refreshFunc: () => Record?.CurRole?.GetRoleDetailDesc() ?? string.Empty);
            ResonatePersonalitiesStr = new(refreshFunc: RefreshResonatePersonalitiesStr);
            RankUpgradeAcceptance = new(refreshFunc: () => GlobalInteractionUtility.CanUpgradeResidentKnightRank(Record, Map, resultOnly: false));
            PostponeResignationAcceptance = new(refreshFunc: () => GlobalInteractionUtility.CanPostponeResidentKnightkResignation(Record, Map, resultOnly: false));
            PreferredFurnitureCount = new(refreshFunc: RefreshPreferredFurnitureCount);
            PreferredFurnitureExplanation = new(refreshFunc: RefreshFurnitureExplanation);
        }

        public void ChangeShowDetail()
        {
            ShowDetail = !ShowDetail;
        }

        public void ClearCache()
        {
            ShowDetail = false;

            RoleExplanationStr.Reset();
            ResonatePersonalitiesStr.Reset();
            RankUpgradeAcceptance.Reset();
            PostponeResignationAcceptance.Reset();
            PreferredFurnitureCount.Reset();
            PreferredFurnitureExplanation.Reset();
        }

        public void OnConditionChanged()
        {
            RankUpgradeAcceptance.MarkDirty();
            PostponeResignationAcceptance.MarkDirty();
        }

        public float Draw(Vector2 position)
        {
            Rect summaryRect = new(position.x, position.y, Width, SummaryHeight);
            GUI.DrawTexture(summaryRect, residentKnightSummary);

            Rect summaryInnerRect = summaryRect.ContractedBy(2f);
            float summaryInnerRectX = summaryInnerRect.xMin;
            float summaryInnerRectY = summaryInnerRect.yMin;

            Rect tileRect = summaryInnerRect;
            float titleRectHeight = 36f;
            tileRect.height = titleRectHeight;

            Rect reusedRect = new(tileRect.xMax - 247f, summaryInnerRectY, 247f, titleRectHeight);
            DrawRankBackGround(reusedRect);

            reusedRect = new(tileRect.x + 4f, tileRect.y + 1f, 24f, titleRectHeight - 2f);
            GUI.DrawTexture(reusedRect, PortraitsCache.Get(Record.Knight, reusedRect.size, Rot4.South));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(summaryInnerRectX + 30f, summaryInnerRectY, 50f, titleRectHeight);
            Widgets.Label(reusedRect, Record.Knight.NameShortColored);

            reusedRect = OARO_WindowUtility.CenterRectOnY(tileRect, summaryInnerRectX + 115f, 45f, titleRectHeight - 2f);
            if (Record.CurRole is not null)
            {
                GUI.DrawTexture(reusedRect, Record.CurRole.iconTexture.Texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.DrawTexture(reusedRect, IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
            }
            if (Mouse.IsOver(reusedRect))
            {
                Widgets.DrawHighlight(reusedRect);
                TooltipHandler.TipRegion(reusedRect, () => RoleExplanationStr.Value, uniqueId: 36431436);
            }
            if (Widgets.ButtonInvisible(reusedRect))
            {
                RoleFloatMenu();
            }

            reusedRect = new(summaryInnerRectX + 150f, summaryInnerRectY, 85f, titleRectHeight);
            Widgets.Label(reusedRect, MeditationFactor.ToStringPercent().Colorize(MeditationFactor < 1f ? ColorLibrary.RedReadable : Color.green));

            reusedRect = new(summaryInnerRectX + 235f, summaryInnerRectY, 85f, titleRectHeight);
            Widgets.Label(reusedRect, Record.MeditationPoints.ToString("F0"));

            reusedRect = new(summaryInnerRectX + 320f, summaryInnerRectY, 85f, titleRectHeight);
            Widgets.Label(reusedRect, ResidentKnightRecord.GetRankLabel(Record.CurRank));

            reusedRect = summaryInnerRect;
            reusedRect.yMin = tileRect.yMax + 1f;
            Rect buttomTextRect = reusedRect;
            buttomTextRect.xMin += 25f;
            if (ShowDetail)
            {
                GUI.DrawTexture(reusedRect, detailButton_Down);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(buttomTextRect, "OARO_HallWin_ShowDetail".Translate());
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonInvisible(reusedRect, doMouseoverSound: true))
                {
                    Parent.OnShowDrawerDetailChanged(this);
                    OARO_WindowUtility.ResetText();
                    return summaryRect.yMax;
                }
                else
                {
                    return DrawDetail(new Vector2(position.x, summaryRect.yMax));
                }
            }
            else
            {
                GUI.DrawTexture(reusedRect, detailButton);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(buttomTextRect, "OARO_HallWin_ShowDetail".Translate());
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonInvisible(reusedRect, doMouseoverSound: true))
                {
                    Parent.OnShowDrawerDetailChanged(this);
                }
                OARO_WindowUtility.ResetText();
                return summaryRect.yMax;
            }
        }

        private float DrawDetail(Vector2 position)
        {
            Rect inRect = new(position.x, position.y, Width, DetailHeight);
            GUI.DrawTexture(inRect, residentKnightDetail);

            inRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.y, 422f, DetailHeight);
            float inRectX = inRect.xMin;
            float inRectY = inRect.yMin;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect reusedRect = new(inRectX + 32f, inRectY + 4f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_AttachToBranch".Translate());
            reusedRect = new(inRectX + 32f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_KnightPersonality".Translate());
            reusedRect = new(inRectX + 32f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_KnightResonatePersonalities".Translate());
            reusedRect = new(inRectX + 32f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_KnightRank".Translate());

            Rect buttonRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRectX + 335f, 71f, 22f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: buttonRect,
                label: "OARO_HallWin_UpgradeRank".Translate(),
                acceptance: RankUpgradeAcceptance.Value,
                baseTex: smallButton,
                downTex: smallButton_Down,
                doMouseoverSound: true))
            {
                Window_ResidentKnight_RankUpgrade rankUpgradeWin = new(Record, Map);
                rankUpgradeWin.PostAcceptAction += OnConditionChanged;
                Find.WindowStack.Add(rankUpgradeWin);
            }

            reusedRect = new(inRectX + 32f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_PreferredFurniture".Translate());

            reusedRect = new(inRectX + 32f, inRectY + 140f, 256f, 20f);

            Widgets.Label(reusedRect, "OARO_HallWin_ResignationDay".Translate(
                GenDate.DateFullStringAt(
                    absTicks: GenDate.TickGameToAbs(Record.ResignationTick),
                    location: Find.WorldGrid.LongLatOf(Map.Tile))));
            buttonRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRectX + 264f, 71f, 22f);
            if (OARO_WindowUtility.TextButtonImage(buttonRect, "OARO_HallWin_DismissalKnight".Translate(), smallButton, smallButton_Down, doMouseoverSound: true))
            {
                Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                    text: "OARO_HallWin_DismissalKnightConfirm".Translate(Record.Knight.Named(KeyLibrary_FormatArgName.PAWN)),
                    ratkinOrder: Record.RatkinOrder,
                    acceptAction: delegate
                    {
                        ResidentKnightsManager.Instance.RemoveResidentKnight(Record.Knight);
                        Parent.OnShowDrawerDetailChanged(this);
                        Parent.ResidentKnightDrawers.Remove(this);
                    });
                Find.WindowStack.Add(nodeTree);
            }
            buttonRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRectX + 335f, 71f, 22f);
            if (OARO_WindowUtility.TextButtonImageDisableable(buttonRect,
                label: "OARO_HallWin_PostponeResignation".Translate(),
                acceptance: PostponeResignationAcceptance.Value,
                baseTex: smallButton,
                downTex: smallButton_Down,
                doMouseoverSound: true))
            {
                AcceptanceReport acceptance = GlobalInteractionUtility.CanPostponeResidentKnightkResignation(Record, Map, resultOnly: false);
                if (acceptance)
                {
                    Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                        text: "OARO_HallWin_PostponeResignationConfirm".Translate(Record.Knight.Named(KeyLibrary_FormatArgName.PAWN), Record.RatkinOrder.NameColored.Named(KeyLibrary_FormatArgName.OrderName)),
                        ratkinOrder: Record.RatkinOrder,
                        acceptAction: delegate
                        {
                            GlobalInteractionUtility.PostponeResidentKnightkResignation(Record, Map);
                            OnConditionChanged();
                        });
                    Find.WindowStack.Add(nodeTree);
                }
                else
                {
                    Messages.Message("OARO_CanNotPostponeResidentKnightkResignationWithReason".Translate(acceptance.Reason.Named(KeyLibrary_FormatArgName.Reason)), MessageTypeDefOf.RejectInput, historical: false);
                    OnConditionChanged();
                }
            }

            reusedRect = new(inRectX + 260f, inRectY + 4f, 128f, 20f);
            Widgets.Label(reusedRect, Record.Branch.NameColored);
            reusedRect = new(inRectX + 260f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, KnightPersonalityUtility.GetPersonalityLabel(Record.Personality));
            reusedRect = new(inRectX + 260f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, ResonatePersonalitiesStr.Value);
            reusedRect = new(inRectX + 260f, reusedRect.yMax + 6f, 128f, 20f);
            Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{Record.CurRank}Knight".Translate().Colorize(ResidentKnightRecord.GetRankColor(Record.CurRank)));

            reusedRect = new(inRectX + 260f, reusedRect.yMax + 6f, 128f, 20f);
            Rect starRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMin, 18f, 18f);
            float starRectX = starRect.xMin;
            float starRectY = starRect.yMin;
            int starCount = 0;
            while (starCount < 6 && starCount < PreferredFurnitureCount.Value.Item1)
            {
                starCount++;
                starRect = new(starRectX, starRectY, 18f, 18f);
                starRectX += 20f;
                GUI.DrawTexture(starRect, IconLibrary.StarWhite, ScaleMode.ScaleToFit);
            }
            while (starCount < 6 && starCount < PreferredFurnitureCount.Value.Item2)
            {
                starCount++;
                starRect = new(starRectX, starRectY, 18f, 18f);
                starRectX += 20f;
                GUI.DrawTexture(starRect, IconLibrary.StarBlack, ScaleMode.ScaleToFit);
            }
            TooltipHandler.TipRegion(reusedRect, () => PreferredFurnitureExplanation.Value, uniqueId: 59748631);

            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX + 260f, inRectY + 164f, 80f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_HonorAcademic".Translate());
            reusedRect = new(inRectX + 240f, reusedRect.yMax + 8f, 100f, 20f);


            if (Record.HonorAcademicDef is null)
            {
                Widgets.Label(reusedRect, "None".Translate());

                reusedRect = new(inRectX + 260, inRectY + 226f, 128f, 22f);
                GUI.DrawTexture(reusedRect, BaseContent.BlackTex);
            }
            else
            {
                BranchHonorDef honorDef = Record.Branch.HonorDef;
                Widgets.Label(reusedRect, Record.HonorAcademicDef.label.Colorize(honorDef.color));
                reusedRect = new(inRectX + 320f, inRectY + 164f, 90f, 55f);
                GUI.DrawTexture(reusedRect, honorDef.iconTexture.Texture, ScaleMode.ScaleToFit);

                float honorAcademicProgress = Record.HonorAcademicLevel / (float)Record.HonorAcademicDef.MaxStageLevel;
                reusedRect = new(inRectX + 260, inRectY + 226f, 128f, 22f);
                Widgets.FillableBar(reusedRect, honorAcademicProgress, honorDef.HonorColorTex);
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX + 255, inRectY + 238f, 142f, 43f);
            if (OARO_WindowUtility.TextButtonImage(
                butRect: reusedRect,
                label: "OARO_HallWin_ArrangeAcademic".Translate(),
                baseTex: academicButton,
                downTex: academicButton_Down,
                doMouseoverSound: true))
            {
                Window_ResidentKnight_AcademicArrange academicArrangeWin = new(Record);
                academicArrangeWin.PostArrangeNewAcademic += RankUpgradeAcceptance.MarkDirty;
                Find.WindowStack.Add(academicArrangeWin);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            reusedRect = new(inRectX + 32f, inRectY + 164f, 256f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_GenealAcademic".Translate());
            Rect academicRect = Rect.MinMaxRect(inRectX + 32f, reusedRect.yMax + 8f, inRectX + (32f + 180f), inRect.yMax - 2f);
            float entryX = academicRect.xMin;
            float entryY = academicRect.yMin;
            float entryHeight = 22f;
            Rect academicViewRect = academicRect;
            float entryWidth = academicViewRect.width;
            academicViewRect.height = (Record.GenealAcademicDefs.Count + 1) * entryHeight;

            Widgets.BeginScrollView(academicRect, ref scrollPosition_GenealAcademic, academicViewRect, showScrollbars: false);
            foreach (KeyValuePair<ResidentKnightAcademicDef, int> kv in Record.GenealAcademicDefs)
            {
                Vector2 entryPos = new(entryX, entryY);
                entryY += entryHeight;
                DrawGenealAcademic(entryPos, kv.Key, kv.Value);
            }
            Widgets.EndScrollView();

            OARO_WindowUtility.ResetText();
            return inRect.yMax;
        }

        private void DrawGenealAcademic(Vector2 position, ResidentKnightAcademicDef academicDef, int academicLevel)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRect = new(position.x, position.y + 1f, 75f, 20f);
            Widgets.LabelEllipses(textRect, academicDef.label);

            Rect levelRect = new(textRect.xMax + 2f, position.y + 1f, 85f, 20f);
            GUI.DrawTexture(levelRect, BaseContent.BlackTex);
            levelRect = levelRect.ContractedBy(2f);

            float paneWidth = levelRect.width / academicDef.MaxStageLevel;
            float paneInterval = paneWidth * 0.33f;
            paneWidth *= 0.67f;

            float paneX = levelRect.x;
            float paneY = levelRect.y;
            float paneHeight = levelRect.height;

            for (int i = 0; i < academicLevel; i++)
            {
                Rect paneRect = new(paneX, paneY, paneWidth, paneHeight);
                paneX += (paneWidth + paneInterval);
                GUI.DrawTexture(paneRect, BaseContent.WhiteTex);
            }
        }

        private void DrawRankBackGround(Rect inRect)
        {
            switch (Record.CurRank)
            {
                case ResidentKnightRecord.Rank.Regular:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_RegularS, ScaleMode.StretchToFill);
                        return;
                    }
                case ResidentKnightRecord.Rank.Elite:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_EliteS, ScaleMode.StretchToFill);
                        return;
                    }
                case ResidentKnightRecord.Rank.Honor:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_HonorS, ScaleMode.StretchToFill);
                        return;
                    }
                case ResidentKnightRecord.Rank.Crown:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_CrownS, ScaleMode.StretchToFill);
                        return;
                    }
                default: return;
            }
        }

        private string RefreshResonatePersonalitiesStr()
        {
            string result = string.Empty;
            foreach (KnightPersonality personality in KnightPersonalityUtility.GetContainedPersonalities(KnightPersonalityUtility.GetResonatePersonality(Record.Personality)))
            {
                if (!string.IsNullOrEmpty(result))
                {
                    result += "  ";
                }
                if ((ResidentKnightsManager.Instance.AllHasPersonalityTypes.Value & personality) != 0)
                {
                    result += KnightPersonalityUtility.GetPersonalityLabel(personality);
                }
                else
                {
                    result += KnightPersonalityUtility.GetPersonalityLabel(personality).Colorize(Color.gray);
                }
            }
            return result;
        }

        private (int, int) RefreshPreferredFurnitureCount()
        {
            if (!OrderDefDataBase.TryGetAllPreferredBuildingsByPersonality(Record.Personality, out List<ThingDef> allJoyBuildings))
            {
                return (0, 0);
            }
            if (OrderHallHandler.Instance.KnightJoyBuildingDefsByPersonality.TryGetValue(Record.Personality, out HashSet<ThingDef> joyBuildingDefs))
            {
                return (joyBuildingDefs.Count, allJoyBuildings.Count);
            }
            return (0, allJoyBuildings.Count);
        }

        private string RefreshFurnitureExplanation()
        {
            if (!OrderDefDataBase.TryGetAllPreferredBuildingsByPersonality(Record.Personality, out List<ThingDef> allJoyBuildings))
            {
                return string.Empty;
            }
            OrderHallHandler.Instance.KnightJoyBuildingDefsByPersonality.TryGetValue(Record.Personality, out HashSet<ThingDef> joyBuildingDefs);
            joyBuildingDefs ??= [];

            StringBuilder sb = new();
            foreach (ThingDef def in allJoyBuildings)
            {
                if (joyBuildingDefs.Contains(def))
                {
                    sb.AppendLine(def.label);
                }
                else
                {
                    sb.AppendLine(def.label.Colorize(Color.gray));
                }
            }
            return sb.ToString();
        }

        private void RoleFloatMenu()
        {
            List<FloatMenuOption> options = [];
            int ticksGame = Find.TickManager.TicksGame;
            if (Record.NextRoleChangeableTick > ticksGame)
            {
                int coolingTicksLeft = Record.NextRoleChangeableTick - ticksGame;
                options.Add(new FloatMenuOption("WaitTime".Translate(coolingTicksLeft.ToStringTicksToPeriod()), action: null));
            }
            else
            {
                ResidentKnightsManager residentKnightsManager = ResidentKnightsManager.Instance;
                foreach (ResidentKnightRoleDef roleDef in DefDatabase<ResidentKnightRoleDef>.AllDefsListForReading)
                {
                    if (residentKnightsManager.TryGetKnightOfRole(roleDef, out ResidentKnightRecord otherRecord))
                    {
                        if (otherRecord.NextRoleChangeableTick > ticksGame)
                        {
                            int coolingTicksLeft = Record.NextRoleChangeableTick - ticksGame;
                            options.Add(new FloatMenuOption(
                                label: $"{roleDef.label} ({otherRecord.Knight.NameShortColored}), " + "WaitTime".Translate(coolingTicksLeft.ToStringTicksToPeriod()),
                                action: null));
                        }
                        else
                        {
                            options.Add(new FloatMenuOption(roleDef.label, action: () => RoleChangeConfirmDialog(roleDef, replaceCurRole: true)));
                        }
                    }
                    else
                    {
                        options.Add(new FloatMenuOption(roleDef.label, action: () => RoleChangeConfirmDialog(roleDef, replaceCurRole: false)));
                    }
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void RoleChangeConfirmDialog(ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
        {
            StringBuilder sb = new("OARO_HallWin_RoleChangeConfirm".Translate(Record.Knight.Named(KeyLibrary_FormatArgName.PAWN), roleDef.Named("ROLEDEF")));
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(roleDef.GetRoleDetailDesc());
            Dialog_NodeTreeWithRatkinOrderInfo nodeTree = OARO_WindowUtility.DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(
                text: sb.ToTaggedString(),
                Record.RatkinOrder,
                acceptAction: delegate
                {
                    if (ResidentKnightsManager.Instance.TrySetResidentKnightRole(Record.Knight, roleDef, replaceCurRole: replaceCurRole))
                    {
                        RoleExplanationStr.MarkDirty();
                        Parent.BuffHediffStageExplanation.MarkDirty();
                    }
                });
            Find.WindowStack.Add(nodeTree);
        }


        private static readonly Texture2D residentKnightSummary = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_KnightSummary");
        private static readonly Texture2D residentKnightDetail = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_KnightDetail");

        private static readonly Texture2D detailButton = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_DetailButton");
        private static readonly Texture2D detailButton_Down = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_DetailButton_Down");

        private static readonly Texture2D smallButton = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_SmallButton");
        private static readonly Texture2D smallButton_Down = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_SmallButton_Down");

        private static readonly Texture2D academicButton = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_AcademicButton");
        private static readonly Texture2D academicButton_Down = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_AcademicButton_Down");

        private static readonly Texture2D rankBackGround_RegularS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_RegularS");
        private static readonly Texture2D rankBackGround_EliteS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_EliteS");
        private static readonly Texture2D rankBackGround_HonorS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_HonorS");
        private static readonly Texture2D rankBackGround_CrownS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_CrownS");
    }
}