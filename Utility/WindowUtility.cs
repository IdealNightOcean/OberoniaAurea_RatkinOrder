using OberoniaAurea_Frame;
using RimWorld;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

using static OberoniaAurea.RatkinOrder.Branch;

public static class OARO_WindowUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Material GetTintMaterial(Color color, Texture2D maskTex)
    {
        MaterialRequest req = new()
        {
            shader = ShaderDatabase.GrayscaleGUI,
            color = color,
            maskTex = maskTex ?? Texture2D.redTexture
        };

        return MaterialPool.MatFrom(req);
    }

    public static Material BlackWhiteMat
    {
        get
        {
            MaterialRequest req = new()
            {
                shader = ShaderDatabase.GrayscaleGUI,
                color = Color.white,
                maskTex = Texture2D.redTexture
            };

            return MaterialPool.MatFrom(req);
        }
    }

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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawCloseX_Corner(Rect mainRect)
    {
        Rect reusedRect = new(mainRect.xMax - 26f, mainRect.y + 2f, 24f, 24f);
        return Widgets.ButtonImage(reusedRect, IconLibrary.ColseX, doMouseoverSound: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawBackArrow_Corner(Rect mainRect)
    {
        Rect reusedRect = new(mainRect.xMax - 54f, mainRect.y + 2f, 24f, 24f);
        return Widgets.ButtonImage(reusedRect, IconLibrary.BackArrow, doMouseoverSound: true);
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
                    text: "OARO_CanApplyBranchInteractionWithReason".Translate(parms.Branch.Name.Named(KeyLibrary_FormatArgName.BranchName), def.Named("INTERACTION"), acceptanceReport.Reason.Named(KeyLibrary_FormatArgName.Reason)),
                    def: MessageTypeDefOf.RejectInput,
                    historical: false);
            }
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

        Branch branch = entry.Branch;
        BranchHonorDef honorDef = branch.HonorDef;
        bool isHonorBranch = branch.IsBranchOfType(BranchType.Honor) && honorDef is not null;

        Rect reusedRect = CenterRectOnY(inRect, inRect.x, 5f, 86f);
        if (isHonorBranch)
        {
            GUI.DrawTexture(reusedRect, honorDef.HonorColorTex);
        }

        Rect leftRect = new(inRect.x + 5f, inRect.y, 224f, inRect.height);

        reusedRect = new(leftRect.x + 2f, leftRect.y + 2f, 32f, 20f);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Small;
        Widgets.Label(reusedRect, entry.Distance.ToString("F0").Colorize(entry.IsInAffectedRange ? Color.green : Color.white));

        if (isHonorBranch)
        {
            reusedRect = leftRect.ContractedBy(10f);
            GUI.DrawTexture(reusedRect, honorDef.chivalry.medal.honorDecorationTexture.Texture, ScaleMode.ScaleToFit);

            reusedRect = CenterRectOnY(leftRect, leftRect.x, 225f, 87f);
            Material tintMat = OARO_WindowUtility.GetTintMaterial(honorDef.color, Texture2D.redTexture);
            GenUI.DrawTextureWithMaterial(reusedRect, IconLibrary.HonorBackgroundTex, tintMat);

            reusedRect = CenterRectOnY(leftRect, leftRect.x + 10f, 90f, 65f);
            GUI.DrawTexture(reusedRect, honorDef.iconTexture.Texture, ScaleMode.ScaleToFit);
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
            Widgets.Label(squadNameRect, squadName.Colorize(branch.Color));
        }
        else
        {
            Widgets.LabelEllipses(squadNameRect, squadName);
            if (!String.IsNullOrEmpty(squadName) && Mouse.IsOver(squadNameRect))
            {
                TooltipHandler.TipRegion(squadNameRect, () => squadName, 6844867);
            }
        }

        reusedRect = new(squadNameRect.x + 16f, squadNameRect.yMax + 4f, 25f, 30f);
        string relation;
        if (branch.IsBranchOfType(BranchType.Friendly))
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
        DrawBranchStateIcon(reusedRect, branch, expand: false);

        reusedRect = CenterRectOnX(reusedRect, reusedRect.yMax + 4f, 60f, 20f);
        string workState = branch.CurWorkStateDesc;
        if (Text.CalcSize(workState).x < 40f)
        {
            Widgets.Label(reusedRect, workState);
        }
        else
        {
            Widgets.LabelEllipses(reusedRect, workState);
            if (!String.IsNullOrEmpty(workState) && Mouse.IsOver(reusedRect))
            {
                TooltipHandler.TipRegion(reusedRect, () => workState, 3548681);
            }
        }

        Text.Anchor = TextAnchor.MiddleLeft;

        Rect rightRect = Rect.MinMaxRect(leftRect.xMax, inRect.yMin, inRect.xMax, inRect.yMax);
        float textX = rightRect.xMin + 24f;
        reusedRect = new(textX, rightRect.y, rightRect.width, 29f);
        Widgets.Label(reusedRect, "OARO_AllCrewCountShortInfo".Translate(branch.Squad.AllCrewCountInt));
        reusedRect = new(textX, reusedRect.yMax, rightRect.width, 29f);
        Widgets.Label(reusedRect, "OARO_BranchPotencyShortInfo".Translate(branch.Potency.ToString("0.##")));
        reusedRect = new(textX, reusedRect.yMax, rightRect.width, 29f);
        string supplyState = "OARO_BranchSupplyState".Translate() + "  " + branch.SupplyState;
        Widgets.Label(reusedRect, supplyState);

        Text.Anchor = preAnchor;
        Text.Font = preFont;
        return rect;
    }

    public static void DrawRecommendationInfo(Rect inRect, int count, float textOffset = 0f)
    {
        TextAnchor preAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleLeft;

        Rect reusedRect = new(inRect.x, inRect.y, inRect.height, inRect.height);
        GUI.DrawTexture(reusedRect, IconLibrary.RecommendationIcon, ScaleMode.ScaleToFit);

        reusedRect = Rect.MinMaxRect(reusedRect.xMax + textOffset, inRect.yMin, inRect.xMax, inRect.yMax);
        Widgets.Label(reusedRect, $"× {count}");

        Text.Anchor = preAnchor;
    }

    public static void DrawBranchIcon(Rect inRect, Branch branch, bool expand)
    {
        if (branch?.HonorDef is null)
        {
            GUI.DrawTexture(inRect, expand ? IconLibrary.BigGeneralBranchIcon : IconLibrary.SmallGeneralBranchIcon, ScaleMode.ScaleToFit);
        }
        else
        {
            GUI.DrawTexture(inRect, expand ? branch.HonorDef.iconTexture.ExpandedTexture : branch.HonorDef.iconTexture.Texture, ScaleMode.ScaleToFit);
        }
    }

    public static void DrawBranchStateIcon(Rect inRect, Branch branch, bool expand)
    {
        switch (branch.CurWorkState)
        {
            case WorkStateType.Idle:
                {
                    GUI.DrawTexture(inRect, expand ? IconLibrary.BigIdleIcon : IconLibrary.SmallIdleIcon, ScaleMode.ScaleToFit);
                    return;
                }
            case WorkStateType.OnBaseTask:
                {
                    GUI.DrawTexture(inRect, expand ? IconLibrary.BigOnBaseIcon : IconLibrary.SmallOnBaseIcon, ScaleMode.ScaleToFit);
                    return;
                }
            case WorkStateType.AbroadTask:
                {
                    GUI.DrawTexture(inRect, expand ? IconLibrary.BigAbroadIcon : IconLibrary.SmallAbroadIcon, ScaleMode.ScaleToFit);
                    return;
                }
            default: return;
        }
    }

    public static void DrawKnightChivalryIcon(Rect inRect, KnightChivalryDef taskChivalry, bool primary)
    {
        if (taskChivalry is not null)
        {
            GUI.DrawTexture(inRect, primary ? taskChivalry.primaryIcon.Texture : taskChivalry.icon.Texture, ScaleMode.ScaleToFit);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetText()
    {
        Text.WordWrap = true;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    public static void DrawTextureWithMaterial(Rect rect, Texture texture, Material material, ScaleMode scaleMode = ScaleMode.StretchToFill)
    {
        if (material == null)
        {
            GUI.DrawTexture(rect, texture, scaleMode);
        }
        else if (Event.current.type == EventType.Repaint)
        {
            Color color = material.shader.SupportsMaskTex() ? GUI.color : new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, GUI.color.a);
            Rect screenRect = default;
            Rect sorceRect = default;
            float imageAspect = texture.width / (float)texture.height;
            CalculateScaledTextureRects(rect, scaleMode, imageAspect, ref screenRect, ref sorceRect);
            Graphics.DrawTexture(screenRect, texture, sorceRect, 0, 0, 0, 0, color, material);
        }
    }

    /// <summary>
    /// UnityEngine.GUI.CalculateScaledTextureRects的实现
    /// </summary>
    private static bool CalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
    {
        float positionAspect = position.width / position.height;
        bool result = false;
        switch (scaleMode)
        {
            case ScaleMode.StretchToFill:
                outScreenRect = position;
                outSourceRect = new Rect(0f, 0f, 1f, 1f);
                result = true;
                break;
            case ScaleMode.ScaleAndCrop:
                if (positionAspect > imageAspect)
                {
                    float scaleFactor = imageAspect / positionAspect;
                    outScreenRect = position;
                    outSourceRect = new Rect(0f, (1f - scaleFactor) * 0.5f, 1f, scaleFactor);
                    result = true;
                }
                else
                {
                    float scaleFactor = positionAspect / imageAspect;
                    outScreenRect = position;
                    outSourceRect = new Rect(0.5f - scaleFactor * 0.5f, 0f, scaleFactor, 1f);
                    result = true;
                }
                break;
            case ScaleMode.ScaleToFit:
                if (positionAspect > imageAspect)
                {
                    float scaleFactor = imageAspect / positionAspect;
                    outScreenRect = new Rect(position.xMin + position.width * (1f - scaleFactor) * 0.5f, position.yMin, scaleFactor * position.width, position.height);
                    outSourceRect = new Rect(0f, 0f, 1f, 1f);
                    result = true;
                }
                else
                {
                    float scaleFactor = positionAspect / imageAspect;
                    outScreenRect = new Rect(position.xMin, position.yMin + position.height * (1f - scaleFactor) * 0.5f, position.width, scaleFactor * position.height);
                    outSourceRect = new Rect(0f, 0f, 1f, 1f);
                    result = true;
                }
                break;
        }
        return result;
    }
}