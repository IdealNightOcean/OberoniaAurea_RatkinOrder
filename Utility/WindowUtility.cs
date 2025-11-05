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

    /// <summary>
    /// 绘制分部简述
    /// inRect: (width: 393f, height: 91f)
    /// </summary>
    /// <param name="inRect">width: 393f, height: 91f</param>
    public static Rect DrawBranchSummary(Vector2 position, BranchSummaryUICache entry)
    {
        Rect rect = new(position.x, position.y, 393f, 91f);
        GUI.DrawTexture(rect, IconLibrary.BranchSummaryBackground);
        Rect inRect = rect.ContractedBy(2f);

        Rect reusedRect = CenterRectOnY(inRect, inRect.x, 6f, 87f);
        if (entry.HonorStripSmall is not null)
        {
            GUI.DrawTexture(reusedRect, entry.HonorStripSmall);
        }

        Rect leftRect = new(inRect.x + 6f, inRect.y, 224f, inRect.height);

        reusedRect = new(leftRect.x + 2f, leftRect.y + 2f, 32f, 20f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, entry.Distance.ToString("F0").Colorize(entry.IsInAffectedRange ? Color.green : Color.white));

        if (entry.HonorDecorationSmall is not null)
        {
            reusedRect = leftRect.ContractedBy(10f);
            GUI.DrawTexture(reusedRect, entry.HonorDecorationSmall, ScaleMode.ScaleToFit);
        }

        if (entry.HonorBackgroundSmall is not null)
        {
            reusedRect = CenterRectOnY(leftRect, leftRect.x, 225f, 87f);
            GUI.DrawTexture(reusedRect, entry.HonorBackgroundSmall);
        }

        if (entry.HonorIcon is not null)
        {
            reusedRect = CenterRectOnY(leftRect, leftRect.x + 10f, 90f, 65f);
            GUI.DrawTexture(reusedRect, entry.HonorIcon, ScaleMode.ScaleToFit);
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
        Widgets.Label(reusedRect, "OARO_CurAllCrewCount".Translate(entry.CurAllCrewCount));
        reusedRect = new(textX, reusedRect.yMax, rightRect.width, 29f);
        Widgets.Label(reusedRect, "OARO_BranchPotency".Translate());
        reusedRect = new(textX, reusedRect.yMax, rightRect.width, 29f);
        string supplyState = "OARO_BranchSupplyState".Translate() + "  ";
        supplyState += entry.Branch.Supply switch
        {
            < 0.2f => "OARO_BranchSupply_Lack".Translate().Colorize(ColorLibrary.Orange),
            < 0.8f => "OARO_BranchSupply_Just".Translate().Colorize(Color.yellow),
            _ => "OARO_BranchSupply_Enough".Translate().Colorize(Color.green),
        };
        Widgets.Label(reusedRect, supplyState);
        Text.Anchor = TextAnchor.UpperLeft;

        return rect;
    }
}