using NightOcean;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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

    public LazyMutable<int> TotalAcademicLevel { get; }

    private int residenceStartTick = -1;
    public int ResignationTick = -1;

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);
        Scribe_References.Look(ref knight, nameof(knight));
        Scribe_References.Look(ref knightRecord, nameof(knightRecord));

        Scribe_Values.Look(ref CurRank, nameof(CurRank), Rank.Regular);
        Scribe_Values.Look(ref MeditationPoints, nameof(MeditationPoints), 0f);
        Scribe_Defs.Look(ref CurRole, nameof(CurRole));
        Scribe_Collections.Look(ref genealAcademicDefs, nameof(genealAcademicDefs), LookMode.Def, LookMode.Value);
        Scribe_Values.Look(ref honorAcademicLevel, nameof(honorAcademicLevel), 0);

        Scribe_Values.Look(ref residenceStartTick, nameof(residenceStartTick), -1);
        Scribe_Values.Look(ref ResignationTick, nameof(ResignationTick), -1);
    }

    private ResidentKnightRecord()
    {
        TotalAcademicLevel = new(refreshFunc: () => honorAcademicLevel + genealAcademicDefs.Values.Sum());
    }

    public ResidentKnightRecord(Pawn knight, KnightRecord knightRecord) : this()
    {
        this.knight = knight;
        this.knightRecord = knightRecord;
        residenceStartTick = Find.TickManager.TicksGame;
        if (RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            ResignationTick = Find.TickManager.TicksGame + 4 * 60 * 60000;
        }
        else
        {
            ResignationTick = Find.TickManager.TicksGame + 2 * 60;
        }

        ResidentKnightAcademicDef def = DefDatabase<ResidentKnightAcademicDef>.AllDefsListForReading.Where(d => !d.isHonorAcademic).RandomElement();

        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentKnightRecord));
    }

    public override string ToString()
    {
        return $"Branch: {Branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, Role: {CurRole} ";
    }

    public void PostponeResignation(int postponeDays)
    {
        ResignationTick += (postponeDays * 60000);
        ResidentKnightsManager.Instance.ShowResignationAlert.MarkDirty();
    }

    public static int GetNoAdditionalCostAcademicCeiling(Rank rank)
    {
        return rank switch
        {
            Rank.Regular => 5,
            Rank.Elite => 10,
            Rank.Honor => 20,
            Rank.Crown => 60,
            _ => 60
        };
    }

    public static Color GetRankColor(Rank rank)
    {
        return rank switch
        {
            Rank.Regular => new Color(0.3f, 0.9f, 0.39f),
            Rank.Elite => new Color(0.3f, 0.51f, 0.9f),
            Rank.Honor => new Color(0.69f, 0.3f, 0.9f),
            Rank.Crown => new Color(1f, 0.65f, 0f),
            _ => Color.white
        };
    }

    public static string GetRankLabel(Rank rank)
    {
        return rank switch
        {
            Rank.Regular => $"OARO_ResidentKnightRank_{Rank.Regular}".Translate().Colorize(new Color(0.3f, 0.9f, 0.39f)),
            Rank.Elite => $"OARO_ResidentKnightRank_{Rank.Elite}".Translate().Colorize(new Color(0.3f, 0.51f, 0.9f)),
            Rank.Honor => $"OARO_ResidentKnightRank_{Rank.Honor}".Translate().Colorize(new Color(0.69f, 0.3f, 0.9f)),
            Rank.Crown => $"OARO_ResidentKnightRank_{Rank.Crown}".Translate().Colorize(new Color(1f, 0.65f, 0f)),
            _ => "ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable)
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

    public void PostRemoved()
    {
        knight.SetFaction(RatkinOrder.Faction);
        RatkinOrder.JointPatrolManager.OnResidentKnightRemoved(this);
        if (knight.Spawned)
        {
            LordMaker.MakeNewLord(RatkinOrder.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Walk, canDefendSelf: true), knight.Map, startingPawns: [knight]);
        }
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