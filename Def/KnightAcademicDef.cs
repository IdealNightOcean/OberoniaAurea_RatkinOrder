using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士课业Def
/// </summary>
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

    /// <summary>对应骑士精神</summary>
    public KnightChivalryDef chivalry;

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
    public void OnAcademicLevelUpgrade(Pawn pawn, int oldLevel, int newLevel)
    {
        if (newLevel < 1 || newLevel > MaxStageLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(newLevel));
        }

        if (buffHediffDef is null)
        {
            return;
        }
        Hediff_ResidentAcademicBuff hediff = pawn.health.GetOrAddHediff(buffHediffDef) as Hediff_ResidentAcademicBuff;
        hediff.Notify_AcademicStageChanged(newLevel);

        academicStages[newLevel - 1].OnAcademicLevelUpgrade(pawn);
    }
}