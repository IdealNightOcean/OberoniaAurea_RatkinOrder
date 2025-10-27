using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class HonorBranchDef : Def
{
    public HediffDef buffHediff;

    public ResidentKnightAcademicDef academicDef;

    public List<PawnGroupMaker> pawnGroupMakers;

    [NoTranslate]
    private string iconPath;
    private Texture2D iconTexture;
    public Texture2D IconTexture
    {
        get
        {
            if (iconTexture is null)
            {
                if (iconPath.NullOrEmpty())
                {
                    return null;
                }
                iconTexture = ContentFinder<Texture2D>.Get(iconPath);
            }
            return iconTexture;
        }
    }

    [NoTranslate]
    private string expandingIconPath;

    private Texture2D expandingIconTexture;
    public Texture2D ExpandingIconTexture
    {
        get
        {
            if (expandingIconTexture is null)
            {
                if (expandingIconPath.NullOrEmpty())
                {
                    return null;
                }
                expandingIconTexture = ContentFinder<Texture2D>.Get(expandingIconPath);
            }
            return expandingIconTexture;
        }
    }

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