using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchHonorDef : Def
{
    public Color color;

    public HediffDef buffHediff;

    public ResidentKnightAcademicDef academicDef;

    public List<PawnGroupOption> pawnGroupOptions;

    [NoTranslate]
    protected string iconPath;

    protected Texture2D iconTexture;
    public Texture2D IconTexture
    {
        get
        {
            if (iconTexture is null)
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    return null;
                }
                iconTexture = ContentFinder<Texture2D>.Get(iconPath);
            }
            return iconTexture;
        }
    }

    protected Texture2D expandingIconTexture;
    public Texture2D ExpandingIconTexture
    {
        get
        {
            if (expandingIconTexture is null)
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    return null;
                }
                expandingIconTexture = ContentFinder<Texture2D>.Get(iconPath + "_Expand");
            }
            return expandingIconTexture;
        }
    }

    [NoTranslate]
    protected string decorationPath;
    protected Texture2D decorationTexture;
    public Texture2D DecorationTexture
    {
        get
        {
            if (decorationPath is null)
            {
                if (string.IsNullOrEmpty(decorationPath))
                {
                    return null;
                }
                decorationTexture = ContentFinder<Texture2D>.Get(decorationPath);
            }
            return decorationTexture;
        }
    }
    protected Texture2D expandingDecorationTexture;
    public Texture2D ExpandingDecorationTexture
    {
        get
        {
            if (decorationPath is null)
            {
                if (string.IsNullOrEmpty(decorationPath))
                {
                    return null;
                }
                expandingDecorationTexture = ContentFinder<Texture2D>.Get(decorationPath + "_Expand");
            }
            return expandingDecorationTexture;
        }
    }

    [NoTranslate]
    protected string honorBarPath;
    protected Texture2D honorBarTexture;
    public Texture2D HonorBarTexture => honorBarTexture ??= SolidColorMaterials.NewSolidColorTexture(color);

    [NoTranslate]
    protected string backgroundPath;
    protected Texture2D backgroundTexture;
    public Texture2D BackgroundTexture
    {
        get
        {
            if (backgroundTexture is null)
            {
                if (string.IsNullOrEmpty(backgroundPath))
                {
                    return null;
                }
                backgroundTexture = ContentFinder<Texture2D>.Get(backgroundPath);
            }
            return backgroundTexture;
        }
    }

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