using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder.UI;

public class UIDrawer_KnightVirtue : IUIDrawer
{
    public Vector2 DefaultSize => new(362f, 182f);
    public TextStyle TextStyle { get; set; } = TextStyle.DefaultStyle;

    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }

    public UIDrawer_KnightVirtue(ResidentKnight knight, KnightVirtue virtue)
    {
        this.Knight = knight;
        this.Virtue = virtue;
    }

    public void Draw(Vector2 position)
    {
        Rect boxRect = new(position.x, position.y, DefaultSize.x, DefaultSize.y);
        Rect innerBoxRect = boxRect.ContractedBy(1f);
        Widgets.DrawBoxSolid(boxRect, OARO_ColorLibrary.MediumDarkBackground);

        float verticalLineX = innerBoxRect.xMin + innerBoxRect.width * (1f / 3f);
        float horizontalLine2Y = innerBoxRect.yMin + innerBoxRect.height * (1f / 3f);
        float horizontalLine3Y = innerBoxRect.yMin + innerBoxRect.height * (2f / 3f);

        Rect labelRect = innerBoxRect;
        labelRect.xMax = verticalLineX;

        this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(labelRect, this.Virtue.Def.LabelCap, this.TextStyle);

        float levelRectHeight = innerBoxRect.height / 3f;
        for (int i = 0; i < this.Virtue.Def.maxLevel; i++)
        {
            Rect levelRect = innerBoxRect;
            levelRect.yMin = innerBoxRect.yMin + i * levelRectHeight;
            levelRect.yMax = levelRect.yMin + levelRectHeight;

            DrawVirtueTrait(inRect: levelRect, level: i + 1);

            OAFrame_Widgets.DrawLineHorizontal(new Vector2(levelRect.xMin, levelRect.yMax), levelRect.width, OARO_ColorLibrary.CommonOutline);
        }

        OAFrame_Widgets.DrawBox(boxRect, OARO_ColorLibrary.CommonOutline, thickness: 1);

        Widgets.DrawLine(start: new Vector2(verticalLineX, innerBoxRect.yMin),
                 end: new Vector2(verticalLineX, innerBoxRect.yMax),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: 1f);

        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine2Y),
                 end: new Vector2(innerBoxRect.xMax, horizontalLine2Y),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: 1f);

        Widgets.DrawLine(start: new Vector2(verticalLineX, horizontalLine3Y),
                 end: new Vector2(innerBoxRect.xMax, horizontalLine3Y),
                 color: OARO_ColorLibrary.CommonOutline,
                 width: 1f);
    }

    private void DrawVirtueTrait(Rect inRect, int level)
    {
        float verticalLineX = inRect.xMin + inRect.width * 0.15f;
        Widgets.DrawLine(start: new Vector2(verticalLineX, inRect.yMin),
                         end: new Vector2(verticalLineX, inRect.yMax),
                         color: OARO_ColorLibrary.CommonOutline,
                         width: 1f);

        Rect levelLabeleRect = inRect;
        levelLabeleRect.xMax = verticalLineX;

        Rect traitInfoRect = inRect;
        traitInfoRect.xMin = verticalLineX + 1f;

        if (this.Virtue.Level < level)
        {
            this.TextStyle = new(GameFont.Medium, TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitInfoRect, "OARO_NotUnlocked".Translate(), this.TextStyle);
            return;
        }

        KnightVirtueTraitDef virtueTrait = this.Virtue.GetTraitOfLevel(level);
        if (virtueTrait is not null)
        {
            DarwVirtueTaritInfo(traitInfoRect, virtueTrait);
            return;
        }

        if (level < this.Virtue.Def.traitGroups.Count + 1)
        {
            DarwVirtueTaritSelection(traitInfoRect, level);
        }
        else
        {
            this.TextStyle = new(OARO_ColorLibrary.DimInactive, GameFont.Medium, TextAnchor.MiddleCenter);
            OAFrame_Widgets.DrawLabel(traitInfoRect, "None".Translate(), this.TextStyle);
        }
    }

    private void DarwVirtueTaritInfo(Rect inRect, KnightVirtueTraitDef virtueTrait)
    {
        Rect iconRect = inRect;
        iconRect.width *= 0.24f;


        Rect descRect = inRect;
        descRect.xMin = iconRect.xMax;
        this.TextStyle = new(GameFont.Small, TextAnchor.MiddleCenter);
        OAFrame_Widgets.DrawLabel(descRect, virtueTrait.description, this.TextStyle);
    }

    private void DarwVirtueTaritSelection(Rect inRect, int level)
    {

    }
}

public class Window_VirtueTaritSelection : OrderWindowBase
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }
    public int Level { get; }
    public KnightVirtueTraitGroups TraitGroup { get; }

    public Window_VirtueTaritSelection(ResidentKnight knight, KnightVirtue virtue, int level) : base()
    {
        this.Knight = knight;
        this.Virtue = virtue;
        this.Level = level;
        this.TraitGroup = virtue.Def.traitGroups[level - 1];
    }

    public override void DoWindowContents(Rect inRect)
    {
        throw new NotImplementedException();
    }

}