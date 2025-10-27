using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MainTabWindow_InteractionKnights : MainTabWindow
{
    public override Vector2 InitialSize => new(1462f, 919f);
    public override Vector2 RequestedTabSize => new(1462f, 919f);
    protected override float Margin => 0f;

    private Vector2 scrollPosition_Buff;
    private Vector2 scrollPosition_Level;
    private Vector2 scrollPosition_AroundGroups;

    private readonly int curOrderHallLevel;
    private readonly Texture2D topShieldTexture;

    private List<(AroundKnightGroup, float)> aroundKnightGroups = [];
    private int aroundGroupTipIndex = -1;
    private string aroundGroupTipCache = string.Empty;

    public MainTabWindow_InteractionKnights()
    {
        forcePause = true;
        draggable = false;
        resizeable = false;
        doCloseButton = false;
        doCloseX = false;

        layer = WindowLayer.Dialog;  //窗体层级
        doWindowBackground = false; //绘制泰南的界面背景
        drawShadow = false; //绘制主体界面阴影

        //声音
        //注：用的通讯台声音
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;

        curOrderHallLevel = Mathf.Max(1, OrderHallHandler.OrderHallLevel);
        topShieldTexture = new CachedTexture($"UI/InteractionKnights/OARO_TopShield_{curOrderHallLevel}").Texture;

        RecacheAroundKnightGroups();
    }

    public override void PreOpen()
    {
        base.PreOpen();
        RecacheAroundKnightGroups();
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect reusedRect = default;
        Rect mainRect = new(37f, 49f, 1388f, 862f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(4f);
        float mainInnerRectY = mainInnerRect.yMin;

        float infoRectY = mainInnerRectY + 184f;
        float infoRectHeight = 591f;

        //中部主要区域
        Rect middleRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, infoRectY, 322f, infoRectHeight);
        DrawBuffAndLevel(middleRect);

        //左|中分割线
        reusedRect = OARO_WindowUtility.CenterRectOnY(mainInnerRect, middleRect.xMin - (24f + 5f), 5f, 707f);
        GUI.DrawTexture(reusedRect, bigCuttingLine);

        //左侧主要区域（角色框）
        Rect leftRect = new(reusedRect.xMin - (19f + 426f), infoRectY, 426f, infoRectHeight);
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
        Widgets.Label(reusedRect, "OARO_AroundKnightGroup".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        //顶部绶带
        reusedRect = new(37f, 46f, 1388f, 104f);
        GUI.DrawTexture(reusedRect, topRibbon);
        //顶部盾徽
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainRect, 0f, 215f, 211f);
        //盾徽绘制逻辑（未完成）
        GUI.DrawTexture(reusedRect, topShieldTexture);

        //左侧上部竖旗
        reusedRect = new(4f, 57f, 70f, 325f);
        GUI.DrawTexture(reusedRect, leftVerticalFlag);

        //左侧下部烛台
        reusedRect = new(14f, inRect.yMax - 284f, 50f, 284f);
        GUI.DrawTexture(reusedRect, leftCandlestick);
    }

    private void DrawResidentKnights(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftStarBorder);
        inRect = inRect.ContractedBy(3f);
    }

    private void DrawBuffAndLevel(Rect inRect)
    {
        GUI.DrawTexture(inRect, middleBackground);
        inRect = inRect.ContractedBy(3f);

        Rect reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.y + 7f, 256f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(reusedRect, "OARO_CurBuff".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect buffRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 270f);
        int buffCount = 0;
        int buffUseCount = Mathf.Max(18, buffCount);
        float buffViewHeight = buffUseCount * 30f;
        Rect buffViewRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 300f, buffViewHeight);

        Widgets.BeginScrollView(buffRect, ref scrollPosition_Buff, buffViewRect);
        float entryX = buffViewRect.x;
        float entryY = buffViewRect.y;
        Rect entryRect;

        if (buffUseCount > buffCount)
        {
            for (int i = buffCount; i < buffUseCount; i++)
            {
                entryRect = new(entryX, entryY, 302f, 30f);
                entryY += 30f;
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
        Widgets.Label(reusedRect, "OARO_NextLevelNeed".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect levelRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 316f, 210f);
        int levelCount = 0;
        int levelUseCount = Mathf.Max(18, levelCount);
        float levelViewHeight = levelUseCount * 30f;
        Rect levelBuffViewRect = OARO_WindowUtility.CenterRectOnX(inRect, reusedRect.yMax + 7f, 300f, levelViewHeight);

        Widgets.BeginScrollView(levelRect, ref scrollPosition_Level, levelBuffViewRect);
        entryX = levelBuffViewRect.x;
        entryY = levelBuffViewRect.y;

        if (levelUseCount > buffCount)
        {
            for (int i = buffCount; i < levelUseCount; i++)
            {
                entryRect = new(entryX, entryY, 302f, 30f);
                entryY += 30f;
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

        Rect innerRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.y, 416f, 40f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(innerRect.x, innerRect.y, 176f, 40f);
        Widgets.Label(reusedRect, "OARO_GroupInfo".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 72f;
        Widgets.Label(reusedRect, "OARO_BusyLevel".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 96f;
        Widgets.Label(reusedRect, "OARO_Route".Translate());

        reusedRect.xMin = reusedRect.xMax;
        reusedRect.xMax = reusedRect.xMin + 64f;
        Widgets.Label(reusedRect, "OARO_InvitationSuccessRate".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        Rect groupRect = new(innerRect.x, innerRect.yMax, 424f, 535f);
        int groupCount = aroundKnightGroups.Count;
        int maxCount = Mathf.Max(10, groupCount);
        float viewHeight = maxCount * 107f;
        Rect viewRect = new(innerRect.x, innerRect.yMax, 408f, viewHeight);
        Widgets.BeginScrollView(groupRect, ref scrollPosition_AroundGroups, viewRect);

        float entryX = viewRect.x;
        float entryY = viewRect.y;

        for (int i = 0; i < groupCount; i++)
        {
            reusedRect = new(entryX, entryY, 408f, 107f);
            entryY += 107f;

            if (DrawAroundKnightGroup(reusedRect, aroundKnightGroups[i].Item1, aroundKnightGroups[i].Item2, i))
            {
                RecacheAroundKnightGroups();
                Widgets.EndScrollView();
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }
        }

        if (maxCount > groupCount)
        {
            for (int i = groupCount; i < maxCount; i++)
            {
                reusedRect = new(entryX, entryY, 408f, 107f);
                entryY += 107f;

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
            if (index != aroundGroupTipIndex)
            {
                aroundGroupTipIndex = index;
                GlobalOrderInteractionUtility.InvitationAcceptanceChance(group, resultOnly: false, out aroundGroupTipCache);
            }
            if (!aroundGroupTipCache.NullOrEmpty())
            {
                TooltipHandler.TipRegion(reusedRect, () => aroundGroupTipCache, 21345447);
            }
        }

        string buttonText = "OARO_Invite".Translate() + "\n";
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: buttonText + successRate.ToStringPercent("F0"),
            baseTex: aroundKnightGroupButton,
            downTex: aroundKnightGroupButton_Down))
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: true, canBeSpace: false);
            GlobalOrderInteractionUtility.InviteAroundKnightGroup(group, map);
            return true;
        }
        return false;
    }

    private void RecacheAroundKnightGroups()
    {
        aroundGroupTipIndex = -1;
        aroundGroupTipCache = string.Empty;
        aroundKnightGroups.Clear();
        IReadOnlyList<AroundKnightGroup> tempGroups = AroundKnightGroupsManager.AroundKnightGroups;
        for (int i = 0; i < tempGroups.Count; i++)
        {
            float successRate = GlobalOrderInteractionUtility.InvitationAcceptanceChance(tempGroups[i], resultOnly: true, out _);
            aroundKnightGroups.Add((tempGroups[i], successRate));
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_MainBackground");

    private static readonly Texture2D topRibbon = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_TopRibbon");

    private static readonly Texture2D leftVerticalFlag = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_LeftVerticalFlag");
    private static readonly Texture2D leftCandlestick = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_LeftCandlestick");

    private static readonly Texture2D leftStarBorder = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_LeftStarBorder");

    private static readonly Texture2D middleBackground = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_MiddleBackground");
    private static readonly Texture2D middleCuttingLine = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_MiddleCuttingLine");
    private static readonly Texture2D middleList_Dark = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_MiddleList_Dark");

    private static readonly Texture2D rightBackground = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_RightBackground");
    private static readonly Texture2D rightList_Dark = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_RightList_Dark");

    private static readonly Texture2D aroundKnightGroupIcon = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_AroundKnightGroupIcon");
    private static readonly Texture2D aroundKnightGroupButton = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_AroundKnightGroupButton");
    private static readonly Texture2D aroundKnightGroupButton_Down = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_AroundKnightGroupButton_Down");

    private static readonly Texture2D bigCuttingLine = ContentFinder<Texture2D>.Get("UI/InteractionKnights/OARO_BigCuttingLine");

}