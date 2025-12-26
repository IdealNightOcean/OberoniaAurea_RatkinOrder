using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_ResidentKnight_RankUpgrade : OrderWindowBase
{
    private ResidentKnightRecord Record { get; }
    private Map Map { get; }
    private ResidentKnightRecord.Rank TargetRank { get; }

    private Vector2 scrollPosition_Description;

    public Action PostAcceptAction { get; set; }

    public override Vector2 InitialSize => new(593f, 480f);

    public Window_ResidentKnight_RankUpgrade(ResidentKnightRecord record, Map map) : base()
    {
        Record = record;
        Map = map;
        TargetRank = ResidentKnightRecord.RankOffsetBy(Record.CurRank, offset: 1);
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
        PostAcceptAction = null;
    }

    public override void DoWindowContents(Rect inRect)
    {
        GUI.DrawTexture(inRect, mainBackground);
        Rect innerRect = OARO_WindowUtility.CenterRect(inRect, 510f, 402f).ContractedBy(2f);
        float innerRectX = innerRect.xMin;
        float innerRectY = innerRect.yMin;

        if (OARO_WindowUtility.DrawCloseX_Corner(innerRect))
        {
            Close();
            return;
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect reusedRect = new(innerRectX, innerRectY + 24f, innerRect.width, 32f);
        Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{TargetRank}".Translate());

        Text.Font = GameFont.Small;
        reusedRect = new(innerRectX + 100f, innerRectY + 60f, innerRect.width - 200f, 64f);
        Widgets.LabelScrollable(reusedRect, $"OARO_ResidentKnightRank_{TargetRank}Desc".Translate(), ref scrollPosition_Description);

        Text.Font = GameFont.Medium;
        reusedRect = new(innerRectX, innerRectY + 137f, innerRect.width, 185f);
        DrawRankBackGround(reusedRect);
        Widgets.Label(reusedRect, $"OARO_ResidentKnightRank_{TargetRank}Knight".Translate().Colorize(ResidentKnightRecord.GetRankColor(TargetRank)));

        Text.Font = GameFont.Small;
        reusedRect = new(innerRectX, innerRectY + 330f, innerRect.width, 20f);
        Widgets.Label(reusedRect, "OARO_HallWin_UpgradeRankConfirm".Translate());

        reusedRect = new(innerRectX + 150f, innerRectY + 356f, 71f, 22f);

        if (OARO_WindowUtility.TextButtonImage(butRect: reusedRect,
            label: "Cancel".Translate(),
            baseTex: smallButton,
            downTex: smallButton_Down,
            doMouseoverSound: true))
        {
            Close();
        }

        reusedRect = new(innerRectX + 290f, innerRectY + 356f, 71f, 22f);
        if (OARO_WindowUtility.TextButtonImage(
            butRect: reusedRect,
            label: "Confirm".Translate(),
            baseTex: smallButton,
            downTex: smallButton_Down,
            doMouseoverSound: true))
        {
            AcceptanceReport acceptance = GlobalInteractionUtility.CanUpgradeResidentKnightRank(Record, Map, resultOnly: false);
            if (acceptance)
            {
                GlobalInteractionUtility.UpgradeResidentKnightRank(Record, Map);
                PostAcceptAction?.Invoke();
                Close();
            }
            else
            {
                Messages.Message("OARO_CanNotUpgradeResidentKnightRankWithReason".Translate(acceptance.Reason.Named(KeyLibrary_FormatArgName.Reason)), MessageTypeDefOf.RejectInput, historical: false);
            }
        }

        OARO_WindowUtility.ResetText();
    }

    private void DrawRankBackGround(Rect inRect)
    {
        Texture2D rankTex = TargetRank switch
        {
            ResidentKnightRecord.Rank.Regular => rankBackground_Regular,
            ResidentKnightRecord.Rank.Elite => rankBackground_Elite,
            ResidentKnightRecord.Rank.Honor => rankBackground_Honor,
            ResidentKnightRecord.Rank.Crown => rankBackground_Crown,
            _ => null,
        };

        if (rankTex is not null)
        {
            GUI.DrawTexture(inRect, rankTex, ScaleMode.ScaleToFit);
        }
    }

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_MainBackground");

    private static readonly Texture2D smallButton = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_SmallButton");
    private static readonly Texture2D smallButton_Down = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_SmallButton_Down");

    private static readonly Texture2D rankBackground_Regular = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_RankBackground_Regular");
    private static readonly Texture2D rankBackground_Elite = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_RankBackground_Elite");
    private static readonly Texture2D rankBackground_Honor = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_RankBackground_Honor");
    private static readonly Texture2D rankBackground_Crown = ContentFinder<Texture2D>.Get("UI/ResidentKnight/RankUpgrade/OARO_RankBackground_Crown");
}