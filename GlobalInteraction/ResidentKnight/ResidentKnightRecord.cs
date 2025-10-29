using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRecord : IExposable
{
    public enum Rank : byte
    {
        Regular,
        Elite,
        Honor,
        Crown
    }
    public static Rank RankOffsetBy(Rank rank, int offset) => (Rank)Mathf.Clamp((int)rank + offset, 0, 3);

    private KnightPersonality personality;
    private Branch branch;
    public Rank CurRank;
    public float MeditationPoints;

    public ResidentKnightRoleDef CurRole;

    public Branch Branch => branch;

    private ResidentKnightAcademicDef genealAcademicDef;
    public ResidentKnightAcademicDef HonorAcademicDef => branch.HonorDef?.academicDef;
    public KnightPersonality Personality => personality;

    private int genealAcademicLevel;
    private int honorAcademicLevel;

    public int GenealAcademicLevel => genealAcademicLevel;
    public int HonorAcademicLevel => honorAcademicLevel;
    public int TotalAcademicLevel => genealAcademicLevel + honorAcademicLevel;

    private int residenceStartTick = -1;
    public int ResignationDaysLeft = -1;
    private bool hasWarnedResignation;

    public void ExposeData()
    {
        Scribe_Values.Look(ref personality, "personality", KnightPersonality.None);
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref CurRank, "CurRank", Rank.Regular);
        Scribe_Values.Look(ref MeditationPoints, "MeditationPoints", 0f);
        Scribe_Defs.Look(ref CurRole, "CurRole");
        Scribe_Defs.Look(ref genealAcademicDef, "genealAcademicDef");
        Scribe_Values.Look(ref genealAcademicLevel, "genealAcademicLevel", 0);
        Scribe_Values.Look(ref honorAcademicLevel, "honorAcademicLevel", 0);

        Scribe_Values.Look(ref residenceStartTick, "residenceStartTick", -1);
        Scribe_Values.Look(ref ResignationDaysLeft, "ResignationDaysLeft", -1);
        Scribe_Values.Look(ref hasWarnedResignation, "hasWarnedResignation", defaultValue: false);
    }

    private ResidentKnightRecord() { }

    public ResidentKnightRecord(KnightRecord knightRecord, ResidentKnightAcademicDef genealAcademicDef = null)
    {
        personality = knightRecord.Personality;
        branch = knightRecord.Branch;
        this.genealAcademicDef = genealAcademicDef ?? OrderDefDataBase.GetRandomKnightAcademicOfPersonality(personality) ?? throw new ArgumentNullException(nameof(this.genealAcademicDef));
        residenceStartTick = Find.TickManager.TicksGame;
        if (branch.RatkinOrder.ReformationManager.HasReformation(OARO_ModDefOf.OARO_ReformationPlaceholder))
        {
            ResignationDaysLeft = 4 * 60;
        }
        else
        {
            ResignationDaysLeft = 2 * 60;
        }
    }

    public override string ToString()
    {
        return $"Branch: {branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, AcademicDef: ({genealAcademicDef},{HonorAcademicDef}),TotalAcademicLevel: {TotalAcademicLevel}({genealAcademicLevel}, {honorAcademicLevel}), Role: {CurRole} ";
    }

    public void ResignationWarningCheck()
    {
        if (!hasWarnedResignation && ResignationDaysLeft <= 15)
        {
            hasWarnedResignation = true;
        }
    }

    public void PostponeResignation(int postponeDays)
    {
        ResignationDaysLeft += postponeDays;
        hasWarnedResignation = false;
    }

    public int NoAdditionalCostAcademicCeiling()
    {
        return CurRank switch
        {
            Rank.Regular => 5,
            Rank.Elite => 10,
            Rank.Honor => 20,
            Rank.Crown => 60,
            _ => 60
        };
    }
}