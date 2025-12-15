using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_OrderHall : OrderWindowBase
{
    public override Vector2 InitialSize => new(1462f, 919f);
    protected override float Margin => 0f;

    private Vector2 scrollPosition_Buff;
    private Vector2 scrollPosition_Level;
    private Vector2 scrollPosition_ResidentKnight;
    private Vector2 scrollPosition_AroundGroups;
    private Vector2 scrollPosition_PreferredBuildingsStr;

    private Map Map { get; }
    private int CurOrderHallLevel { get; }
    private Texture2D TopShieldTexture { get; }
    private ResidentKnightEntryDrawer ShowDetailDrawer { get; set; }

    private LazyMutable<List<string>> BuffHediffStageExplanation { get; }
    private LazyMutable<List<KeyValuePair<AroundKnightGroup, float>>> AroundKnightGroups { get; }
    private int AroundGroupTipIndex { get; set; } = -1;
    private string AroundGroupTipCache { get; set; } = string.Empty;
    private List<ResidentKnightEntryDrawer> ResidentKnightDrawers { get; }
    private string PreferredBuildingsStr { get; } = string.Empty;

    public Window_OrderHall(Map map) : base()
    {
        Map = map;
        CurOrderHallLevel = Mathf.Max(1, OrderHallHandler.Instance.OrderHallLevel);
        TopShieldTexture = new CachedTexture($"UI/OrderHall/OARO_TopShield_{CurOrderHallLevel}").Texture;

        BuffHediffStageExplanation = new(refreshFunc: RefreshBuffHediffStageExplanation);
        AroundKnightGroups = new(refreshFunc: RefreshAroundKnightGroups);
        PreferredBuildingsStr = GetPreferredBuildingsStr();

        OrderHallHandler.Instance.RefreshCache();
        ResidentKnightDrawers = new(ResidentKnightsManager.Instance.ResidentKnights.Count + 1);
        foreach (ResidentKnightRecord record in ResidentKnightsManager.Instance.ResidentKnights.Values)
        {
            ResidentKnightDrawers.Add(new ResidentKnightEntryDrawer(this, record, Map));
        }
        RefreshAroundKnightGroups();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect mainRect = new(37f, 49f, 1385f, 860f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(4f);
        float mainInnerRectY = mainInnerRect.yMin;

        Rect reusedRect = new(mainInnerRect.xMax - 21f, mainInnerRect.y + 1f, 20f, 20f);
        if (Widgets.ButtonImage(reusedRect, IconLibrary.colseX, doMouseoverSound: true))
        {
            Close();
            return;
        }

        float infoRectY = mainInnerRectY + 184f;
        float infoRectHeight = 591f;

        //中部主要区域
        Rect middleRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, infoRectY, 322f, infoRectHeight);
        DrawBuffAndLevel(middleRect);

        //左|中分割线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMin - (24f + 5f), 5f, 707f);
        GUI.DrawTexture(reusedRect, bigCuttingLine);

        //左侧主要区域（角色框）
        Rect leftRect = new(reusedRect.xMin - (19f + 442f), infoRectY, 442f, infoRectHeight);
        DrawResidentKnights(leftRect);
        ////左侧上部角色框标题
        reusedRect = OARO_WindowUtility.CenterRectOnX(leftRect, infoRectY - (36f + 32f), 128f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_ResidentKnights".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;


        //中|右分割线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMax + 24f, 5f, 707f);
        GUI.DrawTexture(reusedRect, bigCuttingLine);

        //右侧主要区域
        Rect rightRect = new(reusedRect.xMax + 19f, infoRectY, 443f, infoRectHeight);
        DrawAroundKnightGroups(rightRect);

        reusedRect = OARO_WindowUtility.CenterRectOnX(rightRect, rightRect.y - (36f + 42f), 256f, 42f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_HallWin_AroundKnightGroup".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        //顶部绶带
        reusedRect = new(37f, 46f, 1388f, 104f);
        GUI.DrawTexture(reusedRect, topRibbon);
        //顶部盾徽
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainRect, 0f, 215f, 211f);
        //盾徽绘制逻辑（未完成）
        GUI.DrawTexture(reusedRect, TopShieldTexture);

        //左侧上部竖旗
        reusedRect = new(4f, 57f, 70f, 325f);
        GUI.DrawTexture(reusedRect, leftVerticalFlag);

        //左侧下部烛台
        reusedRect = new(14f, inRect.yMax - 284f, 50f, 284f);
        GUI.DrawTexture(reusedRect, leftCandlestick);
    }

    private void DrawResidentKnights(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftBackground);
        Rect innerRect = inRect.ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;
        Rect titleRect = innerRect;
        titleRect.height = 22f;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(innerRectX + 30f, innerRectY, 50f, 22f);
        Widgets.Label(reusedRect, "OARO_HallWin_Knight".Translate());

        reusedRect = new(innerRectX + 150f, innerRectY, 85f, 22f);
        Widgets.Label(reusedRect, "OARO_HallWin_MeditationFactor".Translate());

        reusedRect = new(innerRectX + 235f, innerRectY, 85f, 22f);
        Widgets.Label(reusedRect, "OARO_HallWin_MeditationPoint".Translate());

        reusedRect = new(innerRectX + 320f, innerRectY, 85f, 22f);
        Widgets.Label(reusedRect, "OARO_HallWin_KnightRank".Translate());

        Rect knightRect = new(innerRectX, titleRect.yMax + 2f, innerRect.width, 435f);
        float entryX = knightRect.xMin - 2f;
        float entryY = knightRect.yMin;
        float entryWidth = ResidentKnightEntryDrawer.Width;

        Rect knightViewRect = knightRect;
        knightViewRect.width = entryWidth;
        knightViewRect.height = (ResidentKnightDrawers.Count + 1) * ResidentKnightEntryDrawer.SummaryHeight + ResidentKnightEntryDrawer.DetailHeight + 10f;

        Widgets.BeginScrollView(knightRect, ref scrollPosition_ResidentKnight, knightViewRect);
        foreach (ResidentKnightEntryDrawer drawer in ResidentKnightDrawers)
        {
            Vector2 entryPos = new(entryX, entryY);
            entryY += drawer.Draw(entryPos);
        }
        Widgets.EndScrollView();

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;

        reusedRect = new(innerRectX, innerRectY + 462f, 245f, 120f);
        Widgets.Label(reusedRect, "OARO_HallWin_ResidentKnightCeiling".Translate(ResidentKnightsManager.ResidentKnightCeiling.ToString()));

        Text.Font = GameFont.Small;
        reusedRect = new(innerRect.xMax - 190f, innerRectY + 462f, 190f, 24f);
        Widgets.Label(reusedRect, "OARO_HallWin_PreferredBuildingsLabel".Translate());

        Text.Font = GameFont.Tiny;
        reusedRect = new(innerRect.xMax - 190f, reusedRect.yMax + 2f, 190f, 95f);
        Widgets.LabelScrollable(reusedRect, PreferredBuildingsStr, ref scrollPosition_PreferredBuildingsStr);

        OARO_WindowUtility.ResetText();
    }

    private void DrawBuffAndLevel(Rect inRect)
    {
        GUI.DrawTexture(inRect, middleBackground);
        inRect = inRect.ContractedBy(3f);

        Rect reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.y + 7f, 256f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_HallWin_CurBuff".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect buffRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 270f);
        float entryX = buffRect.x;
        float entryY = buffRect.y;
        float entryHeight = 30f;

        Rect buffViewRect = buffRect;
        buffViewRect.width -= 16f;
        float entryWidth = buffViewRect.width;

        List<string> buffHediffStageExplanation = BuffHediffStageExplanation.Value;
        int buffCount = buffHediffStageExplanation.Count;
        int buffUseCount = Mathf.Max(9, buffCount);
        buffViewRect.height = buffUseCount * entryHeight;


        Widgets.BeginScrollView(buffRect, ref scrollPosition_Buff, buffViewRect);

        Rect entryRect;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < buffHediffStageExplanation.Count; i++)
        {
            entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if ((i & 1) == 0)
            {
                GUI.DrawTexture(entryRect, middleList_Dark);
            }
            Widgets.Label(entryRect, buffHediffStageExplanation[i]);
        }

        if (buffUseCount > buffCount)
        {
            for (int i = buffCount; i < buffUseCount; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if ((i & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, middleList_Dark);
                }
            }
        }
        Widgets.EndScrollView();


        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, buffRect.yMax + 12f, 287f, 3f);
        GUI.DrawTexture(reusedRect, middleCuttingLine);

        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 256f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_HallWin_NextLevelNeed".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect levelRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 210f);
        entryX = levelRect.x;
        entryY = levelRect.y;
        entryHeight = 30f;

        Rect levelBuffViewRect = levelRect;
        levelBuffViewRect.width -= 16f;
        entryWidth = levelBuffViewRect.width;

        int levelCount = 0;
        int levelUseCount = Mathf.Max(18, levelCount);
        levelBuffViewRect.height = levelUseCount * entryHeight;

        Widgets.BeginScrollView(levelRect, ref scrollPosition_Level, levelBuffViewRect);
        if (levelUseCount > buffCount)
        {
            for (int i = buffCount; i < levelUseCount; i++)
            {
                entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if ((i & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, middleList_Dark);
                }
            }
        }
        Widgets.EndScrollView();

    }

    private void DrawAroundKnightGroups(Rect inRect)
    {
        GUI.DrawTexture(inRect, rightBackground);
        inRect = inRect.ContractedBy(3f);

        Rect titleRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.y, 416f, 40f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(titleRect.x, titleRect.y, 176f, 40f);
        Widgets.Label(reusedRect, "OARO_HallWin_GroupInfo".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 72f;
        Widgets.Label(reusedRect, "OARO_HallWin_BusyLevel".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 96f;
        Widgets.Label(reusedRect, "OARO_HallWin_Route".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 64f;
        Widgets.Label(reusedRect, "OARO_SuccessRate".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect groupRect = new(titleRect.x, titleRect.yMax, 424f, 535f);

        float entryX = groupRect.x;
        float entryY = groupRect.y;
        float entryHeight = 107f;

        Rect viewRect = groupRect;
        viewRect.width -= 16f;
        float entryWidth = viewRect.width;

        List<KeyValuePair<AroundKnightGroup, float>> aroundKnightGroups = AroundKnightGroups.Value;
        int groupCount = aroundKnightGroups.Count;
        int maxCount = Mathf.Max(5, groupCount);
        viewRect.height = maxCount * entryHeight;

        Widgets.BeginScrollView(groupRect, ref scrollPosition_AroundGroups, viewRect);

        for (int i = 0; i < groupCount; i++)
        {
            reusedRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;

            if (DrawAroundKnightGroup(reusedRect, aroundKnightGroups[i].Key, aroundKnightGroups[i].Value, i))
            {
                AroundKnightGroups.MarkDirty();
            }
        }

        if (maxCount > groupCount)
        {
            for (int i = groupCount; i < maxCount; i++)
            {
                reusedRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;

                if ((i & 1) == 0)
                {
                    GUI.DrawTexture(reusedRect, rightList_Dark);
                }
            }
        }

        Widgets.EndScrollView();
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private bool DrawAroundKnightGroup(Rect inRect, AroundKnightGroup group, float successRate, int index)
    {
        if ((index & 1) == 0)
        {
            GUI.DrawTexture(inRect, rightList_Dark);
        }

        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.x + 2f, 55f, 60f);
        GUI.DrawTexture(reusedRect, aroundKnightGroupIcon, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleLeft;

        reusedRect = new(reusedRect.xMax + 8f, inRect.y + 24f, 105f, 32f);
        Widgets.LabelEllipses(reusedRect, group.Branch.Name);

        reusedRect = new(reusedRect.xMin + 16f, reusedRect.yMax, 97f, 32f);
        Widgets.LabelEllipses(reusedRect, "└  " + group.RatkinOrder.Name);

        Text.Anchor = TextAnchor.MiddleCenter;

        reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.x + 176f, 72f, 32f);
        Widgets.Label(reusedRect, $"OARO_AroundKnightGroup_{group.CurBusyLevel}".Translate());

        reusedRect = new(reusedRect.xMax, inRect.y, 96f, inRect.height);

        reusedRect = new(reusedRect.xMax, inRect.y, 64f, inRect.height);
        reusedRect.ContractedBy(2f);

        if (Mouse.IsOver(reusedRect))
        {
            if (index != AroundGroupTipIndex)
            {
                AroundGroupTipIndex = index;
                string aroundGroupTipCache = string.Empty;
                GlobalInteractionUtility.InvitationAcceptanceChance(group, resultOnly: false, out aroundGroupTipCache);
                AroundGroupTipCache = aroundGroupTipCache;
            }
            if (!string.IsNullOrEmpty(AroundGroupTipCache))
            {
                TooltipHandler.TipRegion(reusedRect, () => AroundGroupTipCache, 21345447);
            }
        }

        string buttonText = "OAFrame_Invite".Translate() + "\n";
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: buttonText + successRate.ToStringPercent("F0"),
            baseTex: aroundKnightGroupButton,
            downTex: aroundKnightGroupButton_Down))
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
            GlobalInteractionUtility.InviteAroundKnightGroup(group, map);
            return true;
        }
        return false;
    }

    private List<string> RefreshBuffHediffStageExplanation()
    {
        HediffStage buffHediffStage = ResidentKnightsManager.Instance.BuffHediffStage;
        if (buffHediffStage is null)
        {
            return [];
        }

        List<string> result = new(buffHediffStage.statFactors.Count + buffHediffStage.statOffsets.Count + 1);
        foreach (StatDrawEntry item in buffHediffStage.SpecialDisplayStats())
        {
            try
            {
                string explanation = $"{item.LabelCap}  {item.ValueString}";
                result.Add(explanation);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"get explanation from {nameof(StatDrawEntry)}.",
                    typeName: nameof(Window_OrderHall),
                    methodName: nameof(RefreshBuffHediffStageExplanation),
                    needStackTrace: true);
            }
        }

        return result;
    }
    private List<KeyValuePair<AroundKnightGroup, float>> RefreshAroundKnightGroups()
    {
        AroundGroupTipIndex = -1;
        AroundGroupTipCache = string.Empty;

        List<KeyValuePair<AroundKnightGroup, float>> pairs = new(AroundKnightGroupsManager.AroundKnightGroups.Count);
        IReadOnlyList<AroundKnightGroup> tempGroups = AroundKnightGroupsManager.AroundKnightGroups;
        for (int i = 0; i < tempGroups.Count; i++)
        {
            float successRate = 0f;
            try
            {
                successRate = GlobalInteractionUtility.InvitationAcceptanceChance(tempGroups[i], resultOnly: true, out _);
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"get invitation acceptance chance of {nameof(AroundKnightGroup)}",
                    typeName: nameof(Window_OrderHall),
                    methodName: nameof(RefreshAroundKnightGroups),
                    needStackTrace: true);
            }

            pairs.Add(new KeyValuePair<AroundKnightGroup, float>(tempGroups[i], successRate));
        }
        return pairs;
    }

    private void ClearShowDetailDrawer()
    {
        ShowDetailDrawer?.ClearCache();
        ShowDetailDrawer = null;
    }
    private void OnShowDrawerDetailChanged(ResidentKnightEntryDrawer drawer)
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

    private string GetPreferredBuildingsStr()
    {
        try
        {
            StringBuilder sb = new(string.Empty);
            HashSet<ThingDef> allPreferredBuildings = OrderHallUtility.GetAllResidentKnightPreferredBuildingDefs(OrderHallHandler.Instance.OrderHallRoom);

            foreach (ThingDef def in OrderDefDataBase.AllResidentPreferredBuildings)
            {
                sb.AppendLine(def.label.Colorize(allPreferredBuildings.Contains(def) ? Color.white : Color.gray));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "get preferred buildings description",
                typeName: nameof(Window_OrderHall),
                methodName: nameof(GetPreferredBuildingsStr),
                needStackTrace: true);
            return "ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable);
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_MainBackground");

    private static readonly Texture2D topRibbon = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_TopRibbon");

    private static readonly Texture2D leftVerticalFlag = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_LeftVerticalFlag");
    private static readonly Texture2D leftCandlestick = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_LeftCandlestick");

    private static readonly Texture2D leftBackground = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_LeftBackground");

    private static readonly Texture2D middleBackground = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_MiddleBackground");
    private static readonly Texture2D middleCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_MiddleCuttingLine");
    private static readonly Texture2D middleList_Dark = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_MiddleList_Dark");

    private static readonly Texture2D rightBackground = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_RightBackground");
    private static readonly Texture2D rightList_Dark = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_RightList_Dark");

    private static readonly Texture2D aroundKnightGroupIcon = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_AroundKnightGroupIcon");
    private static readonly Texture2D aroundKnightGroupButton = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_AroundKnightGroupButton");
    private static readonly Texture2D aroundKnightGroupButton_Down = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_AroundKnightGroupButton_Down");

    private static readonly Texture2D bigCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderHall/OARO_BigCuttingLine");

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
                AcceptanceReport acceptance = GlobalInteractionUtility.CanUpgradeResidentKnightRank(Record, Map, resultOnly: false);
                if (acceptance)
                {
                    GlobalInteractionUtility.UpgradeResidentKnightRank(Record, Map);
                }
                else
                {
                    Messages.Message("OARO_CanNotUpgradeResidentKnightRankWithReason".Translate(acceptance.Reason.Named(KeyLibrary_FormatArgName.Reason)), MessageTypeDefOf.RejectInput, historical: false);
                }
                OnConditionChanged();
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
                GUI.DrawTexture(starRect, starWhite, ScaleMode.ScaleToFit);
            }
            while (starCount < 6 && starCount < PreferredFurnitureCount.Value.Item2)
            {
                starCount++;
                starRect = new(starRectX, starRectY, 18f, 18f);
                starRectX += 20f;
                GUI.DrawTexture(starRect, starBlack, ScaleMode.ScaleToFit);
            }
            TooltipHandler.TipRegion(reusedRect, () => PreferredFurnitureExplanation.Value, uniqueId: 59748631);

            Text.Anchor = TextAnchor.MiddleCenter;
            reusedRect = new(inRectX + 260f, inRectY + 164f, 80f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_HonorAcademic".Translate());
            reusedRect = new(inRectX + 260f, reusedRect.yMax + 8f, 80f, 20f);


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
                GUI.DrawTexture(reusedRect, honorDef.iconTexture.Texture);

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
                //////////////////////////////////
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            reusedRect = new(inRectX + 32f, inRectY + 164f, 256f, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_GenealAcademic".Translate());
            Rect academicRect = Rect.MinMaxRect(inRectX + 32f, reusedRect.yMax + 8f, inRectX + (32f + 140f), inRect.yMax - 2f);
            float entryX = academicRect.xMin;
            float entryY = academicRect.yMin;
            float entryHeight = 22f;
            Rect academicViewRect = academicRect;
            float entryWidth = academicViewRect.width;
            academicViewRect.height = (Record.GenealAcademicDefs.Count + 1) * entryHeight;

            Widgets.BeginScrollView(academicRect, ref scrollPosition_GenealAcademic, academicViewRect, showScrollbars: false);
            foreach (KeyValuePair<ResidentKnightAcademicDef, int> kv in Record.GenealAcademicDefs)
            {
                Rect entryRect = new(entryX, entryY, entryHeight, entryWidth);
                entryY += entryHeight;
                DrawGenealAcademic(entryRect, kv.Key, kv.Value);
            }
            Widgets.EndScrollView();

            OARO_WindowUtility.ResetText();
            return inRect.yMax;
        }

        private void DrawGenealAcademic(Rect inRect, ResidentKnightAcademicDef academicDef, int academicLevel)
        {
            Rect innerRect = inRect.ContractedBy(1f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRect = inRect;
            textRect.width = 40f;
            Widgets.Label(textRect, academicDef.label);

            Rect levelRect = Rect.MinMaxRect(textRect.xMax + 2f, innerRect.y, innerRect.xMax, 64f);
            GUI.DrawTexture(levelRect, BaseContent.BlackTex);
            levelRect = levelRect.ContractedBy(2f);

            float paneX = levelRect.x;
            float paneY = levelRect.y;
            float paneWidth = 8f;
            float paneHeight = levelRect.height;
            float paneInterval = 2f;

            int levelUsed = academicLevel > 6 ? 6 : academicLevel;
            for (int i = 0; i < levelUsed; i++)
            {
                Rect paneRect = new(paneX, paneY, paneWidth, paneHeight);
                paneX += (paneWidth + paneInterval);
                GUI.DrawTexture(paneRect, BaseContent.GreyTex);
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

        private static readonly Texture2D starWhite = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_StarWhite");
        private static readonly Texture2D starBlack = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_StarBlack");

        private static readonly Texture2D rankBackGround_RegularS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_RegularS");
        private static readonly Texture2D rankBackGround_EliteS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_EliteS");
        private static readonly Texture2D rankBackGround_HonorS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_HonorS");
        private static readonly Texture2D rankBackGround_CrownS = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_CrownS");
    }

    [StaticConstructorOnStartup]
    private class SecondWindow_UpgradeRank : OrderWindowBase
    {
        private ResidentKnightRecord.Rank TargetRank { get; }

        public Action AcceptAction { get; }

        public override Vector2 InitialSize => new(593f, 480f);

        public SecondWindow_UpgradeRank(ResidentKnightRecord.Rank targetRank, Action acceptAction) : base()
        {
            TargetRank = targetRank;
            AcceptAction = acceptAction;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUI.DrawTexture(inRect, mainBackGround);
            Rect innerRect = OARO_WindowUtility.CenterRect(inRect, 510f, 402f).ContractedBy(2f);
            float innerRectX = innerRect.xMin;
            float innerRectY = innerRect.yMin;

            Text.Anchor = TextAnchor.MiddleCenter;

            Rect reusedRect = new(innerRectX, innerRectY + 24f, innerRect.width, 32f);
            Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{TargetRank}".Translate());

            Text.Font = GameFont.Small;
            reusedRect = new(innerRectX + 100f, innerRectY + 60f, innerRect.width - 200f, 64f);
            Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{TargetRank}Desc".Translate());

            Text.Font = GameFont.Medium;
            reusedRect = new(innerRectX, innerRectY + 137f, innerRect.width, 185f);
            DrawRankBackGround(reusedRect);
            Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{TargetRank}Knight".Translate().Colorize(ResidentKnightRecord.GetRankColor(TargetRank)));

            Text.Font = GameFont.Small;
            reusedRect = new(innerRectX, innerRectY + 330f, innerRect.width, 20f);
            Widgets.Label(reusedRect, "OARO_HallWin_UpgradeRankConfirm".Translate());

            reusedRect = new(innerRectX + 150f, innerRectY + 356f, 71f, 22f);
            if (OARO_WindowUtility.TextButtonImage(reusedRect, "Cancel".Translate(), secondSmallButton, secondSmallButton_Down, doMouseoverSound: true))
            {
                Close();
            }

            reusedRect = new(innerRectX + 290f, innerRectY + 356f, 71f, 22f);
            if (OARO_WindowUtility.TextButtonImage(
                butRect: reusedRect,
                label: "Confirm".Translate(),
                baseTex: secondSmallButton,
                downTex: secondSmallButton_Down,
                doMouseoverSound: true))
            {
                AcceptAction?.Invoke();
                Close();
            }
        }

        private void DrawRankBackGround(Rect inRect)
        {
            switch (TargetRank)
            {
                case ResidentKnightRecord.Rank.Regular:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_RegularB, ScaleMode.ScaleToFit);
                        return;
                    }
                case ResidentKnightRecord.Rank.Elite:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_EliteB, ScaleMode.ScaleToFit);
                        return;
                    }
                case ResidentKnightRecord.Rank.Honor:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_HonorB, ScaleMode.ScaleToFit);
                        return;
                    }
                case ResidentKnightRecord.Rank.Crown:
                    {
                        GUI.DrawTexture(inRect, rankBackGround_CrownB, ScaleMode.ScaleToFit);
                        return;
                    }
                default: return;
            }
        }

        private static readonly Texture2D mainBackGround = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_SecondWindow_Background");

        private static readonly Texture2D secondSmallButton = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_SecondSmallButton");
        private static readonly Texture2D secondSmallButton_Down = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_SecondSmallButton_Down");

        private static readonly Texture2D rankBackGround_RegularB = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_RegularB");
        private static readonly Texture2D rankBackGround_EliteB = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_EliteB");
        private static readonly Texture2D rankBackGround_HonorB = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_HonorB");
        private static readonly Texture2D rankBackGround_CrownB = ContentFinder<Texture2D>.Get("UI/OrderHall/ResidentKnight/OARO_RankBackground_CrownB");
    }
}