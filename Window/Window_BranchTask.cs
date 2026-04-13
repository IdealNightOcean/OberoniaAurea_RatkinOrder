using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.JointPatrolManager;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public partial class Window_BranchTask : OrderWindowBase
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
    private LazyMutable<string> JointPatrolParticipateInKnightStr { get; }
    private LazyMutable<string> JointPatrolNotParticipateInKnightStr { get; }

    private int OrderResidentKnightCount { get; }
    private LazyMutable<int> TotalJointPatrolKnightCount { get; }

    public Window_BranchTask(RatkinOrder ratkinOrder, Map map)
    {
        RatkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        Map = map ?? throw new ArgumentNullException(nameof(map));

        MapRecommendationCount = new(refreshFunc: () => RecommendationUtility.CurRecommendationCount(Map));
        JointPatrolManager = RatkinOrder.JointPatrolManager;
        JointPatrolManager.TaskPotencys.MarkDirty();
        JointPatrolNeededTaskPotency = new(valueFactory: () => JointPatrolManager.NeededTaskPotency);
        JointPatrolRecordDetailStr = new(valueFactory: JointPatrolRecordDetail);
        JointPatrolParticipateInKnightStr = new(refreshFunc: RefrshJointPatrolParticipateInKnightStr);
        JointPatrolNotParticipateInKnightStr = new(refreshFunc: RefrshJointPatrolNotParticipateInKnightStr);

        TotalJointPatrolKnightCount = new(refreshFunc: () => JointPatrolManager?.ParticipantsDict.Keys.Sum(b => b.Squad.AllCrewCountInt) ?? 0);
        OrderResidentKnightCount = ResidentPawnsManager.Instance.ResidentKnights.Where(kv => kv.Value.RatkinOrder == RatkinOrder).Count();

        BranchTaskEntryDrawers = new(RatkinOrder.BranchManager.AllBranchesCount);
        foreach (Branch branch in RatkinOrder.BranchManager.AllBranches)
        {
            BranchTaskEntryDrawers.Add(new BranchTaskEntryDrawer(this, branch, Map));
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        ShowDetailDrawer?.ClearCache();
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);
        Rect mainInnerRect = inRect.ContractedBy(2f);
        float mainInnerRectX = mainInnerRect.xMin;
        float mainInnerRectY = mainInnerRect.yMin;

        if (OARO_WindowUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }
        if (OARO_WindowUtility.DrawBackArrow_Corner(mainInnerRect))
        {
            Window_RatkinOrder ratkinOrderWin = new(Map);
            Find.WindowStack.Add(ratkinOrderWin);
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(mainInnerRectX, mainInnerRectY + 36f, mainInnerRect.width, 32f);
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
        TooltipHandler.TipRegion(reusedRectII, () => JointPatrolNotParticipateInKnightStr.Value, uniqueId: 14604238);

        reusedRect = new(innerRectX + 10f, reusedRect.yMax, 150f, 26f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, "OARO_TaskWin_BackKnights".Translate());
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reusedRect, JointPatrolManager.ParticipatingResidentKnights.Count.ToString());
        reusedRectII = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 4f, 30f, 23f);
        GUI.DrawTexture(reusedRectII, ellipsisIcon);
        TooltipHandler.TipRegion(reusedRectII, () => JointPatrolParticipateInKnightStr.Value, uniqueId: 23041468);

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

    private string RefrshJointPatrolParticipateInKnightStr()
    {
        if (JointPatrolManager.CurState == PatrolState.Invalid)
        {
            return string.Empty;
        }

        return GenLabel.ThingsLabel(JointPatrolManager.ParticipatingResidentKnights.Select(r => r.Pawn).Cast<Thing>());
    }

    private string RefrshJointPatrolNotParticipateInKnightStr()
    {
        if (JointPatrolManager.CurState == PatrolState.Invalid)
        {
            return string.Empty;
        }
        List<Thing> notParticipatingPawns = [];

        foreach (ResidentKnight record in ResidentPawnsManager.Instance.ResidentKnights.Values.Where(r => r.RatkinOrder == RatkinOrder))
        {
            if (!JointPatrolManager.ParticipatingResidentKnights.Contains(record))
            {
                notParticipatingPawns.Add(record.Pawn);
            }
        }

        return GenLabel.ThingsLabel(notParticipatingPawns);
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
        foreach ((Pawn knight, ResidentKnight record) in ResidentPawnsManager.Instance.ResidentKnights)
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
                JointPatrolParticipateInKnightStr.MarkDirty();
                JointPatrolNotParticipateInKnightStr.MarkDirty();
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

}