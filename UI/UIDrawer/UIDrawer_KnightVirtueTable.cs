using NightOcean.Utility;
using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDrawer_KnightVirtueTable : IUIDrawer
{
    public Vector2 DrawSize { get; protected set; } = new(915f + 18f, 558f);
    public int OutlineThickness => 0;

    public TextStyle TextStyle { get; set; } = TextStyle.DefaultStyle;

    private Vector2 scrollPosition_TableBody;

    private readonly Rect[] reusedRectArr = new Rect[5];

    public ResidentKnight Knight { get; protected set; }
    public HashSet<KnightVirtueDef> ActivedVirtues { get; } = [];

    public void SetDrawSize(Vector2 size) => DrawSize = size;

    public UIDrawer_KnightVirtueTable() { }

    public void SetKnight(ResidentKnight knight)
    {
        this.Knight = knight;
        ActivedVirtues.Clear();
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

        List<KnightVirtueDef> allVirtueDefs = DefDatabase<KnightVirtueDef>.AllDefsListForReading;

        this.TextStyle = new(font: GameFont.Medium, anchor: TextAnchor.MiddleCenter);
        if (this.Knight is not null)
        {
            OAFrame_Widgets.DrawLabel(titleRect, $"{"OARO_KnightVirtueTableTitle".Translate()}  {ActivedVirtues.Count}/{allVirtueDefs.Count}", this.TextStyle);
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
        OAFrame_Widgets.DrawLabel(reusedRectArr[0], "OARO_KnightVirtue_Label".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[1], "OARO_KnightVirtue_UnlockMethod".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[2], "OARO_KnightVirtue_Desc".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[3], $"{"OARO_KnightVirtue_MaxTrait".Translate()} I", this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[4], $"{"OARO_KnightVirtue_MaxTrait".Translate()} II", this.TextStyle);

        float rowHeight = 64f;

        Rect bodyOutRect = tableOutRect;
        bodyOutRect.yMin = headerRect.yMax - 1f;

        float entryInterval = rowHeight - 1f; //上下边界要重叠
        Rect bodyViewRect = new(0f, 0f, tableRect.width, allVirtueDefs.Count * entryInterval + 8f);

        Widgets.BeginScrollView(bodyOutRect, ref scrollPosition_TableBody, bodyViewRect);

        Rect visibleRect = new(scrollPosition_TableBody, bodyOutRect.size);
        Rect rowRect = new(0f, 0f, bodyViewRect.width, rowHeight);

        for (int i = 0; i < allVirtueDefs.Count; i++)
        {
            if (visibleRect.Overlaps(rowRect))
            {
                DrawTableRow(rowRect, allVirtueDefs[i]);
            }

            rowRect = rowRect.OffsetVertical(entryInterval);
        }

        Widgets.EndScrollView();
    }

    private void DrawTableRow(Rect rowRect, KnightVirtueDef virtueDef)
    {
        BuildTableRaw(rowRect);
        bool actived = this.Knight is null || this.ActivedVirtues.Contains(virtueDef);
        this.TextStyle = new(guiColor: actived ? Color.white : Color.gray, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(reusedRectArr[0], virtueDef.LabelCap, this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[1], $"OARO_KnightVirtue_UnlockMethod_{virtueDef.virtueType}".Translate(), this.TextStyle);
        OAFrame_Widgets.DrawLabel(reusedRectArr[2], virtueDef.description, this.TextStyle);

        this.TextStyle = new(guiColor: actived ? Color.green : Color.gray, font: GameFont.Small, anchor: TextAnchor.MiddleCenter);
        IReadOnlyList<KnightVirtueTraitDef> maxTraitOptions = virtueDef.GetTraitOptionsForLevel(virtueDef.MaxLevel);
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
