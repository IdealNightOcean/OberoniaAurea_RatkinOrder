using NightOcean.Utility;
using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademic : UIDataDrawerBase<UIData_KnightAcademic>
{
    public override Vector2 DefaultSize => new(260f, 94f);

    private Vector2 scrollPosition_LevelStar;

    public override void DrawInner(Vector2 position, UIData_KnightAcademic drawData)
    {
        Rect boxRect = new(position, DrawSize);
        OARO_Widgets.DrawDefaultBoxSolidWithOutline(boxRect, outlineThickness: 2);

        Rect innerBoxRect = GenUI.ContractedBy(boxRect, 2f); //标准大约为(256f, 90f)

        Rect upperInnerRect = innerBoxRect.TopPart(0.6f);
        DrawUpperInner(upperInnerRect, drawData);

        float divideLineRectY = upperInnerRect.yMax;
        OAFrame_Widgets.DrawLineHorizontal(new Vector2(innerBoxRect.xMin + 4f, divideLineRectY), innerBoxRect.width - 8f, OARO_ColorLibrary.DivideLine);

        Rect lowerInnerRect = innerBoxRect.BottomPart(0.4f);
        lowerInnerRect.yMin = divideLineRectY + 1f;
        DrawLowerInner(lowerInnerRect, drawData);
    }

    private void DrawUpperInner(Rect inRect, UIData_KnightAcademic drawData)
    {
        Rect labelRect = inRect.LeftPart(0.9f);

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(labelRect, DrawDataValid ? drawData.Academic.LabelCap : "---", this.TextStyle);

        Rect levelRect = inRect.RightPart(0.9f);
        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleRight);
        if (DrawDataValid)
            OAFrame_Widgets.DrawLabel(levelRect, $"{drawData.Level}/{drawData.Academic.MaxStageLevel}", this.TextStyle);
        else
            OAFrame_Widgets.DrawLabel(levelRect, "0/0", this.TextStyle);
    }

    private void DrawLowerInner(Rect inRect, UIData_KnightAcademic drawData)
    {
        Rect factorRect = inRect.LeftPart(0.9f);
        this.TextStyle = new(guiColor: drawData.CostFactor > 1f ? ColorLibrary.RedReadable : Color.green,
                             font: GameFont.Medium, anchor: TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(factorRect, drawData.CostFactor.ToStringPercent("0.##"), this.TextStyle);
        if (DrawDataValid)
            TooltipHandler.TipRegion(factorRect, () => drawData.CostFactorExplanation.Value, uniqueId: 876465514);

        Rect levelStarGroupRect = inRect.CenterSegmentOnY((1f / 3f));
        levelStarGroupRect.width *= 0.4f;
        levelStarGroupRect.MoveTo(inRect.xMax - 0.1f * inRect.width - levelStarGroupRect.width, levelStarGroupRect.yMin);

        int maxStageLevel = DrawDataValid ? drawData.Academic.MaxStageLevel : 0;
        int curStageLevel = DrawDataValid ? drawData.Level : 0;

        float single‌StartWidth = levelStarGroupRect.width / 6f;
        Rect levelStarGroupViewRect = levelStarGroupRect;
        levelStarGroupRect.width = single‌StartWidth * maxStageLevel;

        float startSize = Mathf.Min(levelStarGroupRect.height, single‌StartWidth);
        Widgets.BeginScrollView(levelStarGroupRect, ref scrollPosition_LevelStar, levelStarGroupViewRect, showScrollbars: false);
        Rect levelStarRect = new(levelStarGroupViewRect.x, levelStarGroupViewRect.y, startSize, startSize);
        for (int i = 0; i < maxStageLevel; i++)
        {
            GUI.DrawTexture(levelStarRect, i <= curStageLevel ? OARO_IconLibrary.StarWhite : OARO_IconLibrary.StarBlack, ScaleMode.ScaleToFit);
            levelStarRect.OffsetHorizontal(single‌StartWidth);
        }
        Widgets.EndScrollView();
    }
}
