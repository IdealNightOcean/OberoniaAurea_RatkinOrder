using OberoniaAurea_Frame;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextureOriginalSize(Vector2 position, Texture2D texture) => GUI.DrawTexture(new(position.x, position.y, texture.width, texture.height), texture);

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
            if (!string.IsNullOrEmpty(acceptance.Reason))
            {
                TooltipHandler.TipRegion(butRect, acceptance.Reason);
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

    /// <summary>
    /// 绘制分部简述
    /// inRect: (width: 392f, height: 90f)
    /// </summary>
    /// <param name="position">width: 392f, height: 90f</param>
    public static Rect DrawBranchSummary(Vector2 position, BranchSummaryUICache entry)
    {
        GameFont preFont = Text.Font;
        TextAnchor preAnchor = Text.Anchor;

        Rect rect = new(position.x, position.y, 392f, 90f);
        GUI.DrawTexture(rect, IconLibrary.BranchSummaryBackground);
        Rect inRect = rect.ContractedBy(2f);

        BranchHonorDef honorDef = entry.Branch.HonorDef;
        bool isHonorBranch = entry.Branch.IsBranchOfType(BranchType.Honor) && honorDef is not null;

        Rect reusedRect = CenterRectOnY(inRect, inRect.x, 5f, 86f);
        if (isHonorBranch)
        {
            GUI.DrawTexture(reusedRect, honorDef.HonorBarTexture);
        }

        Rect leftRect = new(inRect.x + 5f, inRect.y, 224f, inRect.height);

        reusedRect = new(leftRect.x + 2f, leftRect.y + 2f, 32f, 20f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, entry.Distance.ToString("F0").Colorize(entry.IsInAffectedRange ? Color.green : Color.white));

        if (isHonorBranch)
        {
            reusedRect = leftRect.ContractedBy(10f);
            GUI.DrawTexture(reusedRect, honorDef.DecorationTexture, ScaleMode.ScaleToFit);

            reusedRect = CenterRectOnY(leftRect, leftRect.x, 225f, 87f);
            GUI.DrawTexture(reusedRect, honorDef.BackgroundTexture);

            reusedRect = CenterRectOnY(leftRect, leftRect.x + 10f, 90f, 65f);
            GUI.DrawTexture(reusedRect, honorDef.IconTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            reusedRect = CenterRectOnY(leftRect, leftRect.x + 38f, 34f, 37f);
            GUI.DrawTexture(reusedRect, IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }

        Rect squadNameRect = Rect.MinMaxRect(leftRect.x + 100f, leftRect.y + 4f, leftRect.xMax - 16f, leftRect.y + 4f + 22f);
        string squadName = entry.SquadName;
        if (Text.CalcSize(squadName).x < 100f)
        {
            Widgets.Label(squadNameRect, squadName);
        }
        else
        {
            Widgets.LabelEllipses(squadNameRect, squadName);
            if (!string.IsNullOrEmpty(squadName) && Mouse.IsOver(squadNameRect))
            {
                TooltipHandler.TipRegion(squadNameRect, () => squadName, 6844867);
            }
        }

        reusedRect = new(squadNameRect.x + 16f, squadNameRect.yMax + 4f, 25f, 30f);
        string relation;
        if (entry.Branch.IsBranchOfType(BranchType.Friendly))
        {
            GUI.DrawTexture(reusedRect, IconLibrary.SmallFriendlyIcon, ScaleMode.ScaleToFit);
            relation = "OARO_Friendly".Translate().Colorize(Color.green);
        }
        else
        {
            GUI.DrawTexture(reusedRect, IconLibrary.SmallStrangeIcon, ScaleMode.ScaleToFit);
            relation = "OARO_Strange".Translate();
        }

        reusedRect = CenterRectOnX(reusedRect, reusedRect.yMax + 3f, 40f, 20f);
        Widgets.Label(reusedRect, relation);

        reusedRect = new(squadNameRect.xMax - 40f, squadNameRect.yMax + 4f, 30f, 30f);
        if (entry.Branch.IsIdleNow)
        {
            GUI.DrawTexture(reusedRect, IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
        }
        else if (entry.Branch.IsOutdoorNow)
        {
            GUI.DrawTexture(reusedRect, IconLibrary.SmallOutdoorIcon, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.DrawTexture(reusedRect, IconLibrary.SmallIndoorIcon, ScaleMode.ScaleToFit);
        }

        reusedRect = CenterRectOnX(reusedRect, reusedRect.yMax + 4f, 40f, 20f);
        string workState = entry.Branch.CurWorkState;
        if (Text.CalcSize(workState).x < 40f)
        {
            Widgets.Label(reusedRect, workState);
        }
        else
        {
            Widgets.LabelEllipses(reusedRect, workState);
            if (!string.IsNullOrEmpty(workState) && Mouse.IsOver(reusedRect))
            {
                TooltipHandler.TipRegion(reusedRect, () => workState, 3548681);
            }
        }

        Text.Anchor = TextAnchor.MiddleLeft;

        Rect rightRect = Rect.MinMaxRect(leftRect.xMax, inRect.yMin, inRect.xMax, inRect.yMax);
        float textX = rightRect.xMin + 24f;
        reusedRect = new(textX, rightRect.y, rightRect.width, 29f);
        Widgets.Label(reusedRect, "OARO_AllCrewCountShortInfo".Translate(entry.Branch.Squad.AllCrewCountInt));
        reusedRect = new(textX, reusedRect.yMax, rightRect.width, 29f);
        Widgets.Label(reusedRect, "OARO_BranchPotencyShortInfo".Translate() + ": ");
        reusedRect = new(textX, reusedRect.yMax, rightRect.width, 29f);
        string supplyState = "OARO_BranchSupplyState".Translate() + "  ";
        supplyState += entry.Branch.Supply switch
        {
            < 0.2f => "OARO_BranchSupply_Lack".Translate().Colorize(ColorLibrary.Orange),
            < 0.8f => "OARO_BranchSupply_Just".Translate().Colorize(Color.yellow),
            _ => "OARO_BranchSupply_Enough".Translate().Colorize(Color.green),
        };
        Widgets.Label(reusedRect, supplyState);

        Text.Anchor = preAnchor;
        Text.Font = preFont;
        return rect;
    }

    public static void DrawRecommendationInfo(Rect inRect, int count, float textOffset = 0f)
    {
        Rect reusedRect = new(inRect.x, inRect.y, inRect.height, inRect.height);
        GUI.DrawTexture(reusedRect, IconLibrary.RecommendationIcon, ScaleMode.ScaleToFit);

        reusedRect = Rect.MinMaxRect(reusedRect.xMax + textOffset, inRect.yMin, inRect.xMax, inRect.yMax);
        Widgets.Label(reusedRect, $"× {count}");
    }

    public static void DrawBranchIcon(Rect inRect, Branch branch, bool expand)
    {
        if (branch?.HonorDef is null)
        {
            GUI.DrawTexture(inRect, expand ? IconLibrary.BigGeneralBranchIcon : IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.DrawTexture(inRect, expand ? branch.HonorDef.ExpandingIconTexture : branch.HonorDef.IconTexture, ScaleMode.ScaleToFit);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetText()
    {
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }
}