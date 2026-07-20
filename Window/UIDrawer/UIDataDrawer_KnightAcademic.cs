using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademic : UIDataDrawerBase<UIData_KnightAcademic>
{
    public override void DrawInner(Rect inRect, UIData_KnightAcademic drawData)
    {
        Rect boxRect = new(inRect.x, inRect.y, 260f, 94f);

        Widgets.DrawBoxSolidWithOutline(boxRect, solidColor: OARO_ColorLibrary.DimDarkBackground, outlineColor: OARO_ColorLibrary.CommonOutline, outlineThickness: 2);

        Rect innerBoxRect = boxRect.ContractedBy(2f);

        Rect upperInnerRect = innerBoxRect;
        upperInnerRect.yMax = innerBoxRect.yMin + 55f;
        DrawUpperInner(upperInnerRect, drawData);

        Rect divideLineRect = new(innerBoxRect.xMin + 4f, upperInnerRect.yMax, innerBoxRect.width - 8f, 1f);
        Widgets.DrawBoxSolid(divideLineRect, OARO_ColorLibrary.DivideLine);

        Rect lowerInnerRect = innerBoxRect;
        lowerInnerRect.yMin = divideLineRect.yMax;
        DrawLowerInner(lowerInnerRect, drawData);
    }

    private void DrawUpperInner(Rect upperInnerRect, UIData_KnightAcademic drawData)
    {
        Rect labelRect = upperInnerRect;
        labelRect.xMin = upperInnerRect.xMin + upperInnerRect.width * 0.1f;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        OAFrame_UIUtility.DrawLabel(labelRect, drawData.Academic.LabelCap, GameFont.Medium, TextAnchor.MiddleLeft);

        Rect levelRect = upperInnerRect;
        levelRect.xMax = upperInnerRect.xMax - upperInnerRect.width * 0.1f;

        Text.Anchor = TextAnchor.MiddleRight;
        OAFrame_UIUtility.DrawLabel(levelRect, $"{drawData.Level}/{drawData.Academic.MaxStageLevel}", GameFont.Medium, TextAnchor.MiddleRight);

        Text.Font = GameFont.Small;
    }

    private void DrawLowerInner(Rect lowerInnerRect, UIData_KnightAcademic drawData)
    {
        Rect factorRect = lowerInnerRect;
        factorRect.xMin = lowerInnerRect.xMin + lowerInnerRect.width * 0.1f;

        OAFrame_UIUtility.DrawLabel(factorRect, $"{drawData.Level}/{drawData.Academic.MaxStageLevel}", GameFont.Medium, TextAnchor.MiddleLeft);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;

        Widgets.Label(factorRect, OAFrame_TextUtility.ColoredFloatString(drawData.CostFactor, originPoint: 1f));
        TooltipHandler.TipRegion(factorRect, () => drawData.CostFactorExplanation, uniqueId: 876465514);
    }
}
