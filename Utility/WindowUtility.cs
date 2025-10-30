using OberoniaAurea_Frame;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_WindowUtility
{
    /// <summary>
    /// Rect居中对应最小坐标
    /// </summary>
    /// <param name="outerMinCoords">做标准Rect对应最小坐标</param>
    /// <param name="outerSize">做标准Rect尺寸</param>
    /// <param name="innerSize">被居中Rect尺寸</param>
    /// <returns>Rect居中对应最小坐标</returns>
    public static float CenterMinCoords(float outerMinCoords, float outerSize, float innerSize) => outerMinCoords + (outerSize - innerSize) * 0.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rect CenterRectOnX(Rect outerRect, float y, float width, float height) => new(outerRect.x + (outerRect.width - width) / 2, y, width, height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rect CenterRectOnY(Rect outerRect, float x, float width, float height) => new(x, outerRect.y + (outerRect.height - height) / 2, width, height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rect CenterRect(Rect outerRect, float width, float height) => new(outerRect.x + (outerRect.width - width) / 2, outerRect.y + (outerRect.height - height) / 2, width, height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dialog_NodeTreeWithRatkinOrderInfo DefaultConfirmDiaNodeTreeWithRatkinOrderInfo(TaggedString text, RatkinOrder ratkinOrder, Action acceptAction = null, Action rejectAction = null)
    {
        return new Dialog_NodeTreeWithRatkinOrderInfo(OAFrame_DiaUtility.ConfirmDiaNode(text, "Confirm".Translate(), acceptAction, "Close".Translate(), rejectAction), ratkinOrder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dialog_NodeTreeWithRatkinOrderInfo ConfirmDiaNodeTreeWithRatkinOrderInfo(TaggedString text, RatkinOrder ratkinOrder, string acceptText = null, Action acceptAction = null, string rejectText = null, Action rejectAction = null)
    {
        return new Dialog_NodeTreeWithRatkinOrderInfo(OAFrame_DiaUtility.ConfirmDiaNode(text, acceptText, acceptAction, rejectText, rejectAction), ratkinOrder);
    }

    public static bool ButtonImage(Rect butRect, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        if (Mouse.IsOver(butRect))
        {
            GUI.DrawTexture(butRect, downTex);
        }
        else
        {
            GUI.DrawTexture(butRect, baseTex);
        }

        if (!string.IsNullOrEmpty(tooltip))
        {
            TooltipHandler.TipRegion(butRect, tooltip);
        }

        return Widgets.ButtonInvisible(butRect, doMouseoverSound);
    }

    public static bool TextButtonImage(Rect butRect, string label, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        bool result = ButtonImage(butRect, baseTex, downTex, doMouseoverSound, tooltip);

        TextAnchor anchor = Text.Anchor;
        Color color = GUI.color;
        bool wordWrap = Text.WordWrap;

        Text.Anchor = TextAnchor.MiddleCenter;
        if (butRect.height < Text.LineHeight * 2f)
        {
            Text.WordWrap = false;
        }

        Widgets.Label(butRect, label);

        Text.Anchor = anchor;
        GUI.color = color;
        Text.WordWrap = wordWrap;

        return result;
    }
}