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

    public override void DrawInner(Vector2 position)
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
        Rect labelRect = inRect.LeftPart(0.9f);

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(labelRect, DrawDataValid ? DrawData.Academic.LabelCap : "---", this.TextStyle);

        Rect levelRect = inRect.RightPart(0.9f);
        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleRight);
        if (DrawDataValid)
            OAFrame_Widgets.DrawLabel(levelRect, $"{DrawData.StageLevel}/{DrawData.Academic.MaxStageLevel}", this.TextStyle);
        else
            OAFrame_Widgets.DrawLabel(levelRect, "0/0", this.TextStyle);
    }

    private void DrawLowerInner(Rect inRect)
    {
        Rect factorRect = inRect.LeftPart(0.9f);
        this.TextStyle = new(guiColor: DrawData.CostFactor > 1f ? ColorLibrary.RedReadable : Color.green,
                             font: GameFont.Medium, anchor: TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(factorRect, DrawData.CostFactor.ToStringPercent("0.##"), this.TextStyle);
        if (DrawDataValid)
            TooltipHandler.TipRegion(factorRect, () => DrawData.CostFactorExplanation.Value, uniqueId: 876465514);

        Rect levelStarGroupRect = inRect.CenterSegmentOnY((1f / 3f));
        levelStarGroupRect.width *= 0.4f;
        levelStarGroupRect.MoveTo(inRect.xMax - 0.1f * inRect.width - levelStarGroupRect.width, levelStarGroupRect.yMin);
        float starSize = Mathf.Min(levelStarGroupRect.height, levelStarGroupRect.width / 6f);

        OARO_UIUtility.DrawStarGroup(outRect: levelStarGroupRect,
                                     starSize: new(starSize, starSize),
                                     interval: 3f,
                                     totalStarNum: DrawDataValid ? DrawData.Academic.MaxStageLevel : 0,
                                     activeStarNum: DrawDataValid ? DrawData.StageLevel : 0,
                                     scrollPosition: ref scrollPosition_LevelStar);
    }
}
