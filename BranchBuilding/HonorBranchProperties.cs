using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

//xml相关
public class HonorBranchProperties
{
    [MustTranslate]
    public string honorName;
    [MustTranslate]
    public string honorDescription;

    public HediffDef buffHediff;

    public List<PawnGroupMaker> pawnGroupMakers;

    public bool TryGetRandomPawnGroupMaker(PawnGroupKindDef pawnGroupKindDef, out PawnGroupMaker groupMaker)
    {
        if (pawnGroupMakers.NullOrEmpty())
        {
            groupMaker = null;
            return false;
        }
        return pawnGroupMakers.Where(g => g.kindDef == pawnGroupKindDef)
                              .TryRandomElementByWeight(g => g.commonality, out groupMaker);
    }

}