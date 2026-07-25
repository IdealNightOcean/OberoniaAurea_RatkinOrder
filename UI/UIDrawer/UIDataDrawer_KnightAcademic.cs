using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademic : UIDataDrawerBase<UIData_KnightAcademic>
{
    public override Vector2 InitSize => new(260f, 94f);

    public override void DrawInner(Vector2 position, UIData_KnightAcademic drawData)
    {
        Rect boxRect = new(position.x, position.y, InitSize.x, InitSize.y);

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

        GameFontText = new(GameFont.Medium, TextAnchor.MiddleLeft);
        GameFontText.DrawLabel(labelRect, drawData.Academic.LabelCap);

        Rect levelRect = upperInnerRect;
        levelRect.xMax = upperInnerRect.xMax - upperInnerRect.width * 0.1f;

        GameFontText = new(GameFont.Medium, TextAnchor.MiddleRight);
        GameFontText.DrawLabel(labelRect, $"{drawData.Level}/{drawData.Academic.MaxStageLevel}");

        Text.Font = GameFont.Small;
    }

    private void DrawLowerInner(Rect lowerInnerRect, UIData_KnightAcademic drawData)
    {
        Rect factorRect = lowerInnerRect;
        factorRect.xMin = lowerInnerRect.xMin + lowerInnerRect.width * 0.1f;

        GameFontText = new(font: GameFont.Medium, anchor: TextAnchor.MiddleLeft,
                           guiColor: drawData.CostFactor > 1f ? ColorLibrary.RedReadable : Color.green);
        GameFontText.DrawLabel(factorRect, $"{drawData.Level}/{drawData.Academic.MaxStageLevel}");
        TooltipHandler.TipRegion(factorRect, () => drawData.CostFactorExplanation, uniqueId: 876465514);
    }
}
