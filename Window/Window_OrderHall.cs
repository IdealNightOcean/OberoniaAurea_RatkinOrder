using NightOcean;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public partial class Window_OrderHall : OrderWindowBase
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
    private IReadOnlyList<string> HallLevelEffectDescs { get; }
    private IReadOnlyList<string> HallLevelRestrictionDescs { get; }
    private Texture2D TopShieldTexture { get; }
    private ResidentKnightEntryDrawer ShowDetailDrawer { get; set; }
    private LazyMutable<List<KeyValuePair<AroundKnightGroup, float>>> AroundKnightGroups { get; }
    private int AroundGroupTipIndex { get; set; } = -1;
    private string AroundGroupTipCache { get; set; } = string.Empty;
    private List<ResidentKnightEntryDrawer> ResidentKnightDrawers { get; }
    private string PreferredBuildingsStr { get; } = string.Empty;

    public Window_OrderHall(Map map) : base()
    {
        Map = map;

        AroundKnightGroups = new(refreshFunc: RefreshAroundKnightGroups);
        PreferredBuildingsStr = GetPreferredBuildingsStr();

        OrderHallHandler.Instance.RefreshCache();

        CurOrderHallLevel = Mathf.Max(1, OrderHallHandler.Instance.OrderHallLevel);
        TopShieldTexture = new CachedTexture($"UI/OrderHall/OARO_TopShield_{CurOrderHallLevel}").Texture;

        OrderHallRestrictionExtension hallRestriction = OARO_ModDefOf.OARO_RatkinOrderHall.GetModExtension<OrderHallRestrictionExtension>();

        HallLevelEffectDescs = [.. (hallRestriction.GetRestrictionOfLevel(CurOrderHallLevel)?.effectDescs ?? Enumerable.Empty<string>())];
        List<string> restrictionDescs;
        if (CurOrderHallLevel >= hallRestriction.MaxLevel)
        {
            restrictionDescs = ["OARO_ReachMax_OrderHallLevel".Translate()];
        }
        else
        {
            restrictionDescs = hallRestriction.GetRestrictionOfLevel(CurOrderHallLevel + 1)?.GetRestrictionDescs().ToList();
        }
        HallLevelRestrictionDescs = restrictionDescs ?? [];

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

        if (OARO_WindowUtility.DrawCloseX(mainInnerRect))
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
        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMin - (24f + 5f), 5f, 707f);
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
        GUI.DrawTexture(reusedRect, TopShieldTexture, ScaleMode.ScaleToFit);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(reusedRect.x + 30f, reusedRect.yMax - (20f + 38f), reusedRect.width - 60f, 38f);
        Widgets.Label(reusedRect, $"OARO_OrderHall_LevelLabel_{CurOrderHallLevel}".Translate());
        TooltipHandler.TipRegion(reusedRect, () => "OARO_OrderHall_LevelLabelTip".Translate(), uniqueId: 3786490);

        //左侧上部竖旗
        reusedRect = new(4f, 57f, 70f, 325f);
        GUI.DrawTexture(reusedRect, leftVerticalFlag);

        //左侧下部烛台
        reusedRect = new(14f, inRect.yMax - 284f, 50f, 284f);
        GUI.DrawTexture(reusedRect, leftCandlestick);

        OARO_WindowUtility.ResetText();
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
        if (ResidentKnightDrawers.Count <= 0)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(knightRect, "OARO_HallWin_NoResidentKnight".Translate().Colorize(Color.gray));
        }
        else
        {
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
                entryY = drawer.Draw(entryPos);
            }
            Widgets.EndScrollView();
        }

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
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_HallWin_CurBuff".Translate());

        Rect effectRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 270f);
        float entryX = effectRect.x;
        float entryY = effectRect.y;
        float entryHeight = 30f;

        Rect effectViewRect = effectRect;
        effectViewRect.width -= 16f;
        float entryWidth = effectViewRect.width;

        int effectCount = HallLevelEffectDescs.Count;
        int effectUseCount = Mathf.Max(9, effectCount);
        effectViewRect.height = effectUseCount * entryHeight;

        Widgets.BeginScrollView(effectRect, ref scrollPosition_Buff, effectViewRect);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < HallLevelEffectDescs.Count; i++)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if ((i & 1) == 0)
            {
                GUI.DrawTexture(entryRect, middleList_Dark);
            }
            Widgets.Label(entryRect, HallLevelEffectDescs[i]);
        }

        if (effectUseCount > effectCount)
        {
            for (int i = effectCount; i < effectUseCount; i++)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if ((i & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, middleList_Dark);
                }
                if (i == effectCount)
                {
                    Widgets.Label(entryRect, "OARO_HallWin_EmptyBuff".Translate().Colorize(Color.gray));
                }
            }
        }
        Widgets.EndScrollView();


        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, effectRect.yMax + 12f, 287f, 3f);
        GUI.DrawTexture(reusedRect, middleCuttingLine);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 256f, 32f);
        Widgets.Label(reusedRect, "OARO_HallWin_NextLevelNeed".Translate());

        Rect levelRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 210f);
        entryX = levelRect.x;
        entryY = levelRect.y;
        entryHeight = 30f;

        Rect levelBuffViewRect = levelRect;
        levelBuffViewRect.width -= 16f;
        entryWidth = levelBuffViewRect.width;

        int levelCount = HallLevelRestrictionDescs.Count;
        int levelUseCount = Mathf.Max(7, levelCount);
        levelBuffViewRect.height = levelUseCount * entryHeight;

        Widgets.BeginScrollView(levelRect, ref scrollPosition_Level, levelBuffViewRect);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < levelCount; i++)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if ((i & 1) == 0)
            {
                GUI.DrawTexture(entryRect, middleList_Dark);
            }
            Widgets.Label(entryRect, HallLevelRestrictionDescs[i]);
        }

        if (levelUseCount > levelCount)
        {
            for (int i = levelCount; i < levelUseCount; i++)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if ((i & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, middleList_Dark);
                }
                if (i == levelCount)
                {
                    Widgets.Label(entryRect, "OARO_HallWin_EmptyLevelDesc".Translate().Colorize(Color.gray));
                }
            }
        }
        Widgets.EndScrollView();

        OARO_WindowUtility.ResetText();
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

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
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

                Widgets.Label(reusedRect, "OARO_HallWin_NoOtherAroundKnightGroup".Translate().Colorize(Color.gray));
            }
        }

        Widgets.EndScrollView();
        OARO_WindowUtility.ResetText();
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

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(reusedRect.xMax, inRect.y, 96f, inRect.height);
        Rect routeRect = reusedRect;
        routeRect.height = reusedRect.height / 3f;
        Widgets.Label(routeRect, group.Source);
        routeRect = new(reusedRect.x, reusedRect.y + reusedRect.height / 3f, reusedRect.width, reusedRect.height / 3f);
        Widgets.Label(routeRect, "|");
        routeRect = new(reusedRect.x, reusedRect.yMax - reusedRect.height / 3f, reusedRect.width, reusedRect.height / 3f);
        Widgets.Label(routeRect, group.Destination);

        Text.Anchor = TextAnchor.MiddleCenter;
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

        string buttonText = "OAFrame_Invite".Translate() + "\n" + successRate.ToStringPercent("F0");
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: buttonText,
            baseTex: aroundKnightGroupButton,
            downTex: aroundKnightGroupButton_Down))
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
            GlobalInteractionUtility.InviteAroundKnightGroup(group, map);
            return true;
        }
        return false;
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

}