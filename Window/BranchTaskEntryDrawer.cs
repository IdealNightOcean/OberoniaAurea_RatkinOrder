using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using static OberoniaAurea.RatkinOrder.BranchTaskHandler;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

public partial class Window_BranchTask
{
    [StaticConstructorOnStartup]
    private class BranchTaskEntryDrawer
    {
        private const float Width = 637f;
        public const float UpRectHeight = 82f;
        public const float DetailRectHeight = 301f;
        private static readonly List<BranchInteractionDef> TaskNeedBranchInteractions =
        [
            BranchInteractionDefOf.OARO_RequestCombatReadiness,
            BranchInteractionDefOf.OARO_MapRecommendationToKnight,
            BranchInteractionDefOf.OARO_MapSilverToSupply,
        ];

        private Vector2 scrollPosition_Medals;
        private Vector2 scrollPosition_BackTeamKnights;

        private Window_BranchTask Parent { get; }
        public Branch Branch { get; }
        private Map Map { get; }
        private KnightChivalryDef ProtogenicChivalry { get; }
        private bool ShowDetail { get; set; }

        private JointPatrolManager JointPatrolManager => Branch.RatkinOrder.JointPatrolManager;

        private LazyMutable<JointBranchRecord> JointBranchRecord { get; }
        private Lazy<int> CrewCeiling { get; }

        private LazyMutable<string> BackTeamKnightsLabels { get; }
        private LazyMutable<string> ExpectedTaskRevenue { get; }

        private LazyMutable<AcceptanceReport> ChangeJointPatrolStateAcceptance { get; }
        private LazyMutable<AcceptanceReport> ChangeRadicalismDegreeAcceptance { get; }
        private LazyMutable<AcceptanceReport> ChangeFocusedTaskTypeAcceptance { get; }
        public LazyMutable<Dictionary<BranchInteractionDef, AcceptanceReport>> InteractionAcceptances { get; }
        private LazyMutable<List<KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>>> PatrolInteractionAcceptances { get; }


        public BranchTaskEntryDrawer(Window_BranchTask parent, Branch branch, Map map)
        {
            Parent = parent;
            Branch = branch;
            Map = map;
            ProtogenicChivalry = Branch.MedalHandler.PrimaryChivalry;

            JointBranchRecord = new(refreshFunc: RefreshJointBranchRecord);

            CrewCeiling = new(valueFactory: () => (int)(Branch.Squad.MemberCeiling + Branch.Squad.CommanderCeiling));

            BackTeamKnightsLabels = new(refreshFunc: RefreshBackTeamKnightsLabels);
            ExpectedTaskRevenue = new(refreshFunc: RefreshExpectedTaskRevenue);

            ChangeJointPatrolStateAcceptance = new(refreshFunc: () => RefreshChangeJointPatrolState(resultOnly: false));
            ChangeRadicalismDegreeAcceptance = new(refreshFunc: () => BranchUtility.CanChangeRadicalismDegree(branch, resultOnly: false));
            ChangeFocusedTaskTypeAcceptance = new(refreshFunc: () => BranchUtility.CanChangeFocusedTaskType(branch, resultOnly: false));
            InteractionAcceptances = new(refreshFunc: RefreshInteractionAcceptances);
            PatrolInteractionAcceptances = new(refreshFunc: RefreshPatrolInteractionAcceptances);
        }


        public void ClearCache()
        {
            ShowDetail = false;
            Branch.PostApplyBranchInteraction -= PostApplyBranchInteraction;

            JointBranchRecord.Reset();

            BackTeamKnightsLabels.Reset();
            ExpectedTaskRevenue.Reset();

            ChangeRadicalismDegreeAcceptance.Reset();
            ChangeFocusedTaskTypeAcceptance.Reset();
            InteractionAcceptances.Reset();
            PatrolInteractionAcceptances.Reset();
        }

        public void ChangeShowDetail()
        {
            ShowDetail = !ShowDetail;
            if (ShowDetail)
            {
                Branch.PostApplyBranchInteraction -= PostApplyBranchInteraction;
                Branch.PostApplyBranchInteraction += PostApplyBranchInteraction;
            }
            else
            {
                ClearCache();
            }
        }

        public float DrawTaskEntry(Vector2 position, bool showAsJointPatrol)
        {
            float positionX = position.x;
            float positionY = position.y;

            Rect inRect = new(positionX, positionY, Width, UpRectHeight);
            GUI.DrawTexture(inRect, taskEntryUpBackground);

            Rect innerRect = inRect.ContractedBy(2f);
            float innerRectX = innerRect.xMin;

            Rect topRect = innerRect;
            topRect.yMax = topRect.yMin + 50f;

            Rect reusedRect = topRect;
            reusedRect.xMax = topRect.xMin + 5f;
            GUI.DrawTexture(reusedRect, Branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);

            reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, innerRectX + 13f, 15f, 15f);
            OARO_WindowUtility.DrawBranchIcon(reusedRect, Branch, expand: false);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, innerRectX + 65f, 128f, 20f);
            Widgets.Label(reusedRect, Branch.NameColored);

            reusedRect = OARO_WindowUtility.CenterRectOnY(topRect, innerRectX + 205f, 72f, 20f);

            if (showAsJointPatrol && JointBranchRecord.Value is not null)
            {
                DrawJointPatrol(topRect);
            }
            else
            {
                DrawNoramlTask(topRect);
            }


            reusedRect = new(positionX + 2f, positionY + 54f, Width - 4f, 26f);
            Rect shoeDetailTextRect = OARO_WindowUtility.CenterRectOnY(reusedRect, innerRectX + 35f, 128f, 20f);
            if (ShowDetail)
            {
                GUI.DrawTexture(reusedRect, showDetailButton_Down);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(shoeDetailTextRect, "OARO_TaskWin_ShowDetail".Translate());
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonInvisible(reusedRect, doMouseoverSound: true))
                {
                    Parent.OnShowDrawerDetailChanged(this);
                    OARO_WindowUtility.ResetText();
                    return inRect.yMax;
                }
                else
                {
                    return DrawDetail(new Vector2(positionX, inRect.yMax));
                }
            }
            else
            {
                GUI.DrawTexture(reusedRect, showDetailButton);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(shoeDetailTextRect, "OARO_TaskWin_ShowDetail".Translate());
                Text.Anchor = TextAnchor.MiddleCenter;
                if (Widgets.ButtonInvisible(reusedRect, doMouseoverSound: true))
                {
                    Parent.OnShowDrawerDetailChanged(this);
                }
                OARO_WindowUtility.ResetText();
                return inRect.yMax;
            }
        }

        private void DrawNoramlTask(Rect inRect)
        {
            float inRectX = inRect.xMin;
            float inRectY = inRect.yMin;

            Rect reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 325f, 40f, 20f);
            Widgets.Label(reusedRect, Branch.Potency.ToString("F0"));

            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 420f, 24f, 24f);
            OARO_WindowUtility.DrawBranchStateIcon(reusedRect, Branch, expand: false);

            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX + 525f, inRectY + 4f, 100f, 48f);
            Widgets.Label(reusedRect.TopHalf(), "OARO_AutoStartTaskChance".Translate());

            if (Branch.CurWorkState == Branch.WorkStateType.Idle)
            {
                Widgets.Label(reusedRect.BottomHalf(), Branch.TaskHandler.AutoStartTaskChance.ToStringPercent());
            }
            else
            {
                Widgets.Label(reusedRect.BottomHalf(), Branch.CurWorkStateDesc);
            }

            reusedRect = new(inRectX + 495f, inRectY + 24f, 25f, 20f);
            OARO_WindowUtility.DrawKnightChivalryIcon(reusedRect, Branch.TaskHandler.FocusedTaskChivalry, primary: Branch.TaskHandler.FocusedTaskChivalry == ProtogenicChivalry);
        }

        private void DrawJointPatrol(Rect inRect)
        {
            float inRectX = inRect.xMin;

            Rect reusedRect = new(inRectX + 285f, inRect.y - 2f, 348f, 50f);
            KnightChivalryDef primaryChivalry = Branch.MedalHandler.PrimaryChivalry;
            JointPatrolProperties jointPatrolProp = primaryChivalry?.jointPatrol;
            if (jointPatrolProp is not null)
            {
                GUI.DrawTexture(reusedRect, jointPatrolProp.entryBackgroundTexture.Texture);
                Material tintMat = OARO_WindowUtility.GetTintMaterial(primaryChivalry.color, IconLibrary.JointPatrolEntryShadeMask);
                GenUI.DrawTextureWithMaterial(reusedRect, IconLibrary.JointPatrolEntryShadeTex, tintMat);
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 295f, 128f, 32f);
            Widgets.Label(reusedRect, JointBranchRecord.Value.TaskPotency.Value.ToString("F0"));

            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 500f, 128f, 32f);
            Text.Font = GameFont.Small;
            Widgets.LabelScrollable(reusedRect, BackTeamKnightsLabels.Value, ref scrollPosition_BackTeamKnights);
        }

        private float DrawDetail(Vector2 position)
        {
            Rect inRect = new(position.x, position.y, Width, DetailRectHeight);
            Rect innerRect = OARO_WindowUtility.CenterRectOnX(inRect, position.y, 635f, DetailRectHeight);
            GUI.DrawTexture(innerRect, taskEntryBottomBackground);
            float innerRectX = innerRect.xMin;
            float innerRectY = innerRect.yMin;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect reusedRect = new(innerRectX + 30f, innerRectY + 12f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_CrewCountAndSupply".Translate());

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_PublicSecurityState".Translate());
            TooltipHandler.TipRegion(reusedRect, () => "OARO_TaskWin_PublicSecurityStateTip".Translate(), uniqueId: 44673289);

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_TaskRisk".Translate());
            TooltipHandler.TipRegion(reusedRect, () => "OARO_TaskWin_TaskRiskTip".Translate(), uniqueId: 24850234);

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_ExpectedTaskRevenue".Translate());

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_CurState".Translate());

            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(innerRectX + 30f, innerRectY + 12f, 300f, 20f);
            Widgets.Label(reusedRect, $"{Branch.Squad.AllCrewCountInt}/{CrewCeiling.Value} | {Branch.Supply.ToStringPercent()}");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, Branch.PopulationHandler.PublicSecurityLabel + $" ({Branch.PopulationHandler.PublicSecurity.ToStringPercent()})");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            if (Branch.TaskHandler.CurTask?.Def.hasRisk ?? false)
            {
                Widgets.Label(reusedRect, Branch.TaskHandler.CurTask.TaskRisk().ToStringPercent());
            }
            else
            {
                Widgets.Label(reusedRect, "--%");
            }

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, ExpectedTaskRevenue.Value);

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, Branch.CurWorkStateDesc);

            reusedRect = new(innerRectX, innerRectY + 211f, 357f, 87f);
            DrawMedals(reusedRect);

            float rightRectX = innerRectX + 360f;

            float entryX = rightRectX;
            float entryY = innerRectY;
            float entryWidth = 137f;
            float entryHeight = 32f;
            int column = 0;
            Rect entryRect;

            KnightChivalryDef focusedTaskChivalry = Branch.TaskHandler.FocusedTaskChivalry;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (KnightChivalryDef taskChivalry in OrderDefDatabase.JointPatrolChivalries)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                if ((++column) >= 2)
                {
                    entryX = rightRectX;
                    entryY += entryHeight;
                    column = 0;
                }
                else
                {
                    entryX += entryWidth;
                }
                bool isActive = focusedTaskChivalry.IsSameDefNonNullable(taskChivalry);
                AcceptanceReport acceptance = isActive ? false : ChangeFocusedTaskTypeAcceptance.Value;
                if (DrawTaskTypeButton(entryRect, taskChivalry, isActive: isActive, acceptance: acceptance))
                {
                    Branch.TaskHandler.FocusedTaskChivalry = taskChivalry;
                    ChangeFocusedTaskTypeAcceptance.MarkDirty();
                }
                if (isActive)
                {
                    Widgets.DrawBox(entryRect, lineTexture: Branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);
                }
            }

            entryX = rightRectX + 18f;
            entryY = innerRectY + 88f;
            entryWidth = 79f;
            entryHeight = 24f;
            RadicalismDegree curRadicalismDegree = Branch.TaskHandler.CurRadicalismDegree;
            foreach (RadicalismDegree radicalismDegree in EnumArraryLibrary.RadicalismDegreeArr)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryX += entryWidth;

                if (OARO_WindowUtility.TextButtonImageDisableable(
                    butRect: entryRect,
                    label: $"OARO_TaskRadicalismDegree_{radicalismDegree}".Translate(),
                    acceptance: curRadicalismDegree == radicalismDegree ? false : ChangeRadicalismDegreeAcceptance.Value,
                    baseTex: radicalismDegreeButton,
                    downTex: radicalismDegreeButton_Down,
                    doMouseoverSound: true,
                    tooltip: $"OARO_TaskRadicalismDegreeTip_{radicalismDegree}".Translate()))
                {
                    Branch.TaskHandler.CurRadicalismDegree = radicalismDegree;
                    ChangeRadicalismDegreeAcceptance.MarkDirty();
                }
                if (curRadicalismDegree == radicalismDegree)
                {
                    Widgets.DrawBox(entryRect, lineTexture: Branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);
                }
            }

            reusedRect = new(rightRectX, innerRectY + 123f, 276f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_Interaction".Translate());

            reusedRect = new(rightRectX, innerRectY + 148f, 137f, 24f);
            OARO_WindowUtility.DrawBranchInteractionButton(
                butRect: reusedRect,
                def: BranchInteractionDefOf.OARO_RequestCombatReadiness,
                parms: new BranchInteractionParms(Branch, Map),
                cachedAcceptance: InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_RequestCombatReadiness, fallback: false),
                baseTex: combatReadinessButton,
                downTex: combatReadinessButton_Down,
                doMouseoverSound: true);

            reusedRect.yMax += 24f;
            reusedRect.yMin = reusedRect.yMax - 24f;
            OARO_WindowUtility.DrawBranchInteractionButton(
                butRect: reusedRect,
                def: BranchInteractionDefOf.OARO_MapRecommendationToKnight,
                parms: new BranchInteractionParms(Branch, Map),
                cachedAcceptance: InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_MapRecommendationToKnight, fallback: false),
                baseTex: supplementPersonnelButton,
                downTex: supplementPersonnelButton_Down,
                doMouseoverSound: true);

            reusedRect.yMax += 24f;
            reusedRect.yMin = reusedRect.yMax - 24f;
            OARO_WindowUtility.DrawBranchInteractionButton(
                butRect: reusedRect,
                def: BranchInteractionDefOf.OARO_MapSilverToSupply,
                parms: new BranchInteractionParms(Branch, Map),
                cachedAcceptance: InteractionAcceptances.Value.GetWithFallback(BranchInteractionDefOf.OARO_MapSilverToSupply, fallback: false),
                baseTex: supplementButton,
                downTex: supplementButton_Down,

                doMouseoverSound: true);

            reusedRect.yMax += 24f;
            reusedRect.yMin = reusedRect.yMax - 24f;
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_TaskWin_OpenBranchWin".Translate(), patrolInteractionButton, patrolInteractionButton_Down, doMouseoverSound: true))
            {
                Window_Branch branchWin = new(Branch, caravan: null, Map);
                Find.WindowStack.Add(branchWin);
                Parent.Close();
                return inRect.yMax;
            }

            entryX = reusedRect.xMax;
            entryY = innerRectY + 148f;
            entryWidth = 137f;
            entryHeight = 24f;
            foreach (KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport> interactionAcceptance in PatrolInteractionAcceptances.Value)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if (OARO_WindowUtility.TextButtonImageDisableable(
                    butRect: entryRect,
                    label: $"OARO_PatrolInteractionType_{interactionAcceptance.Key}".Translate(),
                    acceptance: interactionAcceptance.Value,
                    baseTex: patrolInteractionButton,
                    downTex: patrolInteractionButton_Down,
                    doMouseoverSound: true,
                    tooltip: $"OARO_PatrolInteractionTypeTip_{interactionAcceptance.Key}".Translate()))
                {
                    JointPatrolManager.TryActiveParticipantInteraction(JointBranchRecord.Value, interactionAcceptance.Key, Map);
                }
            }

            reusedRect = new(rightRectX, innerRect.yMax - 32f, 274f, 32f);

            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: JointBranchRecord is null ? "OARO_TaskWin_JoinJointPatrol".Translate() : "OARO_TaskWin_QuitJointPatrol".Translate(),
                acceptance: ChangeJointPatrolStateAcceptance.Value,
                baseTex: joinJointPatrolButton,
                downTex: joinJointPatrolButton_Down,
                doMouseoverSound: true))
            {
                ChangeJointPatrolState();
            }
            OARO_WindowUtility.ResetText();
            return inRect.yMax;
        }

        private void ChangeJointPatrolState()
        {
            if (JointPatrolManager.IsParticipant(Branch))
            {
                JointPatrolManager.ChangeParticipant(toAdd: null, toRemove: [Branch]);
            }
            else
            {
                JointPatrolManager.ChangeParticipant(toAdd: [Branch], toRemove: null);
            }

            JointBranchRecord.MarkDirty();
            ChangeJointPatrolStateAcceptance.MarkDirty();
            ExpectedTaskRevenue.MarkDirty();
            BackTeamKnightsLabels.MarkDirty();

            Parent.TotalJointPatrolKnightCount.MarkDirty();
        }

        private void DrawMedals(Rect inRect)
        {
            Rect textRect = inRect;
            textRect.width /= 2;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(textRect, "OARO_TaskWin_BranchMedals".Translate());

            Rect medalsOutRect = inRect;
            medalsOutRect.xMin = textRect.xMax + 10f;
            medalsOutRect.xMax -= 10f;
            medalsOutRect.yMin += 8f;
            medalsOutRect.yMax -= 8f;

            float entryX = medalsOutRect.xMin;
            float entryY = medalsOutRect.yMin;
            float entryWidth = 85f;
            float entryHeight = 35f;
            int column = 0;
            Rect medalsViewRect = medalsOutRect;
            medalsViewRect.height = (Branch.MedalHandler.MedalRecords.Count / 2 + 1) * entryHeight;
            Widgets.BeginScrollView(medalsOutRect, ref scrollPosition_Medals, medalsViewRect, showScrollbars: false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            KnightChivalryDef primaryMedal = Branch.MedalHandler.PrimaryChivalry;
            foreach (KeyValuePair<KnightChivalryDef, BranchMedalRecord> medalRecord in Branch.MedalHandler.MedalRecords)
            {
                KnightChivalryDef medalChivalry = medalRecord.Key;
                if (medalChivalry.medal is null)
                    continue;
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);

                TooltipHandler.TipRegion(entryRect, () => medalChivalry.medal.effectDescription ?? string.Empty, uniqueId: 21194707);
                Rect reusedRect = OARO_WindowUtility.CenterRectOnY(entryRect, entryX + 5f, 32f, 28f);
                GUI.DrawTexture(
                    position: reusedRect,
                    image: primaryMedal.IsSameDefNonNullable(medalChivalry) ? medalChivalry.primaryIcon.Texture : medalChivalry.icon.Texture,
                    scaleMode: ScaleMode.ScaleToFit);

                reusedRect = OARO_WindowUtility.CenterRectOnY(entryRect, entryX + 45f, 40f, 20f);
                Widgets.Label(reusedRect, $"× {medalRecord.Value.Count}");

                if ((++column) >= 2)
                {
                    entryX = medalsOutRect.xMin;
                    entryY += entryHeight;
                    column = 0;
                }
                else
                {
                    entryX += entryWidth;
                }
            }

            Widgets.EndScrollView();
            OARO_WindowUtility.ResetText();
        }

        private JointBranchRecord RefreshJointBranchRecord()
        {
            if (JointPatrolManager.CurState == PatrolState.Invalid)
            {
                return null;
            }
            else
            {
                JointPatrolManager.TryGetJointBranchRecord(Branch, out JointBranchRecord record);
                return record;
            }
        }

        private string RefreshExpectedTaskRevenue()
        {
            if (Branch.TaskHandler.HasTask)
            {
                return Branch.TaskHandler.CurTask.ExpectedRevenue();
            }

            if (JointBranchRecord is null)
            {
                return "OARO_BranchOnJointPatrolNow".Translate();
            }

            return "--";
        }

        private string RefreshBackTeamKnightsLabels()
        {
            if (JointBranchRecord is null)
            {
                return "OARO_JointPatrol_NotParticipantIn".Translate();
            }

            if (JointPatrolManager.CurState != PatrolState.Ongoing)
            {
                return "OARO_JointPatrol_NotInOngoingStage".Translate();
            }

            List<Thing> pawns = JointPatrolManager.ParticipatingResidentKnights.Where(r => r.Branch == Branch).Select(r => r.Pawn).Cast<Thing>().ToList();
            if (pawns.NullOrEmpty())
            {
                return "OARO_TaskWin_NoKnightBackTeam".Translate();
            }
            else
            {
                return GenLabel.ThingsLabel(pawns);
            }
        }

        private AcceptanceReport RefreshChangeJointPatrolState(bool resultOnly)
        {
            if (JointPatrolManager.IsParticipant(Branch))
            {
                return Branch.CanQuitJointPatrol(resultOnly: resultOnly);
            }
            else
            {
                return Branch.CanParticipateInJointPatrol(resultOnly: resultOnly);
            }
        }

        private Dictionary<BranchInteractionDef, AcceptanceReport> RefreshInteractionAcceptances()
        {
            Dictionary<BranchInteractionDef, AcceptanceReport> pairs = [];
            foreach (BranchInteractionDef def in TaskNeedBranchInteractions)
            {
                AcceptanceReport acceptance = false;
                try
                {
                    BranchInteractionParms parms = new(Branch, Map);
                    acceptance = def.Worker.CanUseInteraction(parms, resultOnly: false);
                }
                catch (Exception ex)
                {
                    acceptance = false;
                    ModUtility.LogExceptionError(ex,
                        errorDesc: $"get {nameof(AcceptanceReport)} of {nameof(BranchInteractionDef)}",
                        typeName: nameof(BranchTaskEntryDrawer),
                        methodName: nameof(RefreshInteractionAcceptances),
                        needStackTrace: true);
                }

                pairs.Add(def, acceptance);
            }
            return pairs;
        }

        private List<KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>> RefreshPatrolInteractionAcceptances()
        {
            List<KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>> acceptances = [];

            JointPatrolManager jointPatrolManager = JointPatrolManager;
            JointBranchRecord.MarkDirty();
            JointBranchRecord record = JointBranchRecord.Value;
            bool onJointPatrol = record is not null;
            foreach (JointBranchRecord.PatrolInteractionType interactionType in EnumArraryLibrary.AvailablePatrolInteractionTypeArr)
            {
                AcceptanceReport acceptance = false;
                try
                {
                    if (onJointPatrol)
                    {
                        acceptance = jointPatrolManager.CanActiveParticipantInteraction(record, interactionType, Map, resultOnly: false);
                    }
                    else
                    {
                        acceptance = "OARO_JointPatrol_NotParticipantIn".Translate();
                    }
                }
                catch (Exception ex)
                {
                    acceptance = false;
                    ModUtility.LogExceptionError(ex,
                        errorDesc: $"get {nameof(AcceptanceReport)} of {interactionType}",
                        typeName: nameof(BranchTaskEntryDrawer),
                        methodName: nameof(RefreshPatrolInteractionAcceptances),
                        needStackTrace: true);
                }
                acceptances.Add(new KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>(interactionType, acceptance));
            }
            return acceptances;
        }

        private void PostApplyBranchInteraction(BranchInteractionDef def, BranchInteractionParms parms, bool succeeded)
        {
            Parent.MapRecommendationCount.MarkDirty();
            InteractionAcceptances.MarkDirty();
        }


        private static bool DrawTaskTypeButton(Rect inRect, KnightChivalryDef taskChivalry, bool isActive, AcceptanceReport acceptance)
        {

            bool result = OARO_WindowUtility.TextButtonImageDisableable(
                butRect: inRect,
                label: taskChivalry.jointPatrol.TaskLabelCap,
                acceptance: acceptance,
                baseTex: taskTypeButton_General,
                downTex: taskTypeButton_General_Down,
                doMouseoverSound: true,
                tooltip: "OARO_JointPatrolTaskTypeTip".Translate());

            Rect iconRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.xMin + 8f, 30f, 24f);
            GUI.DrawTexture(iconRect, isActive ? null : null);
            if (isActive)
            {
                GUI.DrawTexture(iconRect, taskChivalry.icon.Texture);
            }
            else
            {
                GenUI.DrawTextureWithMaterial(iconRect, taskChivalry.icon.Texture, OARO_WindowUtility.BlackWhiteMat);
            }
            return result;
        }

        private static readonly Texture2D taskEntryUpBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskEntryUpBackground");
        private static readonly Texture2D taskEntryBottomBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskEntryBottomBackground");

        private static readonly Texture2D showDetailButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_ShowDetailButton");
        private static readonly Texture2D showDetailButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_ShowDetailButton_Down");

        private static readonly Texture2D radicalismDegreeButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_RadicalismDegreeButton");
        private static readonly Texture2D radicalismDegreeButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_RadicalismDegreeButton_Down");

        private static readonly Texture2D combatReadinessButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_CombatReadinessButton");
        private static readonly Texture2D combatReadinessButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_CombatReadinessButton_Down");
        private static readonly Texture2D supplementPersonnelButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_SupplementPersonnelButton");
        private static readonly Texture2D supplementPersonnelButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_SupplementPersonnelButton_Down");
        private static readonly Texture2D supplementButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_SupplementButton");
        private static readonly Texture2D supplementButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_SupplementButton_Down");

        private static readonly Texture2D patrolInteractionButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_PatrolInteractionButton");
        private static readonly Texture2D patrolInteractionButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_PatrolInteractionButton_Down");

        private static readonly Texture2D joinJointPatrolButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JoinJointPatrolButton");
        private static readonly Texture2D joinJointPatrolButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JoinJointPatrolButton_Down");

        private static readonly Texture2D taskTypeButton_General = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_General");
        private static readonly Texture2D taskTypeButton_General_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_General_Down");

    }
}