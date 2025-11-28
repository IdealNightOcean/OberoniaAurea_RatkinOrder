using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchHonorDef : Def
{
    /// <summary>
    /// 荣誉颜色
    /// </summary>
    public Color color;

    /// <summary>
    /// 荣誉Buff（<see cref="HediffDef"/>）
    /// </summary>
    public HediffDef buffHediff;

    /// <summary>
    /// 荣誉课业（<see cref="ResidentKnightAcademicDef"/>）
    /// </summary>
    public ResidentKnightAcademicDef academicDef;

    /// <summary>
    /// 荣誉分部特殊的人物生成组
    /// </summary>
    public List<PawnGroupOption> pawnGroupOptions;

    /// <summary>
    /// 荣誉图标路径
    /// </summary>
    [NoTranslate]
    protected string iconPath;
    protected Texture2D iconTexture;
    /// <summary>
    /// 荣誉图标
    /// </summary>
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
    /// <summary>
    /// 拓展的荣誉图标路径
    /// </summary>
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

    /// <summary>
    /// 荣誉装饰框图标路径
    /// </summary>
    [NoTranslate]
    protected string decorationPath;
    protected Texture2D decorationTexture;
    /// <summary>
    /// 荣誉装饰框图标路径
    /// </summary>
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
    /// <summary>
    /// 扩展的荣誉装饰框图标
    /// </summary>
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


    protected Texture2D honorBarTexture;
    /// <summary>
    /// 荣誉颜色标识图标，颜色使用<see cref="color"/>
    /// </summary>
    public Texture2D HonorBarTexture => honorBarTexture ??= SolidColorMaterials.NewSolidColorTexture(color);

    /// <summary>
    /// 荣誉背景图标路径
    /// </summary>
    [NoTranslate]
    protected string backgroundPath;
    protected Texture2D backgroundTexture;
    /// <summary>
    /// 荣誉背景图标
    /// </summary>
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