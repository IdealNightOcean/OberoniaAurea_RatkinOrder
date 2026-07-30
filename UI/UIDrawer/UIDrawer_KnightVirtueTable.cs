using NightOcean.Utility;
using OberoniaAurea_Frame;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDrawer_KnightVirtueTable : IUIDrawer
{
    protected Vector2? sizeOverride;
    public Vector2 DefaultSize => new(915f + 18f, 558f);
    public Vector2 DrawSize => sizeOverride ?? DefaultSize;

    public TextStyle TextStyle { get; set; } = TextStyle.DefaultStyle;

    private Vector2 scrollPosition_TableBody;

    private readonly Rect[] reusedRectArr = new Rect[5];

    public ResidentKnight Knight { get; }
    public HashSet<KnightVirtueDef> ActivedVirtues { get; } = [];

    public void SetDrawSize(Vector2 size) => sizeOverride = size;

    public UIDrawer_KnightVirtueTable() { }
    public UIDrawer_KnightVirtueTable(ResidentKnight knight)
    {
        this.Knight = knight;
        if (knight is not null)
        {
            ActivedVirtues.AddRange(knight.VirtueHandler.Virtues.Select(v => v.Def));
        }
    }

    public void Draw(Vector2 position)
    {
        Rect boxRect = new(position, DrawSize);
        Rect titleRect = boxRect;
        titleRect.height = 32f;

        int allVirtuesCount = DefDatabase<KnightVirtueDef>.DefCount;

        this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
        if (this.Knight is not null)
        {
            OAFrame_Widgets.DrawLabel(titleRect, $"{"OARO_KnightVirtueTableTitle".Translate()}  {ActivedVirtues.Count}/{allVirtuesCount}", this.TextStyle);
        }
        else
        {
            OAFrame_Widgets.DrawLabel(titleRect, "OARO_KnightVirtueTableTitle".Translate(), this.TextStyle);
        }

        Rect tableOutRect = boxRect;
        tableOutRect.yMin = titleRect.yMax + 16f;

        Rect tableRect = tableOutRect;
        tableRect.xMax -= 18f;

        Rect headerRect = tableRect;
        headerRect.height = 32f;
        BuildTableRaw(headerRect);
        this.TextStyle = new(font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(reusedRectArr[0], "OARO_KnightVirtueLabel".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[1], "OARO_KnightVirtueUnlockMethod".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[2], "OARO_KnightVirtueDesc".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[3], $"{"OARO_MaxKnightVirtueTrait".Translate()} I", this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[4], $"{"OARO_MaxKnightVirtueTrait".Translate()} II", this.TextStyle);

        float rowHeight = 64f;

        Rect bodyOutRect = tableOutRect;
        bodyOutRect.yMin = headerRect.yMax - 1f;

        Rect bodyViewRect = tableRect;
        bodyViewRect.yMin = headerRect.yMax - 1f;
        bodyViewRect.height = rowHeight * allVirtuesCount - (allVirtuesCount - 1) + 8f;

        Widgets.BeginScrollView(bodyOutRect, ref scrollPosition_TableBody, bodyViewRect);

        List<KnightVirtueDef> allVirtueDefs = DefDatabase<KnightVirtueDef>.AllDefsListForReading;
        Rect rowRect = new(bodyViewRect.xMin, bodyViewRect.yMin, bodyViewRect.width, rowHeight);
        for (int i = 0; i < allVirtueDefs.Count; i++)
        {
            DrawTableRow(rowRect, allVirtueDefs[i]);
            rowRect.OffsetVertical(rowHeight - 1f); //上下边界要重叠
        }

        Widgets.EndScrollView();
    }

    private void DrawTableRow(Rect rowRect, KnightVirtueDef virtueDef)
    {
        BuildTableRaw(rowRect);
        bool actived = this.Knight is null || this.ActivedVirtues.Contains(virtueDef);
        this.TextStyle = new(guiColor: actived ? Color.white : Color.gray, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(reusedRectArr[0], virtueDef.LabelCap, this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[1], $"OARO_KnightVirtueUnlockMethod_{virtueDef.virtueType}".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[2], virtueDef.description, this.TextStyle);

        this.TextStyle = new(guiColor: actived ? Color.green : Color.gray, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        IReadOnlyList<KnightVirtueTraitDef> maxTraitOptions = virtueDef.GetTraitOptionsForLevel(virtueDef.maxLevel);
        KnightVirtueTraitDef optTrait1 = maxTraitOptions.ElementAtOrDefault(0) ?? OARO_ModDefOf.OARO_BaseTrait;
        OAFrame_Widgets.DrawLabel(reusedRectArr[3], optTrait1.description, this.TextStyle);
        KnightVirtueTraitDef optTrait2 = maxTraitOptions.ElementAtOrDefault(0) ?? OARO_ModDefOf.OARO_BaseTrait;
        OAFrame_Widgets.DrawLabel(reusedRectArr[4], optTrait2.description, this.TextStyle);
    }

    private void BuildTableRaw(Rect rowRect)
    {
        OAFrame_Widgets.DrawBox(rowRect, OARO_ColorLibrary.CommonOutline);

        float rawXMin = rowRect.xMin;
        float rawWidth = rowRect.width;
        float verticalLine1X = rawXMin + rawWidth * 0.135f;
        float verticalLine2X = rawXMin + rawWidth * 0.333f;
        float verticalLine3X = rawXMin + rawWidth * 0.666f;
        float verticalLine4X = rawXMin + rawWidth * 0.833f;

        float verticalLineY = rowRect.yMin;
        float verticalLineHeight = rowRect.height;
        OAFrame_Widgets.DrawLineVertical(new(verticalLine1X, verticalLineY), verticalLineHeight, OARO_ColorLibrary.CommonOutline);
        OAFrame_Widgets.DrawLineVertical(new(verticalLine2X, verticalLineY), verticalLineHeight, OARO_ColorLibrary.CommonOutline);
        OAFrame_Widgets.DrawLineVertical(new(verticalLine3X, verticalLineY), verticalLineHeight, OARO_ColorLibrary.CommonOutline);
        OAFrame_Widgets.DrawLineVertical(new(verticalLine4X, verticalLineY), verticalLineHeight, OARO_ColorLibrary.CommonOutline);

        Rect innerRowRect = GenUI.ContractedBy(rowRect, 1f);
        reusedRectArr[0] = new(innerRowRect.xMin, innerRowRect.yMin, verticalLine1X - innerRowRect.xMin, innerRowRect.height);
        reusedRectArr[1] = new(verticalLine1X + 1f, innerRowRect.yMin, verticalLine2X - verticalLine1X - 1f, innerRowRect.height);
        reusedRectArr[2] = new(verticalLine2X + 1f, innerRowRect.yMin, verticalLine3X - verticalLine2X - 1f, innerRowRect.height);
        reusedRectArr[3] = new(verticalLine3X + 1f, innerRowRect.yMin, verticalLine4X - verticalLine3X - 1f, innerRowRect.height);
        reusedRectArr[4] = new(verticalLine4X + 1f, innerRowRect.yMin, innerRowRect.xMax - verticalLine4X - 1f, innerRowRect.height);
    }
}
