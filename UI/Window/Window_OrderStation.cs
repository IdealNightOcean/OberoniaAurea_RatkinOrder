using NightOcean;
using OberoniaAurea.RatkinOrder.UI;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public partial class Window_OrderStation : OrderWindowBase
{
    public override Vector2 InitialSize => new(1462f, 919f);
    protected override float Margin => 0f;

    private Vector2 scrollPosition_Buff;
    private Vector2 scrollPosition_Level;
    private Vector2 scrollPosition_ResidentKnight;
    private Vector2 scrollPosition_AroundGroups;
    private Vector2 scrollPosition_PreferredBuildingsStr;

    private Map Map { get; }
    private int CurOrderStationLevel { get; }
    private int AroundGroupSeasonInvitationLimit { get; }

    private IReadOnlyList<string> StationLevelEffectDescs { get; }
    private IReadOnlyList<(string condition, bool isMet)> StationLevelRestrictionDescs { get; }
    private Texture2D TopShieldTexture { get; }
    private ResidentKnightEntryDrawer ShowDetailDrawer { get; set; }
    private LazyMutable<List<KeyValuePair<AroundKnightGroup, float>>> AroundKnightGroups { get; }
    private int AroundGroupTipIndex { get; set; } = -1;
    private string AroundGroupTipCache { get; set; } = string.Empty;
    private List<ResidentKnightEntryDrawer> ResidentKnightDrawers { get; }
    private string PreferredBuildingsStr { get; } = string.Empty;

    public Window_OrderStation(Map map) : base()
    {
        Map = map;

        AroundKnightGroups = new(refreshFunc: RefreshAroundKnightGroups);
        PreferredBuildingsStr = GetPreferredBuildingsStr();

        OrderStationHandler.Instance.RefreshCache();

        CurOrderStationLevel = Mathf.Max(1, OrderStationHandler.Instance.OrderStationLevel);
        TopShieldTexture = new CachedTexture($"UI/OrderStation/OARO_TopShield_{CurOrderStationLevel}").Texture;
        AroundGroupSeasonInvitationLimit = GlobalInteractionUtility.SeasonInvitationLimit();

        OrderStationRestrictionExtension stationRestriction = OARO_ModDefOf.OARO_RatkinOrderStation.GetModExtension<OrderStationRestrictionExtension>();

        StationLevelEffectDescs = [.. (stationRestriction.GetRestrictionOfLevel(CurOrderStationLevel)?.effectDescs ?? Enumerable.Empty<string>())];
        StationLevelRestrictionDescs = OrderStationUtility.GetStationUpgradeInfo() ?? [];

        ResidentKnightDrawers = new(ResidentPawnsManager.Instance.ResidentKnights.Count + 1);
        foreach (ResidentKnight record in ResidentPawnsManager.Instance.ResidentKnights)
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

        if (OARO_UIUtility.DrawCloseX_Corner(mainInnerRect))
        {
            Close();
            return;
        }

        float infoRectY = mainInnerRectY + 184f;
        float infoRectHeight = 591f;

        //中部主要区域
        Rect middleRect = OARO_UIUtility.CenterRectOnX(mainInnerRect, infoRectY, 322f, infoRectHeight);
        DrawBuffAndLevel(middleRect);

        //左|中分割线
        Rect reusedRect = OARO_UIUtility.CenterRectOnY(mainInnerRect, middleRect.xMin - (24f + 5f), 5f, 707f);
        GUI.DrawTexture(reusedRect, bigCuttingLine);

        //左侧主要区域（角色框）
        Rect leftRect = new(reusedRect.xMin - (19f + 442f), infoRectY, 442f, infoRectHeight);
        DrawResidentKnights(leftRect);
        ////左侧上部角色框标题
        reusedRect = OARO_UIUtility.CenterRectOnX(leftRect, infoRectY - (36f + 32f), 128f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_ResidentKnights".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;


        //中|右分割线
        reusedRect = OARO_UIUtility.CenterRectOnY(mainInnerRect, middleRect.xMax + 24f, 5f, 707f);
        GUI.DrawTexture(reusedRect, bigCuttingLine);

        //右侧主要区域
        Rect rightRect = new(reusedRect.xMax + 19f, infoRectY, 443f, infoRectHeight);
        DrawAroundKnightGroups(rightRect);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = OARO_UIUtility.CenterRectOnX(rightRect, rightRect.yMin - 36f, 256f, 36f);
        Widgets.Label(reusedRect, "OARO_StationWin_AroundKnightGroupLimit".Translate(AroundKnightGroupsManager.Instance.SeasonInvitationUsed, AroundGroupSeasonInvitationLimit));

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_UIUtility.CenterRectOnX(rightRect, reusedRect.yMin - 42f, 256f, 42f);
        Widgets.Label(reusedRect, "OARO_StationWin_AroundKnightGroup".Translate());

        //顶部绶带
        reusedRect = new(37f, 46f, 1388f, 104f);
        GUI.DrawTexture(reusedRect, topRibbon);
        //顶部盾徽
        reusedRect = OARO_UIUtility.CenterRectOnX(mainRect, 0f, 215f, 211f);
        //盾徽绘制逻辑（未完成）
        GUI.DrawTexture(reusedRect, TopShieldTexture, ScaleMode.ScaleToFit);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(reusedRect.x + 30f, reusedRect.yMax - (20f + 38f), reusedRect.width - 60f, 38f);
        Widgets.Label(reusedRect, $"OARO_OrderStation_LevelLabel_{CurOrderStationLevel}".Translate());
        TooltipHandler.TipRegion(reusedRect, () => "OARO_OrderStation_LevelLabelTip".Translate(), uniqueId: 3786490);

        //左侧上部竖旗
        reusedRect = new(4f, 57f, 70f, 325f);
        GUI.DrawTexture(reusedRect, leftVerticalFlag);

        //左侧下部烛台
        reusedRect = new(14f, inRect.yMax - 284f, 50f, 284f);
        GUI.DrawTexture(reusedRect, leftCandlestick);

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
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
        Widgets.Label(reusedRect, "OARO_StationWin_Knight".Translate());

        reusedRect = new(innerRectX + 105f, innerRectY, 45f, 22f);
        Widgets.Label(reusedRect, "OARO_StationWin_KnightRole".Translate());

        reusedRect = new(innerRectX + 150f, innerRectY, 85f, 22f);
        Widgets.Label(reusedRect, "OARO_StationWin_MeditationFactor".Translate());

        reusedRect = new(innerRectX + 235f, innerRectY, 85f, 22f);
        Widgets.Label(reusedRect, "OARO_StationWin_MeditationPoint".Translate());

        reusedRect = new(innerRectX + 320f, innerRectY, 85f, 22f);
        Widgets.Label(reusedRect, "OARO_StationWin_KnightRank".Translate());

        Rect knightRect = new(innerRectX, titleRect.yMax + 2f, innerRect.width, 435f);
        if (ResidentKnightDrawers.Count <= 0)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(knightRect, "OARO_StationWin_NoResidentKnight".Translate().Colorize(Color.gray));
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
        Widgets.Label(reusedRect, "OARO_StationWin_ResidentKnightCeiling".Translate(ResidentPawnsManager.ResidentKnightCeiling.ToString()));

        Text.Font = GameFont.Small;
        reusedRect = new(innerRect.xMax - 190f, innerRectY + 462f, 190f, 24f);
        Widgets.Label(reusedRect, "OARO_StationWin_PreferredBuildingsLabel".Translate());

        Text.Font = GameFont.Tiny;
        reusedRect = new(innerRect.xMax - 190f, reusedRect.yMax + 2f, 190f, 95f);
        Widgets.LabelScrollable(reusedRect, PreferredBuildingsStr, ref scrollPosition_PreferredBuildingsStr);

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawBuffAndLevel(Rect inRect)
    {
        GUI.DrawTexture(inRect, middleBackground);
        inRect = inRect.ContractedBy(3f);

        Rect reusedRect = OARO_UIUtility.CenterRectOnX(inRect, inRect.y + 7f, 256f, 32f);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_StationWin_CurBuff".Translate());

        Rect effectRect = OARO_UIUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 270f);
        float entryX = effectRect.x;
        float entryY = effectRect.y;
        float entryHeight = 30f;

        Rect effectViewRect = effectRect;
        effectViewRect.width -= 16f;
        float entryWidth = effectViewRect.width;

        int effectCount = StationLevelEffectDescs.Count;
        int effectUseCount = Mathf.Max(9, effectCount);
        effectViewRect.height = effectUseCount * entryHeight;

        Widgets.BeginScrollView(effectRect, ref scrollPosition_Buff, effectViewRect);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < StationLevelEffectDescs.Count; i++)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if ((i & 1) == 0)
            {
                GUI.DrawTexture(entryRect, middleList_Dark);
            }
            Widgets.Label(entryRect, StationLevelEffectDescs[i]);
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
                    Widgets.Label(entryRect, "OARO_StationWin_EmptyBuff".Translate().Colorize(Color.gray));
                }
            }
        }
        Widgets.EndScrollView();


        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, effectRect.yMax + 12f, 287f, 3f);
        GUI.DrawTexture(reusedRect, middleCuttingLine);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = OARO_UIUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 256f, 32f);
        Widgets.Label(reusedRect, "OARO_StationWin_NextLevelNeed".Translate());

        Rect levelRect = OARO_UIUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 210f);
        levelRect.yMax = inRect.yMax;
        entryX = levelRect.x;
        entryY = levelRect.y;
        entryHeight = levelRect.height / 7f - 0.001f;

        Rect levelBuffViewRect = levelRect;
        levelBuffViewRect.width -= 16f;
        entryWidth = levelBuffViewRect.width;

        int levelCount = StationLevelRestrictionDescs.Count;
        int levelUseCount = Mathf.Max(7, levelCount);
        levelBuffViewRect.height = levelUseCount * entryHeight;

        Widgets.BeginScrollView(levelRect, ref scrollPosition_Level, levelBuffViewRect);
        Text.Font = GameFont.Small;

        int column = 0;
        if (levelCount == 0)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if (((++column) & 1) == 0)
            {
                GUI.DrawTexture(entryRect, middleList_Dark);
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(entryRect, "OARO_ReachMax_OrderStationLevel".Translate());
        }
        else
        {
            foreach ((string condition, bool isMet) restrictionDesc in StationLevelRestrictionDescs)
            {
                Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if (((++column) & 1) == 0)
                {
                    GUI.DrawTexture(entryRect, middleList_Dark);
                }
                entryRect.xMin += 8f;
                entryRect.xMax -= 8f;
                bool isMet = restrictionDesc.isMet;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(entryRect, restrictionDesc.condition.Colorize(isMet ? Color.green : ColorLibrary.RedReadable));
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(entryRect, isMet ? "✓".Colorize(Color.green) : "✗".Colorize(ColorLibrary.RedReadable));
            }
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        if (levelUseCount > column)
        {
            Rect entryRect = new(entryX, entryY, entryWidth, entryHeight);
            entryY += entryHeight;
            if (((++column) & 1) == 0)
            {
                GUI.DrawTexture(entryRect, middleList_Dark);
            }
            Widgets.Label(entryRect, "OARO_StationWin_EmptyLevelDesc".Translate().Colorize(Color.gray));

            while (column < levelUseCount)
            {
                Rect entryRectEmpty = new(entryX, entryY, entryWidth, entryHeight);
                entryY += entryHeight;
                if (((++column) & 1) == 0)
                {
                    GUI.DrawTexture(entryRectEmpty, middleList_Dark);
                }
            }
        }
        Widgets.EndScrollView();

        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private void DrawAroundKnightGroups(Rect inRect)
    {
        GUI.DrawTexture(inRect, rightBackground);
        inRect = inRect.ContractedBy(3f);

        Rect titleRect = OARO_UIUtility.CenterRectOnX(inRect, inRect.y, 416f, 40f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(titleRect.x, titleRect.y, 176f, 40f);
        Widgets.Label(reusedRect, "OARO_StationWin_GroupInfo".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 72f;
        Widgets.Label(reusedRect, "OARO_StationWin_BusyLevel".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 96f;
        Widgets.Label(reusedRect, "OARO_StationWin_Route".Translate());

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

                Widgets.Label(reusedRect, "OARO_StationWin_NoOtherAroundKnightGroup".Translate().Colorize(Color.gray));
            }
        }

        Widgets.EndScrollView();
        OberoniaAurea_Frame.UI.OAFrame_UIUtility.ResetTextStyleToDefault();
    }

    private bool DrawAroundKnightGroup(Rect inRect, AroundKnightGroup group, float successRate, int index)
    {
        if ((index & 1) == 0)
        {
            GUI.DrawTexture(inRect, rightList_Dark);
        }

        Rect reusedRect = OARO_UIUtility.CenterRectOnY(inRect, inRect.x + 2f, 55f, 60f);
        GUI.DrawTexture(reusedRect, aroundKnightGroupIcon, ScaleMode.ScaleToFit);

        Text.Anchor = TextAnchor.MiddleLeft;

        reusedRect = new(reusedRect.xMax + 8f, inRect.y + 24f, 105f, 32f);
        Widgets.LabelEllipses(reusedRect, group.Branch.Name);
        TooltipHandler.TipRegion(reusedRect, () => group.Branch.NameColored ?? string.Empty, uniqueId: 3047428);

        reusedRect = new(reusedRect.xMin + 16f, reusedRect.yMax, 97f, 32f);
        Widgets.LabelEllipses(reusedRect, "└  " + group.RatkinOrder.Name);
        TooltipHandler.TipRegion(reusedRect, () => group.RatkinOrder.NameColored ?? string.Empty, uniqueId: 3047429);

        Text.Anchor = TextAnchor.MiddleCenter;

        reusedRect = OARO_UIUtility.CenterRectOnY(inRect, inRect.x + 176f, 72f, 32f);
        Widgets.Label(reusedRect, $"OARO_AroundKnightGroup_{group.CurBusyLevel}".Translate());

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(reusedRect.xMax, inRect.y, 96f, inRect.height);
        Rect routeRect = reusedRect;
        routeRect.yMin += 4f;
        routeRect.yMax -= 4f;
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
            if (!String.IsNullOrEmpty(AroundGroupTipCache))
            {
                TooltipHandler.TipRegion(reusedRect, () => AroundGroupTipCache, 21345447);
            }
        }

        string buttonText = "OAFrame_Invite".Translate() + "\n" + successRate.ToStringPercent("F0");
        if (OARO_UIUtility.TextButtonImage(
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
                    typeName: nameof(Window_OrderStation),
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
            HashSet<ThingDef> allPreferredBuildings = OrderStationUtility.GetAllResidentKnightPreferredBuildingDefs(OrderStationHandler.Instance.OrderStationRoom);

            foreach (ThingDef def in OrderDefDatabase.AllKnightPreferredBuildings)
            {
                sb.AppendLine(def.label.Colorize(allPreferredBuildings.Contains(def) ? Color.white : Color.gray));
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "get preferred buildings description",
                typeName: nameof(Window_OrderStation),
                methodName: nameof(GetPreferredBuildingsStr),
                needStackTrace: true);
            return KeyLibrary_Misc.ErrorTipWithColor;
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_MainBackground");

    private static readonly Texture2D topRibbon = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_TopRibbon");

    private static readonly Texture2D leftVerticalFlag = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_LeftVerticalFlag");
    private static readonly Texture2D leftCandlestick = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_LeftCandlestick");

    private static readonly Texture2D leftBackground = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_LeftBackground");

    private static readonly Texture2D middleBackground = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_MiddleBackground");
    private static readonly Texture2D middleCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_MiddleCuttingLine");
    private static readonly Texture2D middleList_Dark = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_MiddleList_Dark");

    private static readonly Texture2D rightBackground = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_RightBackground");
    private static readonly Texture2D rightList_Dark = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_RightList_Dark");

    private static readonly Texture2D aroundKnightGroupIcon = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_AroundKnightGroupIcon");
    private static readonly Texture2D aroundKnightGroupButton = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_AroundKnightGroupButton");
    private static readonly Texture2D aroundKnightGroupButton_Down = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_AroundKnightGroupButton_Down");

    private static readonly Texture2D bigCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderStation/OARO_BigCuttingLine");

}