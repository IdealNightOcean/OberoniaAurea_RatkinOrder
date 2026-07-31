using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

using static OberoniaAurea.RatkinOrder.Branch;

public static class OARO_UIUtility
{
    /// <summary>
    /// Rect居中对应最小坐标
    /// </summary>
    /// <param name="outerMinCoords">做标准Rect对应最小坐标</param>
    /// <param name="outerSize">做标准Rect尺寸</param>
    /// <param name="innerSize">被居中Rect尺寸</param>
    /// <returns>Rect居中对应最小坐标</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        return new Dialog_NodeTreeWithRatkinOrderInfo(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.ConfirmDiaNode(text, "Confirm".Translate(), acceptAction, "Close".Translate(), rejectAction), ratkinOrder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dialog_NodeTreeWithRatkinOrderInfo ConfirmDiaNodeTreeWithRatkinOrderInfo(TaggedString text, RatkinOrder ratkinOrder, string acceptText = null, Action acceptAction = null, string rejectText = null, Action rejectAction = null)
    {
        return new Dialog_NodeTreeWithRatkinOrderInfo(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.ConfirmDiaNode(text, acceptText, acceptAction, rejectText, rejectAction), ratkinOrder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawCloseX_Corner(Rect mainRect)
    {
        Rect reusedRect = new(mainRect.xMax - 26f, mainRect.y + 2f, 24f, 24f);
        return Widgets.ButtonImage(reusedRect, OARO_IconLibrary.ColseX, doMouseoverSound: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawBackArrow_Corner(Rect mainRect)
    {
        Rect reusedRect = new(mainRect.xMax - 54f, mainRect.y + 2f, 24f, 24f);
        return Widgets.ButtonImage(reusedRect, OARO_IconLibrary.BackArrow, doMouseoverSound: true);
    }

    public static bool ButtonImage(Rect butRect, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        if (Mouse.IsOver(butRect))
            GUI.DrawTexture(butRect, downTex);
        else
            GUI.DrawTexture(butRect, baseTex);


        if (!String.IsNullOrEmpty(tooltip))
        {
            TooltipHandler.TipRegion(butRect, tooltip);
        }

        return Widgets.ButtonInvisible(butRect, doMouseoverSound);
    }

    public static bool TextButtonImage(Rect butRect, string label, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        bool result = ButtonImage(butRect, baseTex, downTex, doMouseoverSound, tooltip);

        TextAnchor preAnchor = Text.Anchor;
        Color preColor = GUI.color;
        bool preWordWrap = Text.WordWrap;

        Text.Anchor = TextAnchor.MiddleCenter;
        if (butRect.height < Text.LineHeight * 2f)
        {
            Text.WordWrap = false;
        }

        Widgets.Label(butRect, label);

        Text.Anchor = preAnchor;
        GUI.color = preColor;
        Text.WordWrap = preWordWrap;

        return result;
    }

    public static bool TextButtonImageDisableable(Rect butRect, string label, AcceptanceReport acceptance, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        if (acceptance)
        {
            return TextButtonImage(butRect, label, baseTex, downTex, doMouseoverSound, tooltip);
        }
        else
        {
            GUI.DrawTexture(butRect, downTex);
            if (!String.IsNullOrEmpty(acceptance.Reason))
            {
                TooltipHandler.TipRegion(butRect, acceptance.Reason);
            }
            if (!String.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(butRect, tooltip);
            }

            TextAnchor preAnchor = Text.Anchor;
            Color preColor = GUI.color;
            bool preWordWrap = Text.WordWrap;

            Text.Anchor = TextAnchor.MiddleCenter;
            if (butRect.height < Text.LineHeight * 2f)
            {
                Text.WordWrap = false;
            }

            Widgets.Label(butRect, label);

            Text.Anchor = preAnchor;
            GUI.color = preColor;
            Text.WordWrap = preWordWrap;

            return false;
        }
    }

    public static void DrawBranchInteractionButton(Rect butRect, BranchInteractionDef def, BranchInteractionParms parms, AcceptanceReport? cachedAcceptance, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        if (!cachedAcceptance.HasValue)
        {
            cachedAcceptance = def.Worker.CanUseInteraction(parms, resultOnly: false);
        }

        if (TextButtonImageDisableable(butRect, def.label, cachedAcceptance.Value, baseTex, downTex, doMouseoverSound: doMouseoverSound, tooltip: tooltip))
        {
            AcceptanceReport acceptanceReport = def.Worker.CanUseInteraction(parms, resultOnly: false);
            if (acceptanceReport)
            {
                def.Worker.TryApplyInteraction(parms);
            }
            else
            {
                Messages.Message(
                    text: "OARO_CanApplyBranchInteractionWithReason".Translate(parms.Branch.Name.Named(OARO_KeyLibrary_FormatArgName.BranchName), def.Named("INTERACTION"), acceptanceReport.Reason.Named(KeyLibrary_FormatArgName.Reason)),
                    def: MessageTypeDefOf.RejectInput,
                    historical: false);
            }
        }
    }

    public static void DrawRecommendationInfo(Rect inRect, int count, float textOffset = 0f)
    {
        TextAnchor preAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;

        Rect reusedRect = new(inRect.x, inRect.y, inRect.height, inRect.height);
        GUI.DrawTexture(reusedRect, OARO_IconLibrary.RecommendationIcon, ScaleMode.ScaleToFit);

        reusedRect = Rect.MinMaxRect(reusedRect.xMax + textOffset, inRect.yMin, inRect.xMax, inRect.yMax);
        Widgets.Label(reusedRect, $"× {count}");

        Text.Anchor = preAnchor;
    }

    public static void DrawBranchIcon(Rect inRect, Branch branch, bool expand)
    {
        if (branch?.HonorDef is null)
        {
            GUI.DrawTexture(inRect, expand ? OARO_IconLibrary.BigGeneralBranchIcon : OARO_IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.DrawTexture(inRect, expand ? branch.HonorDef.iconTexture.ExpandedTexture : branch.HonorDef.iconTexture.Texture, ScaleMode.ScaleToFit);
        }
    }

    public static void DrawBranchStateIcon(Rect inRect, Branch branch, bool expand)
    {
        if (branch is null)
        {
            GUI.DrawTexture(inRect, expand ? OARO_IconLibrary.BigIdleIcon : OARO_IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
            return;
        }

        switch (branch.CurWorkState)
        {
            case WorkStateType.Idle:
                {
                    GUI.DrawTexture(inRect, expand ? OARO_IconLibrary.BigIdleIcon : OARO_IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
                    return;
                }
            case WorkStateType.OnBaseTask:
                {
                    GUI.DrawTexture(inRect, expand ? OARO_IconLibrary.BigOnBaseIcon : OARO_IconLibrary.SmallOnBaseIcon, ScaleMode.ScaleToFit);
                    return;
                }
            case WorkStateType.AbroadTask:
                {
                    GUI.DrawTexture(inRect, expand ? OARO_IconLibrary.BigAbroadIcon : OARO_IconLibrary.SmallAbroadIcon, ScaleMode.ScaleToFit);
                    return;
                }
            default: return;
        }
    }

    public static void DrawStarGroup(Rect outRect, Vector2 starSize, float interval, int totalStarNum, int activeStarNum, ref Vector2 scrollPosition)
    {
        Rect viewRect = outRect;
        starSize.x = starSize.x > 4f ? starSize.x : 4f;
        starSize.y = starSize.y > 4f ? starSize.y : 4f;

        viewRect.width = starSize.x * totalStarNum;

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, showScrollbars: false);
        Rect starRect = new(viewRect.x, viewRect.y, starSize.x, starSize.y);
        for (int i = 0; i < totalStarNum; i++)
        {
            GUI.DrawTexture(starRect, i <= activeStarNum ? OARO_IconLibrary.StarWhite : OARO_IconLibrary.StarBlack, ScaleMode.ScaleToFit);
            starRect.OffsetHorizontal(starSize.x + interval);
        }
        Widgets.EndScrollView();
    }

    public static void DrawKnightChivalryIcon(Rect inRect, KnightChivalryDef taskChivalry, bool primary)
    {
        if (taskChivalry is not null)
        {
            GUI.DrawTexture(inRect, primary ? taskChivalry.primaryIcon.Texture : taskChivalry.icon.Texture, ScaleMode.ScaleToFit);
        }
    }

}