using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.BranchTaskHandler;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_BranchTask : OrderWindowBase
{
    private Vector2 scrollPosition_Branches;
    private Vector2 scrollPosition_JointPatrolRecordDetailStr;

    public override Vector2 InitialSize => new(1339f, 909f);

    private RatkinOrder RatkinOrder { get; }
    private JointPatrolManager JointPatrolManager { get; }
    private Map Map { get; }
    private LazyMutable<int> MapRecommendationCount { get; }
    private List<BranchTaskEntryDrawer> BranchTaskEntryDrawers { get; }
    private BranchTaskEntryDrawer ShowDetailDrawer { get; set; }

    private bool JointPatrolTab { get; set; }
    private List<TabRecord> BranchTabs { get; } = new(2);

    private bool JointPatrolStaticTab { get; set; }
    private List<TabRecord> JointPatrolTabs { get; } = new(2);

    private Lazy<float> JointPatrolNeededTaskPotency { get; }
    private Lazy<string> JointPatrolRecordDetailStr { get; }
    private int OrderResidentKnightCount { get; }
    private LazyMutable<int> TotalJointPatrolKnightCount { get; }

    public Window_BranchTask(RatkinOrder ratkinOrder, Map map)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        Map = map ?? throw new ArgumentNullException(nameof(map));

        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationOfMap(RatkinOrder, Map));
        JointPatrolManager = RatkinOrder.JointPatrolManager;
        JointPatrolNeededTaskPotency = new(valueFactory: () => JointPatrolManager.NeededTaskPotency);
        JointPatrolRecordDetailStr = new(valueFactory: JointPatrolRecordDetail);
        TotalJointPatrolKnightCount = new(refreshFunc: () => JointPatrolManager?.ParticipantsDict.Keys.Sum(b => b.Squad.AllCrewCountInt) ?? 0);

        BranchTaskEntryDrawers = new(RatkinOrder.BranchManager.AllBranches.Count);
        OrderResidentKnightCount = ResidentKnightsManager.Instance.ResidentKnights.Where(kv => kv.Value.RatkinOrder == RatkinOrder).Count();
        foreach (Branch branch in RatkinOrder.BranchManager.AllBranches)
        {
            BranchTaskEntryDrawers.Add(new BranchTaskEntryDrawer(this, branch, Map));
        }
    }

    public override void PostClose()
    {
        base.PostClose();

        if (ShowDetailDrawer is not null)
        {
            ShowDetailDrawer.ShowDetail = false;
            ShowDetailDrawer.ClearCache();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);
        Rect mainInnerRect = inRect.ContractedBy(2f);
        float mainInnerRectX = mainInnerRect.xMin;
        float mainInnerRectY = mainInnerRect.yMin;

        Rect reusedRect = new(mainInnerRect.xMax - 21f, mainInnerRect.y + 1f, 20f, 20f);
        if (Widgets.ButtonImage(reusedRect, IconLibrary.colseX, doMouseoverSound: true))
        {
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(mainInnerRectX, mainInnerRectY + 36f, mainInnerRect.width, 32f);
        Widgets.Label(reusedRect, "OARO_TaskWin_Title".Translate());

        reusedRect.yMax += 20f;
        reusedRect.yMin = reusedRect.yMax - 20f;
        reusedRect.height = 20f;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, RatkinOrder.NameColored);

        reusedRect = new(mainInnerRectX + 400f, mainInnerRectY + 80f, 155f, 20f);
        Widgets.Label(reusedRect, "OARO_TaskWin_Relationship".Translate());
        reusedRect.yMax += 20f;
        reusedRect.yMin = reusedRect.yMax - 20f;
        Widgets.Label(reusedRect, RatkinOrder.Relationship.GetLabel());

        reusedRect = new(mainInnerRectX + 805f, mainInnerRectY + 80f, 60f, 20f);
        Widgets.Label(reusedRect, "OARO_RecommendationLetter".Translate());
        reusedRect.yMax += 20f;
        reusedRect.yMin = reusedRect.yMax - 20f;
        OARO_WindowUtility.DrawRecommendationInfo(reusedRect, MapRecommendationCount.Value, 4f);

        reusedRect = new(mainInnerRectX + 65f, mainInnerRectY + 180f, 655f, 647f);
        DrawLeftRect(reusedRect);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 28f, 2f, 673f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);

        reusedRect = new(reusedRect.xMax + 28f, mainInnerRectY + 180f, 510f, 647f);
        DrawRightRect(reusedRect);

        OARO_WindowUtility.ResetText();
    }

    private void DrawLeftRect(Rect inRect)
    {
        BranchTabs.Clear();
        BranchTabs.Add(new TabRecord("OARO_TaskWin_BranchTab_All".Translate().CapitalizeFirst(), delegate
        {
            ClearShowDetailDrawer();
            JointPatrolTab = false;
        }, !JointPatrolTab));
        if (JointPatrolManager.CurState != PatrolState.Invalid)
        {
            BranchTabs.Add(new TabRecord("OARO_TaskWin_BranchTab_JointPatrol".Translate().CapitalizeFirst(), delegate
            {
                ClearShowDetailDrawer();
                JointPatrolTab = true;
            }, JointPatrolTab));
        }
        TabDrawer.DrawTabs(inRect, BranchTabs, maxTabWidth: 140f);

        GUI.DrawTexture(inRect, leftMainBackground);
        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        Rect titleRect = innerRect;
        titleRect.height = 26f;

        Rect reusedRect;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = OARO_WindowUtility.CenterRectOnY(titleRect, innerRectX + 108f, 128f, 20f);
        Widgets.Label(reusedRect, "OARO_TaskWin_BranchInfo".Translate());
        if (JointPatrolTab)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = OARO_WindowUtility.CenterRectOnY(titleRect, innerRectX + 295f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_BranchJointPatrolPotency".Translate());

            reusedRect = OARO_WindowUtility.CenterRectOnY(titleRect, innerRectX + 500f, 128f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_BranchJointPatrolKnightInfo".Translate());
        }
        else
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(titleRect, innerRectX + 305f, 100f, 20f);
            Widgets.Label(reusedRect, "OARO_BranchPotency".Translate());

            reusedRect = OARO_WindowUtility.CenterRectOnY(titleRect, innerRectX + 415f, 100f, 20f);
            Widgets.Label(reusedRect, "OARO_BranchWorkState".Translate());

            reusedRect = OARO_WindowUtility.CenterRectOnY(titleRect, innerRectX + 525f, 100f, 20f);
            Widgets.Label(reusedRect, "OARO_TaskWin_BranchTaskSummary".Translate());
        }

        reusedRect = innerRect;
        reusedRect.xMin = innerRect.xMax - 16f;
        GUI.DrawTexture(reusedRect, BaseContent.BlackTex);

        Rect outRect = innerRect;
        outRect.yMin = titleRect.yMax + 2f;

        Rect viewRect = outRect;
        viewRect.width = 635f;

        float entryX = viewRect.xMin - 2f;
        float entryY = viewRect.yMin - 2f;


        IEnumerable<BranchTaskEntryDrawer> showDrawers;
        if (JointPatrolTab)
        {
            viewRect.height = JointPatrolManager.ParticipantsDict.Count * BranchTaskEntryDrawer.UpRectHeight + BranchTaskEntryDrawer.DetailRectHeight + 10f;
            showDrawers = BranchTaskEntryDrawers.Where(d => JointPatrolManager.IsParticipant(d.Branch));
        }
        else
        {
            showDrawers = BranchTaskEntryDrawers;
            viewRect.height = BranchTaskEntryDrawers.Count * BranchTaskEntryDrawer.UpRectHeight + BranchTaskEntryDrawer.DetailRectHeight + 10f;
        }

        Widgets.BeginScrollView(outRect, ref scrollPosition_Branches, viewRect);
        foreach (BranchTaskEntryDrawer drawer in showDrawers)
        {
            Vector2 entryPos = new(entryX, entryY);
            entryY = drawer.DrawTaskEntry(entryPos, JointPatrolTab);
        }
        Widgets.EndScrollView();

        OARO_WindowUtility.ResetText();
    }

    private void DrawRightRect(Rect inRect)
    {
        Rect titleRect = new(inRect.x, inRect.y, inRect.width, 32f);
        Rect reusedRect = OARO_WindowUtility.CenterRect(titleRect, 477f, 7f);
        GUI.DrawTexture(reusedRect, horizontalDecorationLine);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;

        Rect mainRect = new(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
        if (JointPatrolManager.CurState == PatrolState.Invalid)
        {
            Widgets.Label(titleRect, "OARO_TaskWin_PreJointPatrolDesc".Translate());
            DrawRightRect_Normal(mainRect);
        }
        else
        {
            Widgets.Label(titleRect, "OARO_TaskWin_JointPatrolDesc".Translate($"OARO_JointPatrolLevel_{JointPatrolManager.PatrolLevelValue}".Translate()));
            DrawRightRect_JointPatrol(mainRect);
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawRightRect_Normal(Rect inRect)
    {
        JointPatrolTabs.Clear();

        GUI.DrawTexture(inRect, rightBrackground_Normal);
        Rect innerRect = inRect.ContractedBy(2f);



        OARO_WindowUtility.ResetText();
    }

    private void DrawRightRect_JointPatrol(Rect inRect)
    {
        Rect mainRect = inRect;
        mainRect.yMin += 45f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        JointPatrolTabs.Clear();
        JointPatrolTabs.Add(new TabRecord("OARO_TaskWin_JointPatrolTab_Target".Translate().CapitalizeFirst(), delegate
        {
            JointPatrolStaticTab = false;
        }, !JointPatrolStaticTab));

        JointPatrolTabs.Add(new TabRecord("OARO_TaskWin_JointPatrolTab_Static".Translate().CapitalizeFirst(), delegate
        {
            JointPatrolStaticTab = true;
        }, JointPatrolStaticTab));

        TabDrawer.DrawTabs(mainRect, JointPatrolTabs, maxTabWidth: 140f);

        Text.Anchor = TextAnchor.MiddleLeft;
        Rect stageTextRect = new(mainRect.xMax - 200f, mainRect.yMin - 24f, 200f, 20f);
        if (JointPatrolManager.CurState == PatrolState.Prepare)
        {
            Widgets.Label(stageTextRect, $"OARO_JointPatrolStage_{PatrolState.Prepare}".Translate());
        }
        else
        {
            Widgets.Label(stageTextRect, $"OARO_JointPatrolStage_{PatrolState.Ongoing}".Translate());
        }

        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(stageTextRect, "WaitTime".Translate(JointPatrolManager.TickToNextStage.ToStringTicksToPeriod()));

        if (JointPatrolStaticTab)
        {
            DrawJointPatrolStatic(mainRect);
        }
        else
        {
            DrawJointPatrolTarget(mainRect);
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawJointPatrolStatic(Rect inRect)
    {
        GUI.DrawTexture(inRect, jointPatrolStaticBackground);
        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;

        Rect textRect = new(innerRectX + 12f, innerRectY + 12f, innerRect.width - 24f, 448f);
        Rect reusedRect = new(textRect.x, textRect.y, textRect.width, 32f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_TaskWin_JointPatrolStatic".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(textRect.x, textRect.y + 50f, textRect.width, textRect.height - 60f);
        Widgets.LabelScrollable(reusedRect, JointPatrolRecordDetailStr.Value, ref scrollPosition_JointPatrolRecordDetailStr);

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(innerRectX + 10f, innerRect.yMax - (26f * 3f), 150f, 26f);
        Widgets.Label(reusedRect, "OARO_TaskWin_NonBackKnights".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        int nonBackKnightsCount = OrderResidentKnightCount - JointPatrolManager.ParticipatingResidentKnights.Count;
        Widgets.Label(reusedRect, nonBackKnightsCount.ToString());
        Rect reusedRectII = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 30f, 23f);
        GUI.DrawTexture(reusedRectII, ellipsisIcon);

        reusedRect = new(innerRectX + 10f, reusedRect.yMax, 150f, 26f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_TaskWin_BackKnights".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, JointPatrolManager.ParticipatingResidentKnights.Count.ToString());
        reusedRectII = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 30f, 23f);
        GUI.DrawTexture(reusedRectII, ellipsisIcon);

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = new(innerRectX + 10f, innerRect.yMax - 24f, 150f, 24f);
        Widgets.Label(reusedRect, "OARO_TaskWin_TotalJoinKnights".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, TotalJointPatrolKnightCount.Value.ToString());

        reusedRect = new(innerRect.xMax - (150f + 10f), innerRect.yMax - 24f, 150f, 24f);
        Widgets.Label(reusedRect, "OARO_TaskWin_JointPatroBranchBurden".Translate(JointPatrolManager.ParticipantsDict.Count, JointPatrolManager.BurdenCount)
                                                                       .Colorize(JointPatrolManager.ParticipantsDict.Count > JointPatrolManager.BurdenCount ? ColorLibrary.RedReadable : Color.green));

        reusedRect = new(innerRectX + 240f, innerRect.yMax - (26f + 54f), 110f, 54f);
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_TaskWin_KnightBackTeam".Translate(),
            acceptance: JointPatrolManager.CurState != PatrolState.Prepare ? "OARO_JointPatrol_NotInPrepareStage".Translate() : true,
            baseTex: jointPatrolButton,
            downTex: jointPatrolButton_Down,
            doMouseoverSound: true))
        {
            KnightBackTeam();
        }

        reusedRect = new(innerRectX + 396f, innerRect.yMax - (26f + 54f), 110f, 54f);
        string helpPolicyText = "OARO_TaskWin_HelpPolicyButton".Translate();
        helpPolicyText += ("\n" + $"OARO_JointPatrol_HelpPolicy_{JointPatrolManager.CurHelpPolicy}".Translate());
        if (OARO_WindowUtility.TextButtonImage(reusedRect, helpPolicyText, jointPatrolButton, jointPatrolButton_Down, doMouseoverSound: true))
        {
            JointPatrolManager.ChangeHelpPolicy();
        }
    }

    private void DrawJointPatrolTarget(Rect inRect)
    {
        float entryX = inRect.xMin;
        float entryY = inRect.yMin;
        float entryWidth = 510f;
        float entryHeight = 140f;

        foreach (BranchTaskType taskType in EnumArraryLibrary.JointPatrolTaskTypeArr)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            DrawJointPatrolTarget_ByType(entryRect, taskType);
        }
    }

    private void DrawJointPatrolTarget_ByType(Rect inRect, BranchTaskType taskType)
    {
        Texture2D backgroundTex = taskType switch
        {
            BranchTaskType.CrimeFighting => jointPatrolTargetBackground_CrimeFighting,
            BranchTaskType.StabilityMaintenance => jointPatrolTargetBackground_StabilityMaintenance,
            BranchTaskType.Assistance => jointPatrolTargetBackground_Assistance,
            BranchTaskType.Supervision => jointPatrolTargetBackground_Supervision,
            _ => BaseContent.BadTex
        };
        GUI.DrawTexture(inRect, backgroundTex);
        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Rect reusedRect = new(innerRectX + 55f, innerRectY + 12f, 100f, 32f);
        Widgets.Label(reusedRect, $"OARO_JointPatrolTaskType_{taskType}".Translate());

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(innerRect.xMax - 124f, innerRectY + 7f, 124f, 28f);
        Widgets.Label(reusedRect, "OARO_TaskWin_BranchJointPatrolPotency".Translate());

        reusedRect.yMax += 28f;
        reusedRect.yMin = reusedRect.yMax - 28f;
        float potencyValue = 0f;
        if (JointPatrolManager.CurState != PatrolState.Ongoing || !JointPatrolManager.TaskPotencys.Value.TryGetValue(taskType, out potencyValue))
        {
            potencyValue = 0f;
        }

        Widgets.Label(reusedRect, $"{potencyValue:F0}/{JointPatrolNeededTaskPotency.Value:F0}");

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(innerRect.xMax - 124f, innerRectY + 70f, 124f, 66f);
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.y, 80f, 66f);
        if (JointPatrolManager.CurState == PatrolState.Ongoing)
        {
            if (potencyValue < JointPatrolNeededTaskPotency.Value)
            {
                Widgets.Label(reusedRect, "OARO_TaskWin_CanNotCompleteJointPatrol".Translate().Colorize(ColorLibrary.RedReadable));
            }
            else
            {
                Widgets.Label(reusedRect, "OARO_TaskWin_CanCompleteJointPatrol".Translate().Colorize(Color.green));
            }
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_TaskWin_NotInOngoingStage".Translate());
        }
    }

    private string JointPatrolRecordDetail()
    {
        StringBuilder sb = new();
        Vector2 location = Find.WorldGrid.LongLatOf(Map.Tile);
        foreach (JointInteractionRecord record in JointPatrolManager.InteractionRecords)
        {
            sb.Append(GenDate.DateFullStringAt(GenDate.TickAbsToGame(record.TriggerTick), location));
            sb.Append(" —— ");
            sb.Append(record.RelatedBranch.NameColored);
            sb.Append(" —— ");
            sb.AppendLine(record.Label);
            sb.AppendLine(record.Description);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private void ClearShowDetailDrawer()
    {
        ShowDetailDrawer?.ClearCache();
        ShowDetailDrawer = null;
    }

    private void OnShowDrawerDetailChanged(BranchTaskEntryDrawer drawer)
    {
        if (drawer is null)
        {
            return;
        }

        if (drawer == ShowDetailDrawer)
        {
            ClearShowDetailDrawer();
        }
        else
        {
            ClearShowDetailDrawer();
            ShowDetailDrawer = drawer;
            ShowDetailDrawer.ChangeShowDetail();
        }
    }

    private void KnightBackTeam()
    {
        if (JointPatrolManager.CurState != PatrolState.Prepare)
        {
            return;
        }
        List<FloatMenuOption> menuOptions = [];
        foreach ((Pawn knight, ResidentKnightRecord record) in ResidentKnightsManager.Instance.ResidentKnights)
        {
            if (record.RatkinOrder != RatkinOrder || !record.IsValid || knight.Downed)
            {
                continue;
            }
            if (JointPatrolManager.ParticipatingResidentKnights.Contains(record))
            {
                continue;
            }
            menuOptions.Add(new(knight.LabelShortCap, action: delegate
            {
                JointPatrolManager.MarkResidentKnightBackTeam(record);
            }));
        }
        if (menuOptions.Count == 0)
        {
            menuOptions.Add(new FloatMenuOption("OARO_JointPatrol_NoAvailableBackTeamKnights".Translate(), null));
        }

        Find.WindowStack.Add(new FloatMenu(menuOptions));
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_MainBackground");
    private static readonly Texture2D leftMainBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_LeftMainBackground");

    private static readonly Texture2D horizontalDecorationLine = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_HorizontalDecorationLine");
    private static readonly Texture2D rightBrackground_Normal = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_RightBrackground_Normal");

    private static readonly Texture2D ellipsisIcon = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_EllipsisIcon");

    private static readonly Texture2D jointPatrolTargetBackground_CrimeFighting = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolTargetBackground_CrimeFighting");
    private static readonly Texture2D jointPatrolTargetBackground_StabilityMaintenance = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolTargetBackground_StabilityMaintenance");
    private static readonly Texture2D jointPatrolTargetBackground_Assistance = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolTargetBackground_Assistance");
    private static readonly Texture2D jointPatrolTargetBackground_Supervision = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolTargetBackground_Supervision");

    private static readonly Texture2D jointPatrolStaticBackground = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolStaticBackground");

    private static readonly Texture2D jointPatrolButton = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolButton");
    private static readonly Texture2D jointPatrolButton_Down = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_JointPatrolButton_Down");

    private static readonly Texture2D verticalCuttingLine = ContentFinder<Texture2D>.Get("UI/BranchTask/OARO_VerticalCuttingLine");

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

        private Window_BranchTask Parent { get; }
        public Branch Branch { get; }
        private Map Map { get; }
        private LazyMutable<JointBranchRecord> JointBranchRecord { get; }

        private JointPatrolManager JointPatrolManager => Branch.RatkinOrder.JointPatrolManager;

        public bool ShowDetail { get; set; }

        private LazyMutable<AcceptanceReport> ChangeRadicalismDegreeAcceptance { get; }
        private LazyMutable<AcceptanceReport> ChangeFocusedTaskTypeAcceptance { get; }
        public LazyMutable<Dictionary<BranchInteractionDef, AcceptanceReport>> InteractionAcceptances { get; }
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
            InteractionAcceptances = new(refreshFunc: RefreshInteractionAcceptances);
            PatrolInteractionAcceptances = new(refreshFunc: RefreshPatrolInteractionAcceptances);
            BackTeamKnights = new(refreshFunc: () => JointPatrolManager.ParticipatingResidentKnights.Where(r => r.Branch == Branch).Select(r => r.Knight).ToList());
        }

        public void ClearCache()
        {
            ShowDetail = false;
            Branch.PostApplyBranchInteraction -= PostApplyBranchInteraction;

            JointBranchRecord.Reset();
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
            Widgets.Label(reusedRect, Branch.Potency.ToStringPercent("F0"));

            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 420f, 24f, 24f);
            OARO_WindowUtility.DrawBranchStateIcon(reusedRect, Branch, expand: false);

            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX + 525f, inRectY + 4f, 100f, 48f);
            Widgets.Label(reusedRect.TopHalf(), "OARO_AutoStartTaskChance".Translate());

            if (Branch.IsIdleNow)
            {
                Widgets.Label(reusedRect.BottomHalf(), Branch.TaskHandler.AutoStartTaskChance.ToStringPercent());
            }
            else
            {
                Widgets.Label(reusedRect.BottomHalf(), Branch.CurWorkState);
            }

            reusedRect = new(inRectX + 495f, inRectY + 24f, 25f, 20f);
            OARO_WindowUtility.DrawBranchTaskTypeIcon(reusedRect, Branch.TaskHandler.FocusedTaskType, expand: false);
        }

        private void DrawJointPatrol(Rect inRect)
        {
            float inRectX = inRect.xMin;

            Rect reusedRect = new(inRectX + 285f, inRect.y - 2f, 348f, 50f);
            GUI.DrawTexture(reusedRect, Branch.MedalHandler.PrimaryMedal.jointPatrolEntryBackgroundTexture.Texture);
            GUI.DrawTexture(reusedRect, Branch.MedalHandler.PrimaryMedal.jointPatrolEntryShadeTexture.Texture);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 295f, 128f, 32f);
            Widgets.Label(reusedRect, JointBranchRecord.Value.TaskPotency.Value.ToString("F0"));

            reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRectX + 500f, 128f, 32f);
            Text.Font = GameFont.Small;
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
            Widgets.Label(reusedRect, Branch.PopulationHandler.PublicSecurityLabel + $" ({Branch.PopulationHandler.PublicSecurity.ToStringPercent()})");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            if (Branch.TaskHandler.CurTask?.Def.hasRisk ?? false)
            {
                Widgets.Label(reusedRect, Branch.TaskHandler.CurTask.TaskRisk(Branch).ToStringPercent());
            }
            else
            {
                Widgets.Label(reusedRect, "--%");
            }

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, "");

            reusedRect = new(innerRectX + 30f, reusedRect.yMax + 10f, 300f, 20f);
            Widgets.Label(reusedRect, Branch.CurWorkState);

            reusedRect = new(innerRectX, innerRectY + 211f, 357f, 87f);
            DrawMedals(reusedRect);

            float rightRectX = innerRectX + 360f;

            float entryX = rightRectX;
            float entryY = innerRectY;
            float entryWidth = 137f;
            float entryHeight = 32f;
            int column = 0;
            Rect entryRect;

            BranchTaskType focusedTaskType = Branch.TaskHandler.FocusedTaskType;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (BranchTaskType taskType in EnumArraryLibrary.JointPatrolTaskTypeArr)
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

                if (OARO_WindowUtility.TextButtonImageDisableable(
                    butRect: entryRect,
                    label: $"OARO_JointPatrolTaskType_{taskType}".Translate(),
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
                    doMouseoverSound: true))
                {
                    JointPatrolManager.TryActiveParticipantInteraction(JointBranchRecord.Value, interactionAcceptance.Key, Map);
                }
            }

            reusedRect = new(rightRectX, innerRect.yMax - 32f, 274f, 32f);
            if (OARO_WindowUtility.TextButtonImageDisableable(
                butRect: reusedRect,
                label: "OARO_TaskWin_JoinJointPatrol".Translate(),
                acceptance: Branch.CanParticipateInJointPatrol(resultOnly: false),
                baseTex: joinJointPatrolButton,
                downTex: joinJointPatrolButton_Down,
                doMouseoverSound: true))
            {
                JointPatrolManager.ChangeParticipant(toAdd: [Branch], toRemove: null);
                Parent.TotalJointPatrolKnightCount.MarkDirty();
            }



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
                GUI.DrawTexture(reusedRect, medalRecord.Key.iconTexture.Texture, ScaleMode.ScaleToFit);

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
                        acceptance = "OARO_NotOnJointPatrol".Translate();
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