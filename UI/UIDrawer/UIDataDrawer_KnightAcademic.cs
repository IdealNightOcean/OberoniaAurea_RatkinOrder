using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.UI;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademic : UIDataDrawerBase<UIData_KnightAcademic>
{

    private Vector2 scrollPosition_LevelStar;

    public UIDataDrawer_KnightAcademic()
    {
        DrawSize = new(260f, 94f);
        OutlineThickness = 2;
    }

    protected override void DrawInner(Vector2 position)
    {
        Rect boxRect = new(position, DrawSize);
        OARO_Widgets.DrawDefaultBoxSolidWithOutline(boxRect, outlineThickness: OutlineThickness);

        Rect innerBoxRect = GenUI.ContractedBy(boxRect, OutlineThickness); //标准大约为(256f, 90f)

        Rect upperInnerRect = innerBoxRect.TopPart(0.6f);
        DrawUpperInner(upperInnerRect);

        float divideLineRectY = upperInnerRect.yMax;
        OAFrame_Widgets.DrawLineHorizontal(new Vector2(innerBoxRect.xMin + 4f, divideLineRectY), innerBoxRect.width - 8f, OARO_ColorLibrary.DivideLine);

        Rect lowerInnerRect = innerBoxRect.BottomPart(0.4f);
        lowerInnerRect.yMin = divideLineRectY + 1f;
        DrawLowerInner(lowerInnerRect);
    }

    private void DrawUpperInner(Rect inRect)
    {
        Rect validRect = inRect.CenterSegmentOnX(0.9f);

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(validRect, DrawData.IsDataValid ? DrawData.Academic.LabelCap : "---", this.TextStyle);

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleRight);
        if (DrawData.IsDataValid)
            OAFrame_Widgets.DrawLabel(validRect, $"{DrawData.StageLevel}/{DrawData.Academic.MaxStageLevel}", this.TextStyle);
        else
            OAFrame_Widgets.DrawLabel(validRect, "0/0", this.TextStyle);
    }

    private void DrawLowerInner(Rect inRect)
    {
        Rect validRect = inRect.CenterSegmentOnX(0.9f);
        this.TextStyle = new(guiColor: DrawData.CostFactor > 1f ? ColorLibrary.RedReadable : Color.green,
                             font: GameFont.Medium, anchor: TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(validRect, DrawData.CostFactor.ToStringPercent("0.##"), this.TextStyle);
        if (DrawData.IsDataValid)
            TooltipHandler.TipRegion(validRect, () => DrawData.CostFactorExplanation.Value, uniqueId: 876465514);

        Rect levelStarGroupRect = validRect.CenterSegmentOnY((1f / 3f));
        levelStarGroupRect = levelStarGroupRect.RightHalf();
        float starSize = Mathf.Min(levelStarGroupRect.height, levelStarGroupRect.width / 6f);

        OARO_UIUtility.DrawStarGroup(outRect: levelStarGroupRect,
                                     starSize: new(starSize, starSize),
                                     interval: 3f,
                                     totalStarNum: DrawData.IsDataValid ? DrawData.Academic.MaxStageLevel : 0,
                                     activeStarNum: DrawData.IsDataValid ? DrawData.StageLevel : 0,
                                     scrollPosition: ref scrollPosition_LevelStar);
    }
}
