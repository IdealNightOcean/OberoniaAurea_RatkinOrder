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

    private Pawn knight;
    public Pawn Knight => knight;

    [Unsaved] private readonly Lazy<KnightRecord> knightRecord;
    public KnightRecord KnightRecord => knightRecord.Value;

    public bool IsValid => !knight.DestroyedOrNull() && !knight.Dead && KnightRecord is not null;

    public Branch Branch => KnightRecord.Branch;

    public Rank CurRank;
    public float MeditationPoints;
    public ResidentKnightRoleDef CurRole;

    private ResidentKnightAcademicDef genealAcademicDef;
    public ResidentKnightAcademicDef HonorAcademicDef => Branch.HonorDef?.academicDef;
    public KnightPersonality Personality => KnightRecord.Personality;

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
        Scribe_References.Look(ref knight, "knight");

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

    private ResidentKnightRecord()
    {
        knightRecord = new(valueFactory: () => KnightPawnsManager.GetKnightRecord(knight), isThreadSafe: false);
    }

    public ResidentKnightRecord(Pawn knight, ResidentKnightAcademicDef genealAcademicDef = null)
    {
        this.knight = knight ?? throw new ArgumentNullException(nameof(knight));
        knightRecord = new(valueFactory: () => KnightPawnsManager.GetKnightRecord(this.knight), isThreadSafe: false);
        if (KnightRecord is null)
        {
            throw new ArgumentNullException(nameof(KnightRecord));
        }

        this.genealAcademicDef = genealAcademicDef ?? OrderDefDataBase.GetRandomKnightAcademicOfPersonality(Personality) ?? throw new ArgumentNullException(nameof(this.genealAcademicDef));
        residenceStartTick = Find.TickManager.TicksGame;
        if (KnightRecord.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
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
        return $"Branch: {Branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, AcademicDef: ({genealAcademicDef},{HonorAcademicDef}),TotalAcademicLevel: {TotalAcademicLevel}({genealAcademicLevel}, {honorAcademicLevel}), Role: {CurRole} ";
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