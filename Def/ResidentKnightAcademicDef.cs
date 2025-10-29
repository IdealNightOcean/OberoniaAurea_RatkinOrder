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

    public int MaxStageLevel => academicStages.Count;

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
    public Hediff buffHediff;
    public float buffHediffStage;
}
