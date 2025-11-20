using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightAcademicDef : Def
{
    /// <summary>
    /// 不要使用组合枚举！
    /// </summary>
    public KnightPersonality knightPersonality;

    public List<ResidentKnightAcademicStage> academicStages = [];

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

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (knightPersonality == KnightPersonality.None)
        {
            yield return "";
        }
    }

    public override void PostLoad()
    {
        base.PostLoad();
        if (knightPersonality != KnightPersonality.None)
        {
            OrderDefDataBase.AddKnightAcademicByPersonality(knightPersonality, this);
        }
    }
}


public class ResidentKnightAcademicStage
{
    public HediffDef buffHediff;
    public int buffHediffStage;


    //只执行一次，在升级时执行
    public virtual void OnAcademicLevelUp(Pawn pawn)
    {
        if (buffHediff is not null)
        {
            Hediff_ResidentKnightBuff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(buffHediff) as Hediff_ResidentKnightBuff;
            if (hediff is not null)
            {

            }
            else
            {
                hediff = pawn.health.AddHediff(buffHediff) as Hediff_ResidentKnightBuff;
                hediff?.SetBuffStage(buffHediffStage);
            }
        }
    }
}
