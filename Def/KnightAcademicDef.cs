using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightAcademicDef : Def
{
    public enum AcademicType
    {
        /// <summary>
        /// 普通课业
        /// </summary>
        Geneal,
        /// <summary>
        /// 荣誉课业
        /// </summary>
        Honor,
        /// <summary>
        /// 传统课业
        /// </summary>
        Traditional
    }

    /// <summary>对应骑士个性</summary>
    ///<remarks>- 不要使用组合枚举！！！</remarks>
    public KnightPersonality personality;

    /// <summary>
    /// 课业类型
    /// </summary>
    public AcademicType academicType;

    public HediffDef buffHediffDef;

    /// <summary>
    /// 课业阶段
    /// </summary>
    public List<ResidentKnightAcademicStage> academicStages = [];
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
    public void OnAcademicLevelUpgrade(Pawn pawn, int targetLevel)
    {
        if (targetLevel < 1 || targetLevel > MaxStageLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(targetLevel));
        }

        if (buffHediffDef is null)
        {
            return;
        }
        Hediff_ResidentAcademicBuff hediff = pawn.health.GetOrAddHediff(buffHediffDef) as Hediff_ResidentAcademicBuff;
        hediff.Notify_AcademicStageChanged(targetLevel);

        academicStages[targetLevel - 1].OnAcademicLevelUpgrade(pawn);
    }
}