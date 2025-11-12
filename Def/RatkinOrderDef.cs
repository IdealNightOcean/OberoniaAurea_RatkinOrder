using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RatkinOrderDef : Def
{
    [MustTranslate]
    public string fixedName;

    public RulePackDef nameMaker;

    public RulePackDef branchNameCoreSelecter;

    public Color? color;

    public List<PawnGroupOption> pawnGroupOptions;


    public bool TryGetRandomPawnGroupMaker(PawnGroupKindDef pawnGroupKindDef, out PawnGroupOption groupOption)
    {
        if (pawnGroupOptions.NullOrEmpty())
        {
            groupOption = null;
            return false;
        }
        return pawnGroupOptions.Where(g => g.kindDef == pawnGroupKindDef)
                               .TryRandomElementByWeight(g => g.commonality, out groupOption);
    }

}