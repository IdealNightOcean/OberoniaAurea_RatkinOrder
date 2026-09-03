using System;
using System.Collections.Generic;
using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public class Window_VirtueTraitSelection : OrderWindowBase
{
    public UIData_KnightVirtue DrawData { get; }
    public KnightVirtue Virtue { get; }
    public int TargetLevel { get; }
    public IReadOnlyList<KnightVirtueTraitDef> TraitOptions { get; }

    public Window_VirtueTraitSelection(UIData_KnightVirtue drawData, int level) : base()
    {
        this.DrawData = drawData;
        this.TargetLevel = level;
        this.TraitOptions = drawData.Virtue.Def.GetTraitOptionsForLevel(level);
    }

    public override void DoWindowContents(Rect inRect)
    {
        throw new NotImplementedException();
    }

}