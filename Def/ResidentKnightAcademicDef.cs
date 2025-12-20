using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightAcademicDef : Def
{
    /// <summary>对应骑士个性</summary>
    ///<remarks>- 不要使用组合枚举！！！</remarks>
    public KnightPersonality personality;

    public HediffDef buffHediffDef;

    /// <summary>
    /// 课业阶段
    /// </summary>
    public List<ResidentKnightAcademicStage> academicStages = [];

    /// <summary>
    /// 是否为荣誉课业
    /// </summary>
    public bool isHonorAcademic;

    public int MaxStageLevel => academicStages.Count;

    public ResidentKnightAcademicStage GetStage(int level)
    {
        if (level < 1 || level > academicStages.Count)
        {
            return null;
        }
        return academicStages[level - 1];
    }

    /// <summary>
    /// 只执行一次，在升级时执行
    /// </summary>
    public void OnAcademicLevelUpgrade(Pawn pawn, int targetStageIndex)
    {
        if (targetStageIndex < 0 || targetStageIndex > MaxStageLevel - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetStageIndex));
        }

        if (buffHediffDef is null)
        {
            return;
        }
        Hediff_ResidentAcademicBuff hediff = pawn.health.GetOrAddHediff(buffHediffDef) as Hediff_ResidentAcademicBuff;
        hediff.Notify_AcademicStageChanged(targetStageIndex);

        academicStages[targetStageIndex].OnAcademicLevelUpgrade(pawn);
    }
}


public class ResidentKnightAcademicStage
{
    [MustTranslate]
    public string label;
    [MustTranslate]
    public string shortDescription;
    [MustTranslate]
    public string description;

    public virtual void OnAcademicLevelUpgrade(Pawn pawn) { }
}
