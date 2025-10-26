using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Hediff_ResidentKnightBuff : Hediff
{
    public override int CurStageIndex => buffStageIndex;

    private int buffStageIndex;

    public void SetBuffStage(int buffStageIndex)
    {
        this.buffStageIndex = Mathf.Min(def.stages?.Count ?? 0, buffStageIndex);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref buffStageIndex, "buffStageIndex", 0);
    }
}

public class ResidentKnightAcademicDef : Def
{
    /// <summary>
    /// 不要使用组合枚举！
    /// </summary>
    public KnightRecord.PersonalityType knightPersonality;

    public List<ResidentKnightAcademicStage> academicStages = [];

    public int MaxStageLevel => academicStages.Count;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (knightPersonality == KnightRecord.PersonalityType.None)
        {
            yield return "";
        }
    }
}


public class ResidentKnightAcademicStage
{
    public Hediff buffHediff;
    public float buffHediffStage;

}

public class ResidentKnight
{
    public enum Rank : byte
    {
        Regular,
        Elite,
        Honor,
        Crown
    }

    private Branch branch;
    private Rank curRank;
    private float meditationPoints;

    public ResidentKnightRoleDef CurRole;

    public Branch Branch => branch;
    public Rank CurRank => curRank;
    public float MeditationPoints => meditationPoints;


    private ResidentKnightAcademicDef genealAcademicDef;
    public ResidentKnightAcademicDef HonorAcademicDef => branch.HonorProperties?.academicDef;

    private int genealAcademicLevel;
    private int honorAcademicLevel;
    public int TotalAcademicLevel => genealAcademicLevel + honorAcademicLevel;


    private ResidentKnight() { }

    public ResidentKnight(Branch branch)
    {
        this.branch = branch ?? throw new ArgumentNullException(nameof(branch));
    }

    public override string ToString()
    {
        return $"Branch: {branch.Name}, Rank: {curRank}, MeditationPoints: {meditationPoints}, AcademicDef: ({genealAcademicDef},{HonorAcademicDef}),TotalAcademicLevel: {TotalAcademicLevel}({genealAcademicLevel}, {honorAcademicLevel}), Role: {CurRole} ";
    }

    private int NoAdditionalCostAcademicCeiling()
    {
        return curRank switch
        {
            Rank.Regular => 5,
            Rank.Elite => 10,
            Rank.Honor => 20,
            Rank.Crown => 60,
            _ => 60
        };
    }
}