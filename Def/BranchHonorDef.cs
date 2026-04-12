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
    /// 荣誉课业（<see cref="KnightAcademicDef"/>）
    /// </summary>
    public KnightAcademicDef academicDef;

    /// <summary>
    /// 核心印记Def（<see cref="BranchMedalDef"/>）
    /// </summary>
    public BranchMedalDef medalDef;

    /// <summary>
    /// 荣誉加成的专注任务类型
    /// </summary>
    public BranchTaskType focusedTaskType;

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

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }

        if (academicDef is not null && academicDef.academicType != KnightAcademicDef.AcademicType.Honor)
        {
            yield return $"设置了{nameof(academicDef)}，但 {nameof(academicDef)} 的 {nameof(KnightAcademicDef.academicType)} 不为 {KnightAcademicDef.AcademicType.Honor}";
        }
    }
}