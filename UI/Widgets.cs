using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.UI;
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


    public static bool DefaultTextButton(Rect butRect, string text, bool doMouseoverSound = true)
    {
        return OAFrame_Widgets.DefaultTextButtonImageFitted(butRect, text, OARO_IconLibrary.DefaultButton_Active, doMouseoverSound: doMouseoverSound);
    }

    public static bool DefaultTextButtonDisableable(Rect butRect, string text, AcceptanceReport acceptance, bool doMouseoverSound = true)
    {
        if (acceptance.Accepted)
        {
            return OAFrame_Widgets.DefaultTextButtonImageFitted(butRect, text, OARO_IconLibrary.DefaultButton_Active, doMouseoverSound: doMouseoverSound);
        }
        else
        {
            Color oriColor = GUI.color;
            TextAnchor oriAnchor = Text.Anchor;

            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(butRect, text);

            Text.Anchor = oriAnchor;
            GUI.color = oriColor;
            return false;
        }
    }
}
