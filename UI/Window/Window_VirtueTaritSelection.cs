using System;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public class Window_VirtueTaritSelection : OrderWindowBase
{
    public ResidentKnight Knight { get; }
    public KnightVirtue Virtue { get; }
    public int TargetLevel { get; }
    public KnightVirtueTraitGroups TraitGroup { get; }

    public Window_VirtueTaritSelection(ResidentKnight knight, KnightVirtue virtue, int level) : base()
    {
        this.Knight = knight;
        this.Virtue = virtue;
        this.TargetLevel = level;
        this.TraitGroup = virtue.Def.traitGroups[level - 1];
    }

    public override void DoWindowContents(Rect inRect)
    {
        throw new NotImplementedException();
    }

}