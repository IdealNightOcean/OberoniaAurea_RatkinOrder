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

    public RulePackDef branchNameMaker;

    public Color? color;

    public List<PawnGroupMaker> pawnGroupMakers;


    public bool TryGetRandomPawnGroupMaker(PawnGroupKindDef pawnGroupKindDef, out PawnGroupMaker pawnGroupMaker)
    {
        if (pawnGroupMakers.NullOrEmpty())
        {
            pawnGroupMaker = null;
            return false;
        }
        return pawnGroupMakers.Where(g => g.kindDef == pawnGroupKindDef)
                              .TryRandomElementByWeight(g => g.commonality, out pawnGroupMaker);
    }

}