using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_Widgets
{
    public static void DrawDefaultBoxSolidWithOutline(Rect boxRect, int outlineThickness = 1)
    {
        Widgets.DrawBoxSolidWithOutline(rect: boxRect,
                                        solidColor: OARO_ColorLibrary.DimDarkBackground,
                                        outlineColor: OARO_ColorLibrary.CommonOutline,
                                        outlineThickness: outlineThickness);
    }
}
