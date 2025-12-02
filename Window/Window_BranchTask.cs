using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchTaskHandler;

namespace OberoniaAurea.RatkinOrder;

internal class Window_BranchTask : OrderWindowBase
{
    public override Vector2 InitialSize => new(1339f, 909f);

    private RatkinOrder RatkinOrder { get; }
    private Map Map { get; }
    private List<BranchTaskEntryDrawer> BranchTaskEntryDrawers { get; }
    private BranchTaskEntryDrawer ShowDetailDrawer { get; set; }

    public Window_BranchTask(RatkinOrder ratkinOrder, Map map)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        Map = map ?? throw new ArgumentNullException(nameof(map));

        BranchTaskEntryDrawers = new(RatkinOrder.BranchManager.AllBranches.Count);
        foreach (Branch branch in RatkinOrder.BranchManager.AllBranches)
        {
            BranchTaskEntryDrawers.Add(new BranchTaskEntryDrawer(this, branch, map));
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        BranchTaskEntryDrawers.Clear();
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);
        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(innerRectX, innerRectY + 36f, innerRect.width, 32f);
        Widgets.Label(reusedRect, "");

        reusedRect.yMax += 20f;
        reusedRect.yMin += 20f;
        reusedRect.height = 20f;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, RatkinOrder.NameColored);

        reusedRect = new(innerRectX + 65f, innerRectY + 180f, 655f, 647f);
        DrawLeftRect(reusedRect);
    }

    private void DrawLeftRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftMainBackground);
        Rect innerRect = inRect.ContractedBy(2f);
        Rect titleRect = innerRect;
        titleRect.height = 26f;

        Rect outRect = innerRect;
        outRect.yMin = titleRect.yMax + 2f;

        Rect viewRect = outRect;
        viewRect.width = 635f;

        float entryX = viewRect.xMin - 2f;
        float entryY = viewRect.yMin - 2f;
        viewRect.height = BranchTaskEntryDrawers.Count * BranchTaskEntryDrawer.UpRectHeight + BranchTaskEntryDrawer.DetailRectHeight + 10f;
        foreach (BranchTaskEntryDrawer drawer in BranchTaskEntryDrawers)
        {
            Vector2 entryPos = new(entryX, entryY);
            entryY = drawer.DrawTaskEntry(entryPos);
        }

    }

    private void OnShowDrawerDetailChanged(BranchTaskEntryDrawer drawer)
    {
        if (drawer is null)
        {
            return;
        }

        if (ShowDetailDrawer is not null)
        {
            ShowDetailDrawer.ShowDetail = false;
            ShowDetailDrawer.ClearCache();
        }

        if (ShowDetailDrawer == drawer)
        {
            ShowDetailDrawer = null;
        }
        else
        {
            ShowDetailDrawer = drawer;
            drawer.ShowDetail = true;
        }
    }


    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_MainBackground");
    private static readonly Texture2D leftMainBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_LeftMainBackground");

    private class BranchTaskEntryDrawer
    {
        private const float Width = 637f;
        public const float UpRectHeight = 82f;
        public const float DetailRectHeight = 301f;

        private Vector2 scrollPosition_Medals;

        private Window_BranchTask Parent { get; }
        private Branch Branch { get; }
        private Map Map { get; }
        private LazyMutable<JointBranchRecord> JointBranchRecord { get; }
        private JointPatrolManager JointPatrolManager => Branch.RatkinOrder.JointPatrolManager;

        public bool ShowDetail { get; set; }

        private LazyMutable<AcceptanceReport> ChangeRadicalismDegreeAcceptance { get; }
        private LazyMutable<AcceptanceReport> ChangeFocusedTaskTypeAcceptance { get; }
        private LazyMutable<AcceptanceReport> CombatReadinessAcceptance { get; }
        private LazyMutable<List<KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>>> PatrolInteractionAcceptances { get; }
        private LazyMutable<List<Pawn>> BackTeamKnights { get; }
        private Lazy<int> CrewCeiling { get; }

        public BranchTaskEntryDrawer(Window_BranchTask parent, Branch branch, Map map)
        {
            Parent = parent;
            Branch = branch;
            Map = map;

            JointBranchRecord = new(refreshFunc: RefreshJointBranchRecord);
            CrewCeiling = new(valueFactory: () => (int)(Branch.Squad.MemberCeiling + Branch.Squad.CommanderCeiling));
            ChangeRadicalismDegreeAcceptance = new(refreshFunc: () => BranchUtility.CanChangeRadicalismDegree(branch, resultOnly: false));
            ChangeFocusedTaskTypeAcceptance = new(refreshFunc: () => BranchUtility.CanChangeFocusedTaskType(branch, resultOnly: false));
            CombatReadinessAcceptance = new(refreshFunc: () => Branch.TaskHandler.CanSwitchToTask(BranchTaskDefOf.OARO_CombatReadiness));
            PatrolInteractionAcceptances = new(refreshFunc: RecachePatrolInteractionAcceptances);
            BackTeamKnights = new(refreshFunc: () => JointPatrolManager.ParticipatingResidentKnights.Where(r => r.Branch == Branch).Select(r => r.Knight).ToList());
        }

        public void ClearCache()
        {
            ShowDetail = false;
            JointBranchRecord.Reset();
            ChangeRadicalismDegreeAcceptance.Reset();
            ChangeFocusedTaskTypeAcceptance.Reset();
            CombatReadinessAcceptance.Reset();
            PatrolInteractionAcceptances.Reset();
        }

        private JointBranchRecord RefreshJointBranchRecord()
        {
            if (JointPatrolManager.CurState == JointPatrolManager.PatrolState.Invalid)
            {
                return null;
            }
            else
            {
                JointPatrolManager.TryGetJointBranchRecord(Branch, out JointBranchRecord record);
                return record;
            }
        }

        public float DrawTaskEntry(Vector2 position)
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
            if (Branch.TaskHandler.CurTask?.Def.hasRisk ?? false)
            {
                Widgets.Label(reusedRect, Branch.TaskHandler.CurTask.TaskRisk(Branch).ToStringPercent());
            }
            else
            {
                Widgets.Label(reusedRect, "--%");
            }


            if (JointBranchRecord.Value is not null)
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
            Widgets.Label(reusedRect, Branch.Potency.ToStringPercent("F0"));

            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 420f, 24f, 24f);
            OARO_WindowUtility.DrawBranchStateIcon(reusedRect, Branch, expand: false);

            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(inRect.xMax - (10f + 100f), inRectY + 4f, 100f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_AutoTaskChance".Translate());

            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect.yMax += 20f;
            reusedRect.yMin += 20f;
            if (Branch.IsIdleNow)
            {
                Widgets.Label(reusedRect, Branch.TaskHandler.AutoStartTaskChance.ToStringPercent());
            }
            else
            {
                Widgets.Label(reusedRect, Branch.CurWorkState);
            }

            reusedRect = new(inRectX + 495f, inRectY + 24f, 25f, 20f);
            OARO_WindowUtility.DrawBranchTaskTypeIcon(reusedRect, Branch.TaskHandler.FocusedTaskType, expand: false);
        }

        private void DrawJointPatrol(Rect inRect)
        {

            float inRectX = inRect.xMin;

            Rect reusedRect = new(inRectX + 285f, inRect.y - 2f, 348f, 52f);
            //

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 350f, 45f, 32f);
            Widgets.Label(reusedRect, JointBranchRecord.Value.TaskPotency.Value.ToString("F0"));

            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 350f, 45f, 32f);
            if (BackTeamKnights.Value.NullOrEmpty())
            {
                Widgets.Label(reusedRect, "OARO_TaskWin_NoKnightBackTeam".Translate());
            }
            else
            {
                Widgets.TextArea(reusedRect, GenLabel.ThingsLabel(BackTeamKnights.Value.Cast<Thing>()), readOnly: true);
            }
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

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_TaskRisk".Translate());

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_ExpectedTaskRevenue".Translate());

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_CurState".Translate());

            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(innerRectX + 30f, innerRectY + 12f, 300f, 20f);
            Widgets.Label(reusedRect, $"{Branch.Squad.AllCrewCountInt}/{CrewCeiling.Value} | {Branch.Supply.ToStringPercent()}");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, Branch.PopulationHandler.PublicSecurityLabel + $"( {Branch.PopulationHandler.PublicSecurity.ToStringPercent()} )");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, Branch.CurWorkState);

            float entryX = innerRectX + 210f;
            float entryY = innerRectY;
            float entryWidth = 137f;
            float entryHeight = 32f;
            int column = 0;
            Rect entryRect;

            BranchTaskType focusedTaskType = Branch.TaskHandler.FocusedTaskType;
            foreach (BranchTaskType taskType in EnumArraryLibrary.AvailableBranchTaskTypeArr)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                if ((++column) >= 2)
                {
                    entryX = innerRectX + 210f;
                    entryY += entryHeight;
                }
                else
                {
                    entryX += entryWidth;
                }

                if (OARO_WindowUtility.TextButtonImageDisableable(
                    butRect: entryRect,
                    label: $"OARO_TaskType_{taskType}".Translate(),
                    acceptance: focusedTaskType == taskType ? false : ChangeFocusedTaskTypeAcceptance.Value,
                    baseTex: GetTaskTypeButtonTex(taskType, downed: false, disable: taskType != focusedTaskType),
                    downTex: GetTaskTypeButtonTex(taskType, downed: true, disable: taskType != focusedTaskType),
                    doMouseoverSound: true))
                {
                    Branch.TaskHandler.FocusedTaskType = taskType;
                    ChangeFocusedTaskTypeAcceptance.MarkDirty();
                }
                if (focusedTaskType == taskType)
                {
                    Widgets.DrawBox(entryRect, lineTexture: Branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);
                }
            }

            entryX = innerRectX + 377f;
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
                    doMouseoverSound: true))
                {
                    Branch.TaskHandler.CurRadicalismDegree = radicalismDegree;
                    ChangeRadicalismDegreeAcceptance.MarkDirty();
                }
                if (curRadicalismDegree == radicalismDegree)
                {
                    Widgets.DrawBox(entryRect, lineTexture: Branch.HonorDef?.HonorColorTex ?? BaseContent.WhiteTex);
                }
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(innerRectX + 360f, innerRectY + 123f, 276f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_Interaction".Translate());

            reusedRect = new(innerRectX + 360f, innerRectY + 148f, 137f, 24f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "OARO_TaskWin_RequireCombatReadiness".Translate(),
                acceptance: CombatReadinessAcceptance.Value,
                baseTex: combatReadinessButton,
                downTex: combatReadinessButton_Down,
                doMouseoverSound: true))
            {
                AcceptanceReport acceptanceReport = Branch.TaskHandler.CanSwitchToTask(BranchTaskDefOf.OARO_CombatReadiness, resultOnly: false);
                if (acceptanceReport)
                {
                    Branch.TaskHandler.TrySwitchToTask(BranchTaskDefOf.OARO_CombatReadiness, endCurIfCantSwitch: false);
                }
                else
                {
                    Messages.Message(
                        text: "OARO_CanNotSwithToBranchTaskWithReason".Translate(Branch.Name.Named(KeyLibrary_FormatArgName.BranchName), BranchTaskDefOf.OARO_CombatReadiness.Named("TASK"), acceptanceReport.Reason.Named("Reason")),
                        def: MessageTypeDefOf.RejectInput,
                        historical: false);
                }
                CombatReadinessAcceptance.MarkDirty();
            }
            reusedRect.yMax += 24f;
            reusedRect.yMin += 24f;
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_TaskWin_SupplementPersonnel".Translate(), supplementPersonnelButton, supplementPersonnelButton_Down, doMouseoverSound: true))
            {

            }
            reusedRect.yMax += 24f;
            reusedRect.yMin += 24f;
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_TaskWin_SupplementPersonnel".Translate(), supplementButton, supplementButton_Down, doMouseoverSound: true))
            {

            }
            reusedRect.yMax += 24f;
            reusedRect.yMin += 24f;
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_TaskWin_OpenBranchWin".Translate(), patrolInteractionButton, patrolInteractionButton_Down, doMouseoverSound: true))
            {
                Window_Branch branchWin = new(Branch, caravan: null, Map);
                Find.WindowStack.Add(branchWin);
                Parent.Close();
                return inRect.yMax;
            }

            entryX = reusedRect.xMax + 377f;
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
                    doMouseoverSound: true))
                {
                    JointPatrolManager.TryActiveParticipantInteraction(JointBranchRecord.Value, interactionAcceptance.Key, Map);
                }
            }

            reusedRect = new(innerRectX + 360f, innerRect.yMax - 32f, 274f, 32f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "OARO_TaskWin_JoinJointPatrol".Translate(),
                acceptance: Branch.CanParticipateInJointPatrol(resultOnly: false),
                baseTex: joinJointPatrolButton,
                downTex: joinJointPatrolButton_Down,
                doMouseoverSound: true))
            {
                JointPatrolManager.ChangeParticipant(toAdd: [Branch], toRemove: null);
            }

            reusedRect = new(innerRectX, innerRectY + 211f, 357f, 87f);
            DrawMedals(reusedRect);

            OARO_WindowUtility.ResetText();
            return inRect.yMax;
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
            foreach (var medalRecord in Branch.MedalHandler.MedalRecords)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                Rect reusedRect = OARO_WindowUtility.CenterRectOnY(entryRect, entryX + 5f, 32f, 28f);
                GUI.DrawTexture(reusedRect, medalRecord.Key.IconTexture, ScaleMode.ScaleToFit);

                reusedRect = OARO_WindowUtility.CenterRectOnY(entryRect, entryX + 45f, 40f, 20f);
                Widgets.Label(reusedRect, $"× {medalRecord.Value.Count}");

                if ((++column) >= 2)
                {
                    entryX = medalsOutRect.xMin;
                    entryY += entryHeight;
                }
                else
                {
                    entryX += entryWidth;
                }
            }

            Widgets.EndScrollView();
            OARO_WindowUtility.ResetText();
        }


        private List<KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>> RecachePatrolInteractionAcceptances()
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
                        acceptance = "OARO_NotOnJointPatrol".Translate();
                    }
                }
                catch (Exception ex)
                {
                    acceptance = false;
                    ModUtility.LogExceptionError(ex,
                        errorDesc: $"get AcceptanceReport of {interactionType}",
                        typeName: nameof(BranchTaskEntryDrawer),
                        methodName: nameof(RecachePatrolInteractionAcceptances),
                        needStackTrace: true);
                }
                acceptances.Add(new KeyValuePair<JointBranchRecord.PatrolInteractionType, AcceptanceReport>(interactionType, acceptance));
            }
            return acceptances;
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

        private static Texture2D GetTaskTypeButtonTex(BranchTaskType taskType, bool downed, bool disable)
        {
            switch (taskType)
            {
                case BranchTaskType.CrimeFighting:
                    {
                        if (downed)
                        {
                            return disable ? taskTypeButton_CrimeFightingD_Down : taskTypeButton_CrimeFighting_Down;
                        }
                        else
                        {
                            return disable ? taskTypeButton_CrimeFightingD : taskTypeButton_CrimeFighting;
                        }
                    }
                case BranchTaskType.StabilityMaintenance:
                    {
                        if (downed)
                        {
                            return disable ? taskTypeButton_StabilityMaintenanceD_Down : taskTypeButton_StabilityMaintenance_Down;
                        }
                        else
                        {
                            return disable ? taskTypeButton_StabilityMaintenanceD : taskTypeButton_StabilityMaintenance;
                        }
                    }
                case BranchTaskType.Assistance:
                    {
                        if (downed)
                        {
                            return disable ? taskTypeButton_AssistanceD_Down : taskTypeButton_Assistance_Down;
                        }
                        else
                        {
                            return disable ? taskTypeButton_AssistanceD : taskTypeButton_Assistance;
                        }
                    }
                case BranchTaskType.Supervision:
                    {
                        if (downed)
                        {
                            return disable ? taskTypeButton_SupervisionD_Down : taskTypeButton_Supervision_Down;
                        }
                        else
                        {
                            return disable ? taskTypeButton_SupervisionD : taskTypeButton_Supervision;
                        }
                    }
                default: return downed ? taskTypeButton_General_Down : taskTypeButton_General;
            }
        }

        private static readonly Texture2D taskTypeButton_General = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_General");
        private static readonly Texture2D taskTypeButton_General_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_General_Down");

        private static readonly Texture2D taskTypeButton_CrimeFighting = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_CrimeFighting");
        private static readonly Texture2D taskTypeButton_CrimeFighting_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_CrimeFighting_Down");
        private static readonly Texture2D taskTypeButton_CrimeFightingD = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_CrimeFightingD");
        private static readonly Texture2D taskTypeButton_CrimeFightingD_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_CrimeFightingD_Down");

        private static readonly Texture2D taskTypeButton_StabilityMaintenance = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_StabilityMaintenance");
        private static readonly Texture2D taskTypeButton_StabilityMaintenance_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_StabilityMaintenance_Down");
        private static readonly Texture2D taskTypeButton_StabilityMaintenanceD = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_StabilityMaintenanceD");
        private static readonly Texture2D taskTypeButton_StabilityMaintenanceD_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_StabilityMaintenanceD_Down");

        private static readonly Texture2D taskTypeButton_Assistance = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_Assistance");
        private static readonly Texture2D taskTypeButton_Assistance_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_Assistance_Down");
        private static readonly Texture2D taskTypeButton_AssistanceD = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_AssistanceD");
        private static readonly Texture2D taskTypeButton_AssistanceD_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_AssistanceD_Down");

        private static readonly Texture2D taskTypeButton_Supervision = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_Supervision");
        private static readonly Texture2D taskTypeButton_Supervision_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_Supervision_Down");
        private static readonly Texture2D taskTypeButton_SupervisionD = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_SupervisionD");
        private static readonly Texture2D taskTypeButton_SupervisionD_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_TaskTypeButton_SupervisionD_Down");
    }
}