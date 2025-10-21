using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchSummaryCacheEntry
{
    public readonly Branch Branch;

    public string SquadName = string.Empty;
    public string BaseSiteName = string.Empty;
    public float Distance = -1f;
    public bool OnTask;
    public int CurAllCrewCount;
    public float Potency;

    public Texture2D HonorIcon;
    public Texture2D HonorStripSmall;
    public Texture2D HonorBackgroundSmall;
    public Texture2D HonorDecorationSmall;

    public BranchSummaryCacheEntry() { }

    public BranchSummaryCacheEntry(Branch branch, Map map)
    {
        Branch = branch;
        SquadName = branch.Squad.Name;

        if (branch.BaseSite is INameableWorldObject nameSite)
        {
            BaseSiteName = nameSite.Name;
        }
        else
        {
            BaseSiteName = branch.BaseSite.Label;
        }

        Distance = branch.DistanceTo(map.Tile);
        CurAllCrewCount = branch.Squad.AllCrewCountInt;
        HonorIcon = branch.HonorProperties?.IconTexture;

        BranchMedalRecord.BranchMedalType primaryMedal = branch.MedalHandler.PrimaryMedal;
        if (primaryMedal != BranchMedalRecord.BranchMedalType.None)
        {
            HonorStripSmall = new CachedTexture($"UI/OrderBranches/OARO_HonorStripSmall_{primaryMedal}").Texture;
            HonorBackgroundSmall = new CachedTexture($"UI/OrderBranches/OARO_HonorBackgroundSmall_{primaryMedal}").Texture;
            HonorDecorationSmall = new CachedTexture($"UI/OrderBranches/OARO_HonorDecorationSmall_{primaryMedal}").Texture;
        }
    }
}

public class BranchInfoCacheEntry : BranchSummaryCacheEntry
{
    public bool HasSupportAuthority;

    public string FriendlyExpireDateStr = string.Empty;
    public float FriendlyProcess;

    public int CommanderCeiling;
    public int CrewCeiling;
    public float MemberRecoveryRate;
    public int BombardSupportCeiling;

    public Texture2D HonorExpandIcon;
    public Texture2D HonorStrip;
    public Texture2D HonorBackground;
    public Texture2D HonorDecoration;
    public Texture2D MedalBackground;

    public BranchInfoCacheEntry() : base() { }

    public BranchInfoCacheEntry(Branch branch, Map map) : base(branch, map)
    {
        HasSupportAuthority = branch.EffectTags.HasTag(KeyLibrary_EffectTag.SupportAuthority);

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            FriendlyProcess = Mathf.Clamp01(branch.FriendlyExpiredTick / 40f * 60000f);
            FriendlyExpireDateStr = GenDate.SeasonDateStringAt(GenTicks.TicksAbs + branch.FriendlyExpiredTick, Find.WorldGrid.LongLatOf(map.Tile));
        }
        HonorExpandIcon = branch.HonorProperties?.ExpandingIconTexture;

        BranchMedalRecord.BranchMedalType primaryMedal = branch.MedalHandler.PrimaryMedal;
        if (primaryMedal != BranchMedalRecord.BranchMedalType.None)
        {
            HonorStrip = new CachedTexture($"UI/OrderBranches/OARO_HonorStrip_{primaryMedal}").Texture;
            HonorBackground = new CachedTexture($"UI/OrderBranches/OARO_HonorBackground_{primaryMedal}").Texture;
            HonorDecoration = new CachedTexture($"UI/OrderBranches/OARO_HonorDecoration_{primaryMedal}").Texture;
            MedalBackground = new CachedTexture($"UI/OrderBranches/OARO_MedalBackground_{primaryMedal}").Texture;
        }

        CommanderCeiling = (int)branch.Squad.CommanderCeiling;
        CrewCeiling = (int)branch.Squad.MemberCeiling + CommanderCeiling;

        MemberRecoveryRate = branch.GetStatValue(BranchStatDefOf.OARO_SquadMemberRecoveryRate);
        BombardSupportCeiling = (int)branch.GetStatValue(BranchStatDefOf.OARO_BombardSupportCeiling);
    }

}



public class MainTabWindow_OrderBranches : MainTabWindow
{
    public override Vector2 InitialSize => new(1627f, 944f);
    public override Vector2 RequestedTabSize => new(1627f, 944f);

    private Vector2 scrollPosition_Squads;

    private Map map;

    private int selBranchIndex;
    private Branch selBranch;
    private BranchInfoCacheEntry selBranchInfo;

    private List<BranchSummaryCacheEntry> branchSummaryCaches = [];

    public MainTabWindow_OrderBranches()
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


    }

    public override void PreOpen()
    {
        base.PreOpen();
        map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
        RatkinOrder ratkinOrder = RatkinOrderManager.Instance.AllRatkinOrders[0];

        branchSummaryCaches = [];
        foreach (Branch branch in ratkinOrder.BranchManager.AllBranches)
        {
            branchSummaryCaches.Add(new BranchSummaryCacheEntry(branch, map));
        }

        SelectBranch(branchSummaryCaches[0].Branch, 0);
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
        Rect mainRect = OARO_WindowUtility.CenterRect(inRect, 1547f, 904f);
        GUI.DrawTexture(mainRect, mainBackground);

        Rect mainInnerRect = mainRect.ContractedBy(3f);
        float mainInnerRectY = mainInnerRect.yMin;

        float rectY = mainInnerRectY + 205f;
        float rectHeight = 657f;

        //顶部缎带
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 4f, 1568f, 137f);
        GUI.DrawTexture(reusedRect, topRibbon);

        //顶部标题框
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 12f, 322f, 90f);
        GUI.DrawTexture(reusedRect, topTitleground);

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, mainInnerRectY + 6f, 236f, 78f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_BranchWindows".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        //分部名称
        reusedRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, mainInnerRectY + 150f, 322f, 48f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, selBranchInfo.SquadName);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        Rect middleRect = OARO_WindowUtility.CenterRectOnX(mainInnerRect, rectY, 583f, rectHeight);
        if (DrawMiddleRect(middleRect))
        {
            return;
        }

        //左|中分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(middleRect, middleRect.x - (32f + 3f), 3f, 717f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);

        Rect leftRect = new(reusedRect.xMin - (12f + 415f), rectY, 415f, rectHeight);
        DrawLeftRect(leftRect);

        //中|右分界线
        reusedRect = OARO_WindowUtility.CenterRectOnY(middleRect, middleRect.xMax + 32f, 3f, 717f);
        GUI.DrawTexture(reusedRect, verticalCuttingLine);


        Rect rightRect = new(reusedRect.xMax + 32f, rectY, 352f, rectHeight);
        //小队科技
        reusedRect = OARO_WindowUtility.CenterRectOnX(rightRect, mainInnerRectY + 150f, 322f, 48f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_SquadTech".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        DrawRightRect(rightRect);


        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private bool DrawMiddleRect(Rect inRect)
    {
        float inRectX = inRect.x;
        float inRectY = inRect.y;
        float frameWidth = 2f;
        float rectWidth = inRect.width;
        float rectHeight = 150f;

        Rect reusedRect;

        //名称|内容分割线
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY, 579f, 3f);
        GUI.DrawTexture(reusedRect, middleCuttingLine);

        reusedRect = new(inRectX, reusedRect.yMax + 10f, 23f, 32f);
        GUI.DrawTexture(reusedRect, branchBaseSiteIcon);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 8f, 80f, 32f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(reusedRect, selBranchInfo.BaseSiteName);

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 32f, 60f, 32f);
        Text.Anchor = TextAnchor.MiddleCenter;
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "Check".Translate(), middleCheckButton, middleCheckButton_Down))
        {
            Close();
            return true;
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRect.xMax - 180f, 90f, 32f);
        Widgets.Label(reusedRect, "OARO_BranchSiteDistance_Prefix".Translate());

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, inRect.xMax - 90f, 90f, 32f);
        if (selBranch is not null)
        {
            Widgets.Label(reusedRect, "OARO_BranchSiteDistance_Suffix".Translate(selBranchInfo.Distance.ToString("F0")));
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_BranchSiteDistance_SuffixN".Translate());
        }

        Text.Anchor = TextAnchor.MiddleCenter;

        Rect upRect = new(inRectX, inRectY + 80f, rectWidth, rectHeight);
        GUI.DrawTexture(upRect, middleUpBackground, ScaleMode.ScaleToFit);
        Rect upInnerRect = upRect.ContractedBy(frameWidth);

        //上部丝带
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY + 70f, 629f, 101f);
        GUI.DrawTexture(reusedRect, middleUpRibbon);

        //上左侧颜色条
        reusedRect = OARO_WindowUtility.CenterRectOnY(upInnerRect, upInnerRect.x, 6f, 144f);
        if (selBranchInfo.HonorStrip is not null)
        {
            GUI.DrawTexture(reusedRect, selBranchInfo.HonorStrip);
        }

        //上左侧部分
        Rect areaRect = new(reusedRect.xMax, upInnerRect.y, 245f, upInnerRect.height);
        reusedRect = OARO_WindowUtility.CenterRectOnY(areaRect, areaRect.x, 240f, areaRect.height - 5f);
        if (selBranchInfo.HonorBackground is not null)
        {
            GUI.DrawTexture(reusedRect, selBranchInfo.HonorBackground, ScaleMode.ScaleToFit);
        }
        if (selBranchInfo.HonorDecoration is not null)
        {
            reusedRect = OARO_WindowUtility.CenterRect(areaRect, 230f, 130f);
            GUI.DrawTexture(reusedRect, selBranchInfo.HonorDecoration, ScaleMode.ScaleToFit);
        }
        if (selBranchInfo.HonorExpandIcon is not null)
        {
            reusedRect = OARO_WindowUtility.CenterRect(areaRect, 190f, 107f);
            GUI.DrawTexture(reusedRect, selBranchInfo.HonorExpandIcon, ScaleMode.ScaleToFit);
        }
        else
        {
            reusedRect = OARO_WindowUtility.CenterRect(areaRect, 62f, 68f);
            GUI.DrawTexture(reusedRect, middleUpGeneralSquadIcon, ScaleMode.ScaleToFit);
        }

        //上右侧部分
        areaRect = Rect.MinMaxRect(areaRect.xMax + 2f, upInnerRect.yMin, upInnerRect.xMax, upInnerRect.yMax);
        if (selBranchInfo.MedalBackground is not null)
        {
            GUI.DrawTexture(areaRect, selBranchInfo.MedalBackground, ScaleMode.ScaleToFit);
        }

        reusedRect = OARO_WindowUtility.CenterRect(areaRect, 300f, 112f);
        GUI.DrawTexture(reusedRect, middleUpPeristele);

        //中部
        Rect middleRect = new(inRectX, upRect.yMax + 32f, rectWidth, rectHeight);
        GUI.DrawTexture(middleRect, middleMiddleBackground, ScaleMode.ScaleToFit);
        Rect middleInnerRect = middleRect.ContractedBy(frameWidth);

        float middleUpHeight = 106f;
        float middleBottomHeight = 38f;
        //中部左上区域
        areaRect = new(middleInnerRect.x, middleInnerRect.y, 260f, middleUpHeight);
        reusedRect = new(areaRect.x + 8f, areaRect.y + 6f, 72f, 24f);
        Widgets.Label(reusedRect, "OARO_CurSquadRelation".Translate());

        TaggedString relation;
        float friendlyProcess;
        TaggedString friendlyExpireDate;

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, areaRect.y + 40f, 43f, 51f);
        if (selBranch?.IsBranchOfType(Branch.BranchType.Friendly) ?? false)
        {
            GUI.DrawTexture(reusedRect, bigFriendlyIcon, ScaleMode.ScaleToFit);

            relation = "OARO_Friendly".Translate().Colorize(Color.green);
            friendlyProcess = selBranchInfo.FriendlyProcess;
            friendlyExpireDate = "OARO_FriendlyExpireDate".Translate(selBranchInfo.FriendlyExpireDateStr);
        }
        else
        {
            GUI.DrawTexture(reusedRect, bigStrangeIcon, ScaleMode.ScaleToFit);

            relation = "OARO_Strange".Translate();
            friendlyProcess = 0f;
            friendlyExpireDate = "OARO_FriendlyExpireDateN".Translate();
        }

        reusedRect = new(middleInnerRect.x + 136f, middleInnerRect.y + 4f, 54f, 32f);
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, relation);
        Text.Font = GameFont.Small;

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, areaRect.y + 40f, 120f, 24f);
        Widgets.FillableBar(reusedRect, friendlyProcess, middleMiddleBarHighlightTex, middleMiddleEmptyBarTex, doBorder: true);

        reusedRect = new(reusedRect.x, reusedRect.yMax + 8f, 120f, 24f);
        Widgets.Label(reusedRect, friendlyExpireDate);

        //中部左下区域
        areaRect = new(areaRect.x, middleInnerRect.yMax - middleBottomHeight, 260f, middleBottomHeight);
        reusedRect = areaRect;
        reusedRect.xMax = areaRect.x + 100f;
        Widgets.Label(reusedRect, "OARO_ColonistsMember".Translate());

        reusedRect.xMin = reusedRect.xMax + 8f;
        reusedRect.xMax = areaRect.xMax - 2f;
        if (OARO_WindowUtility.TextButtonImage(reusedRect, "OARO_ClickToAdd".Translate(), middleClickToAddButton, middleClickToAddButton_Down))
        {

        }

        //中部中上区域
        areaRect = new(areaRect.xMax + 2f, middleInnerRect.y, 128f, middleUpHeight);
        reusedRect = new(areaRect.x, areaRect.y + 4f, 128f, 24f);
        Widgets.Label(reusedRect, "OARO_BranchSupplyState".Translate());

        if (selBranch is not null)
        {
            switch (selBranch.Supply)
            {
                case < 0.2f:
                    reusedRect = OARO_WindowUtility.CenterRectOnX(areaRect, reusedRect.yMax + 26f, 20f, 19f);
                    GUI.DrawTexture(reusedRect, branchSupplyLack);
                    reusedRect = new(areaRect.x + 10f, areaRect.yMax - (24f + 2f), 64f, 24f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(reusedRect, "OARO_BranchSupply_Lack".Translate().Colorize(ColorLibrary.Orange));
                    break;

                case < 0.8f:
                    reusedRect = OARO_WindowUtility.CenterRectOnX(areaRect, reusedRect.yMax + 6f, 62f, 39f);
                    GUI.DrawTexture(reusedRect, branchSupplyJust);
                    reusedRect = new(areaRect.x + 10f, areaRect.yMax - (24f + 2f), 64f, 24f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(reusedRect, "OARO_BranchSupply_Just".Translate().Colorize(Color.yellow));
                    break;

                default:
                    reusedRect = OARO_WindowUtility.CenterRectOnX(areaRect, reusedRect.yMax + 6f, 104f, 39f);
                    GUI.DrawTexture(reusedRect, branchSupplyEnough);
                    reusedRect = new(areaRect.x + 10f, areaRect.yMax - (24f + 2f), 64f, 24f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(reusedRect, "OARO_BranchSupply_Enough".Translate().Colorize(Color.green));
                    break;
            }
            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(areaRect.xMax - (10f + 64f), areaRect.yMax - (24f + 2f), 64f, 24f);
            Widgets.Label(reusedRect, selBranch.Supply.ToStringPercent("F0"));
            Text.Anchor = TextAnchor.MiddleCenter;
        }
        else
        {
            reusedRect = new(areaRect.x + 10f, areaRect.yMax - (24f + 2f), 64f, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(reusedRect, "--");
            Text.Anchor = TextAnchor.MiddleRight;
            reusedRect = new(areaRect.xMax - (10f + 64f), areaRect.yMax - (24f + 2f), 64f, 24f);
            Widgets.Label(reusedRect, "--%");
            Text.Anchor = TextAnchor.MiddleCenter;
        }


        //中部中下区域
        areaRect = new(areaRect.x, middleInnerRect.yMax - middleBottomHeight, 128f, middleBottomHeight);
        if (OARO_WindowUtility.ButtonImage(areaRect, middleSilverButton, middleSilverButton_Down))
        {

        }
        reusedRect = new(areaRect.x + 16f, areaRect.y + 8f, 40f, 24f);

        reusedRect = new(reusedRect.xMax + 8f, areaRect.y + 8f, 40f, 24f);
        Widgets.Label(reusedRect, ThingDefOf.Silver.label);

        //中部右上区域
        areaRect = new(areaRect.xMax + 2f, middleInnerRect.y, 185f, middleUpHeight);
        reusedRect = new(areaRect.x + 10f, areaRect.y + 10f, 21f, 26f);
        GUI.DrawTexture(reusedRect, middleMemberIcon);

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 10f, 135f, 24f);
        if (selBranch is not null)
        {
            Widgets.Label(reusedRect, "OARO_MemberCountInfo".Translate(selBranch.Squad.AllCrewCountInt, selBranchInfo.CrewCeiling));
            reusedRect.yMin = reusedRect.yMax;
            reusedRect.yMax += 24f;
            Widgets.Label(reusedRect, "OARO_MemberRecoveryInfo".Translate(selBranchInfo.MemberRecoveryRate.ToStringWithSign("F1"))
                                                               .Colorize(selBranchInfo.MemberRecoveryRate < 0 ? ColorLibrary.RedReadable : Color.green));
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_MemberCountInfoN".Translate());
            reusedRect.yMin = reusedRect.yMax;
            reusedRect.yMax += 24f;
            Widgets.Label(reusedRect, "OARO_MemberRecoveryInfoN".Translate());
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        reusedRect = new(areaRect.x, areaRect.yMax - 39f, 186f, 39f);
        if (OARO_WindowUtility.ButtonImage(reusedRect, middleSilverButton, middleSilverButton_Down))
        {

        }

        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, areaRect.x + 50f, 26f, 23f);
        GUI.DrawTexture(reusedRect, recommendationIcon);
        reusedRect = OARO_WindowUtility.CenterRectOnY(reusedRect, reusedRect.xMax + 6f, 64f, 24f);
        Widgets.Label(reusedRect, "OARO_RecommendationLetter".Translate());

        //中部右下区域
        areaRect = new(areaRect.x, middleInnerRect.yMax - middleBottomHeight, 186f, middleBottomHeight);
        reusedRect = new(areaRect.x + 4f, areaRect.y + 8f, 70f, 24f);
        Widgets.Label(reusedRect, "OARO_CommanderCountInfo".Translate());

        reusedRect = OARO_WindowUtility.CenterRectOnY(areaRect, reusedRect.xMax + 4f, 29f, 27f);
        GUI.DrawTexture(reusedRect, middleCommanderIcon);

        reusedRect = OARO_WindowUtility.CenterRectOnY(areaRect, areaRect.xMax - 54f, 50f, 24f);
        if (selBranch is not null)
        {
            Widgets.Label(reusedRect, "OARO_CommanderCountNum".Translate(selBranch.Squad.CommanderCountInt, selBranchInfo.CommanderCeiling));
        }
        else
        {
            Widgets.Label(reusedRect, "OARO_CommanderCountNumN".Translate());
        }

        //下部区域
        Rect bottomRect = new(inRectX, middleRect.yMax + 32f, rectWidth, rectHeight);

        return false;
    }

    private void DrawLeftRect(Rect inRect)
    {
        GUI.DrawTexture(inRect, leftBackground);
        inRect = inRect.ContractedBy(2f);



        float viewHeight = branchSummaryCaches.Count * 91f;
        Rect listRect = inRect;
        listRect.width = 393f;
        listRect.height = viewHeight;


        Widgets.BeginScrollView(inRect, ref scrollPosition_Squads, listRect);
        float entryX = listRect.x;
        float entryY = listRect.y;
        Rect entryRect;

        Text.Anchor = TextAnchor.MiddleCenter;
        for (int i = 0; i < branchSummaryCaches.Count; i++)
        {
            entryRect = new(entryX, entryY, 393f, 91f);
            entryY += 91;

            DrawSquadEntry(entryRect, branchSummaryCaches[i], i);
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.EndScrollView();
    }

    private void DrawRightRect(Rect inRect)
    {
        float inRectX = inRect.x;
        float inRectY = inRect.y;

        Rect reusedRect;

        //名称|内容分割线
        reusedRect = OARO_WindowUtility.CenterRectOnX(inRect, inRectY, 347f, 3f);
        GUI.DrawTexture(reusedRect, rightCuttingLine);

        reusedRect = new(inRectX + 30f, reusedRect.yMax + 10f, 35f, 24f);
        GUI.DrawTexture(reusedRect, rightSupportSquadIcon);


        reusedRect.xMin = reusedRect.xMax + 2f;
        reusedRect.xMax = inRect.xMin + 180f;
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(reusedRect, "OARO_SupportSquadNum".Translate());

        reusedRect = new(inRect.xMax - 64f, reusedRect.y, 32f, 24f);
        Text.Anchor = TextAnchor.LowerRight;
        Widgets.Label(reusedRect, $"× {5}");

        reusedRect = new(reusedRect.x - (12f + 26f), reusedRect.y, 26f, 24f);
        GUI.DrawTexture(reusedRect, recommendationIcon);

        Text.Anchor = TextAnchor.MiddleCenter;

        Rect mainRect = OARO_WindowUtility.CenterRectOnX(inRect, inRect.yMax - 597f, 352f, 597f);
        GUI.DrawTexture(mainRect, rightBackground);
        mainRect = mainRect.ContractedBy(2f);



    }

    private void DrawSquadEntry(Rect inRect, BranchSummaryCacheEntry entry, int index)
    {
        if (Mouse.IsOver(inRect))
        {
            Widgets.DrawHighlight(inRect);
        }
        inRect = inRect.ContractedBy(2f);
        if (Widgets.ButtonInvisible(inRect))
        {
            SelectBranch(entry.Branch, index);
        }

        Rect reusedRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.x, 6f, 87f);
        if (entry.HonorStripSmall is not null)
        {
            GUI.DrawTexture(reusedRect, entry.HonorStripSmall);
        }

        Rect leftRect = new(inRect.x + 6f, inRect.y, 224f, inRect.height);
        if (entry.HonorDecorationSmall is not null)
        {
            reusedRect = leftRect.ContractedBy(10f);
            GUI.DrawTexture(reusedRect, entry.HonorDecorationSmall, ScaleMode.ScaleToFit);
        }

        if (entry.HonorBackgroundSmall is not null)
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(leftRect, leftRect.x, 225f, 87f);
            GUI.DrawTexture(reusedRect, entry.HonorBackgroundSmall);
        }

        if (entry.HonorIcon is not null)
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(leftRect, leftRect.x + 10f, 90f, 65f);
            GUI.DrawTexture(reusedRect, entry.HonorIcon, ScaleMode.ScaleToFit);
        }
        else
        {
            reusedRect = OARO_WindowUtility.CenterRectOnY(leftRect, leftRect.x + 38f, 34f, 37f);
            GUI.DrawTexture(reusedRect, leftGeneralSquadIcon, ScaleMode.ScaleToFit);
        }

        Rect squadNameRect = new(leftRect.x + 100f, leftRect.y + 4f, 128f, 22f);
        Widgets.Label(squadNameRect, entry.SquadName);

        reusedRect = new(squadNameRect.x, squadNameRect.yMax + 4f, 25f, 30f);
        string relation;
        if (entry.Branch?.IsBranchOfType(Branch.BranchType.Friendly) ?? false)
        {
            GUI.DrawTexture(reusedRect, smallFriendlyIcon, ScaleMode.ScaleToFit);
            relation = "OARO_Friendly".Translate().Colorize(Color.green);
        }
        else
        {
            GUI.DrawTexture(reusedRect, smallStrangeIcon, ScaleMode.ScaleToFit);
            relation = "OARO_Strange".Translate();
        }

        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax + 3f, 40f, 20f);
        Widgets.Label(reusedRect, relation);

        reusedRect = new(squadNameRect.xMax - 30f, squadNameRect.yMax + 4f, 30f, 30f);

        Text.Anchor = TextAnchor.MiddleRight;
        reusedRect = OARO_WindowUtility.CenterRectOnX(reusedRect, reusedRect.yMax + 3f, 60f, 20f);
        Widgets.Label(reusedRect, entry.Branch.CurWorkState);

        Text.Anchor = TextAnchor.MiddleLeft;
        Rect rightRect = Rect.MinMaxRect(leftRect.xMax + 10f, inRect.yMin, inRect.xMax, inRect.yMax);
        reusedRect = new(rightRect.x, rightRect.y, rightRect.width, 29f);
        Widgets.Label(reusedRect, "OARO_CurAllCrewCount".Translate(entry.CurAllCrewCount));
        reusedRect = new(rightRect.x, reusedRect.yMax, rightRect.width, 29f);
        Widgets.Label(reusedRect, relation);
        reusedRect = new(rightRect.x, reusedRect.yMax, rightRect.width, 29f);
        string supplyState = "OARO_BranchSupplyState".Translate() + "  ";
        supplyState += selBranch.Supply switch
        {
            < 0.2f => "OARO_BranchSupply_Lack".Translate().Colorize(ColorLibrary.Orange),
            < 0.8f => "OARO_BranchSupply_Just".Translate().Colorize(Color.yellow),
            _ => "OARO_BranchSupply_Enough".Translate().Colorize(Color.green),
        };
        Widgets.Label(reusedRect, supplyState);
    }

    private void SelectBranch(Branch branch, int index)
    {
        if (branch is null)
        {
            DeselectBranch();
            return;
        }
        selBranch = branch;
        selBranchIndex = index;
        selBranchInfo = new(branch, map);
    }

    private void DeselectBranch()
    {
        selBranch = null;
        selBranchIndex = -1;
        selBranchInfo = new();
    }

    public override void PreClose()
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        base.PreClose();
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MainBackground");

    private static readonly Texture2D topTitleground = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_TopTitleground");
    private static readonly Texture2D topRibbon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_TopRibbon");

    private static readonly Texture2D middleCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleCuttingLine");
    private static readonly Texture2D middleCheckButton = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleCheckButton");
    private static readonly Texture2D middleCheckButton_Down = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleCheckButton_Down");

    private static readonly Texture2D middleUpRibbon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleUpRibbon");
    private static readonly Texture2D middleUpBackground = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleUpBackground");
    private static readonly Texture2D middleUpPeristele = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleUpPeristele");
    private static readonly Texture2D middleUpGeneralSquadIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleUpGeneralSquadIcon");

    private static readonly Texture2D middleMiddleBackground = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleMiddleBackground");
    private static readonly Texture2D middleMiddleBarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));
    private static readonly Texture2D middleMiddleEmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
    private static readonly Texture2D middleClickToAddButton = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleClickToAddButton");
    private static readonly Texture2D middleClickToAddButton_Down = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleClickToAddButton_Down");
    private static readonly Texture2D middleSilverButton = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleSilverButton");
    private static readonly Texture2D middleSilverButton_Down = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleSilverButton_Down");
    private static readonly Texture2D middleMemberIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleMemberIcon");
    private static readonly Texture2D middleCommanderIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_MiddleCommanderIcon");

    private static readonly Texture2D leftBackground = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_LeftBackground");
    private static readonly Texture2D leftGeneralSquadIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_LeftGeneralSquadIcon");

    private static readonly Texture2D rightBackground = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_RightBackground");
    private static readonly Texture2D rightCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_RightCuttingLine");
    private static readonly Texture2D rightSupportSquadIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_RightSupportSquadIcon");

    private static readonly Texture2D branchSupplyLack = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_BranchSupply_Lack");
    private static readonly Texture2D branchSupplyJust = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_BranchSupply_Just");
    private static readonly Texture2D branchSupplyEnough = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_BranchSupply_Enough");

    private static readonly Texture2D bigStrangeIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_BigStrangeIcon");
    private static readonly Texture2D bigFriendlyIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_BigFriendlyIcon");
    private static readonly Texture2D smallStrangeIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_SmallStrangeIcon");
    private static readonly Texture2D smallFriendlyIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_SmallFriendlyIcon");

    private static readonly Texture2D branchBaseSiteIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_BranchBaseIcon");
    private static readonly Texture2D recommendationIcon = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_RecommendationIcon");
    private static readonly Texture2D verticalCuttingLine = ContentFinder<Texture2D>.Get("UI/OrderBranches/OARO_VerticalCuttingLine");
}
