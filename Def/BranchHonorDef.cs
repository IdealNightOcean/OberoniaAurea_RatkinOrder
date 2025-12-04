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
    /// 荣誉颜色
    /// </summary>
    public Color color;
    protected Texture2D honorColorTex;
    /// <summary>
    /// 荣誉颜色标识图标，颜色使用<see cref="color"/>
    /// </summary>
    public Texture2D HonorColorTex => honorColorTex ??= SolidColorMaterials.NewSolidColorTexture(color);

    /// <summary>
    /// 荣誉图标
    /// </summary>
    public PathedTexture2DWithExpanded iconTexture;

    /// <summary>
    /// 荣誉装饰框图标
    /// </summary>
    public PathedTexture2DWithExpanded decorationTexture;

    /// <summary>
    /// 荣誉背景图标
    /// </summary>
    public PathedTexture2D backgroundTexture;

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