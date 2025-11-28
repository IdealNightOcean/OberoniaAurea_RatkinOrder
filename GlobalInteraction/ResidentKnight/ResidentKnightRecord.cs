using NightOcean;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRecord : IExposable, ILoadReferenceable
{
    public enum Rank : byte
    {
        Regular,
        Elite,
        Honor,
        Crown
    }
    public static Rank RankOffsetBy(Rank rank, int offset) => (Rank)Mathf.Clamp((int)rank + offset, 0, 3);

    private int loadID = -1;
    public int LoadID => loadID;

    private Pawn knight;
    public Pawn Knight => knight;

    private KnightRecord knightRecord;
    public KnightRecord KnightRecord => knightRecord;

    public bool IsValid => !knight.DestroyedOrNull() && !knight.Dead && knightRecord is not null;
    public bool ShouldRemove => knight is null || knightRecord is null;

    public RatkinOrder RatkinOrder => knightRecord.RatkinOrder;
    public Branch Branch => knightRecord.Branch;

    public Rank CurRank;
    public float MeditationPoints;
    public ResidentKnightRoleDef CurRole;

    public KnightPersonality Personality => KnightRecord.Personality;

    private Dictionary<ResidentKnightAcademicDef, int> genealAcademicDefs = [];
    public IReadOnlyDictionary<ResidentKnightAcademicDef, int> GenealAcademicDefs => genealAcademicDefs;

    public ResidentKnightAcademicDef HonorAcademicDef => Branch.HonorDef?.academicDef;

    private int honorAcademicLevel;
    public int HonorAcademicLevel => honorAcademicLevel;

    public readonly LazyMutable<int> TotalAcademicLevel;

    private int residenceStartTick = -1;
    public int ResignationDaysLeft = -1;
    private bool hasWarnedResignation;

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);
        Scribe_References.Look(ref knight, "knight");
        Scribe_References.Look(ref knightRecord, "knightRecord");

        Scribe_Values.Look(ref CurRank, "CurRank", Rank.Regular);
        Scribe_Values.Look(ref MeditationPoints, "MeditationPoints", 0f);
        Scribe_Defs.Look(ref CurRole, "CurRole");
        Scribe_Collections.Look(ref genealAcademicDefs, "genealAcademicDefs", LookMode.Def, LookMode.Value);
        Scribe_Values.Look(ref honorAcademicLevel, "honorAcademicLevel", 0);

        Scribe_Values.Look(ref residenceStartTick, "residenceStartTick", -1);
        Scribe_Values.Look(ref ResignationDaysLeft, "ResignationDaysLeft", -1);
        Scribe_Values.Look(ref hasWarnedResignation, "hasWarnedResignation", defaultValue: false);
    }

    private ResidentKnightRecord()
    {
        TotalAcademicLevel = new(refreshFunc: () => honorAcademicLevel + genealAcademicDefs.Values.Sum());
    }

    public ResidentKnightRecord(Pawn knight, Branch branch) : base()
    {
        if (branch is null)
        {
            throw new ArgumentNullException(nameof(branch));
        }

        this.knight = knight;
        residenceStartTick = Find.TickManager.TicksGame;
        if (branch.RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            ResignationDaysLeft = 4 * 60;
        }
        else
        {
            ResignationDaysLeft = 2 * 60;
        }

        loadID = UniqueIDManager.GetUniqueID("ResidentKnight");
    }

    public override string ToString()
    {
        return $"Branch: {Branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, Role: {CurRole} ";
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

    public AcceptanceReport CanUpgradeAcademicLevel(ResidentKnightAcademicDef academicDef, bool ignorePoints, bool resultOnly)
    {
        int academicLevel;
        if (academicDef.isHonorAcademic)
        {
            if (academicDef != HonorAcademicDef)
            {
                return resultOnly ? false : "OARO_NotCorrespondingHonorAcademicDef".Translate();
            }
            academicLevel = honorAcademicLevel;
        }
        else
        {
            if (!genealAcademicDefs.TryGetValue(academicDef, out academicLevel))
            {
                academicLevel = 0;
            }
        }
        if (academicLevel > academicDef.MaxStageLevel)
        {
            return resultOnly ? false : "OARO_ReachMax_AcademicLevel".Translate();
        }

        if (!ignorePoints)
        {
            float neededPoints = GetMeditationPointsNeeded(academicDef, academicLevel + 1);
            if (MeditationPoints < neededPoints)
            {
                return resultOnly ? false : "OARO_Insufficient_MeditationPoints".Translate(neededPoints.ToString("F0"));
            }
        }

        return true;
    }

    public void UpgradeAcademicLevel(ResidentKnightAcademicDef academicDef, bool usePoints)
    {
        int targetLevel;
        if (academicDef.isHonorAcademic)
        {
            if (honorAcademicLevel >= academicDef.MaxStageLevel)
            {
                return;
            }
            targetLevel = ++honorAcademicLevel;
        }
        else
        {
            if (!genealAcademicDefs.TryGetValue(academicDef, out int academicLevel))
            {
                academicLevel = 0;
            }
            if (academicLevel >= academicDef.MaxStageLevel)
            {
                return;
            }
            targetLevel = academicLevel + 1;
            genealAcademicDefs[academicDef] = targetLevel;
        }

        TotalAcademicLevel.MarkDirty();
        if (usePoints)
        {
            float neededPoints = GetMeditationPointsNeeded(academicDef, targetLevel);
            MeditationPoints = Mathf.Max(0f, MeditationPoints - neededPoints);
        }

        academicDef.GetStage(targetLevel)?.OnAcademicLevelUp(knight);
    }

    private float GetMeditationPointsNeeded(ResidentKnightAcademicDef academicDef, int targetLevel)
    {
        if (targetLevel < 1)
        {
            return 0f;
        }

        float baseUnitCost = academicDef.isHonorAcademic ? 500f : 250f;
        float neededPoints = baseUnitCost + (targetLevel - 1) * baseUnitCost;
        if ((academicDef.knightPersonality & Personality) != 0)
        {
            neededPoints /= 2;
        }
        return neededPoints;
    }

    public string GetUniqueLoadID() => $"{nameof(ResidentKnightRecord)}_{loadID}";
}