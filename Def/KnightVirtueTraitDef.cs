using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士美德词条Def
/// </summary>
public class KnightVirtueTraitDef : Def
{
    public List<StatModifier> statOffsets;

    public List<StatModifier> statFactors;

    public List<StatModifierBySeverity> statOffsetsByVirtue;

    public List<StatModifierBySeverity> statFactorsByVirtue;
}