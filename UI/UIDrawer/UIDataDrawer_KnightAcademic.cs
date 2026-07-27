using OberoniaAurea_Frame;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDataDrawer_KnightAcademic : UIDataDrawerBase<UIData_KnightAcademic>
{
    public override Vector2 InitSize => new(260f, 94f);

    public override void DrawInner(Vector2 position, UIData_KnightAcademic drawData)
    {
        Rect boxRect = new(position, InitSize);
        OARO_Widgets.DrawDefaultBoxSolidWithOutline(boxRect, outlineThickness: 2);

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

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleLeft);
        OAFrame_Widgets.DrawLabel(labelRect, drawData.Academic.LabelCap, this.TextStyle);

        Rect levelRect = upperInnerRect;
        levelRect.xMax = upperInnerRect.xMax - upperInnerRect.width * 0.1f;

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleRight);
        OAFrame_Widgets.DrawLabel(levelRect, $"{drawData.Level}/{drawData.Academic.MaxStageLevel}", this.TextStyle);

        Text.Font = GameFont.Small;
    }

    private void DrawLowerInner(Rect lowerInnerRect, UIData_KnightAcademic drawData)
    {
        Rect factorRect = lowerInnerRect;
        factorRect.xMin = lowerInnerRect.xMin + lowerInnerRect.width * 0.1f;

        this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleLeft,
                             guiColor: drawData.CostFactor > 1f ? ColorLibrary.RedReadable : Color.green);
        OAFrame_Widgets.DrawLabel(factorRect, drawData.CostFactor.ToStringPercent("0.##"), this.TextStyle);
        TooltipHandler.TipRegion(factorRect, () => drawData.CostFactorExplanation, uniqueId: 876465514);
    }
}
