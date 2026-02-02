using System;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRecord : ResidentColonistRecord
{
    public enum Rank : byte
    {
        Regular,
        Elite,
        Honor,
        Crown
    }
    public static Rank RankOffsetBy(Rank rank, int offset) => (Rank)Mathf.Clamp((int)rank + offset, 0, 3);

    private KnightRecord knightRecord;
    public KnightRecord KnightRecord => knightRecord;

    public override bool IsValid => base.IsValid && Branch.IsValid();
    public bool ShouldTransToColonist => base.IsValid && !Branch.IsValid();


    public RatkinOrder RatkinOrder => knightRecord.RatkinOrder;
    public Branch Branch => knightRecord?.Branch;

    public Rank CurRank;
    public float MeditationPoints;

    private ResidentKnightRoleDef curRole;
    private int nextRoleChangeableTick = -1;
    public ResidentKnightRoleDef CurRole => curRole;
    public int NextRoleChangeableTick => nextRoleChangeableTick;

    public KnightPersonality Personality => KnightRecord.Personality;

    private int residenceStartTick = -1;
    public int ResignationTick = -1;

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref knightRecord, nameof(knightRecord));

        Scribe_Values.Look(ref CurRank, nameof(CurRank), Rank.Regular);
        Scribe_Values.Look(ref MeditationPoints, nameof(MeditationPoints), 0f);

        Scribe_Defs.Look(ref curRole, nameof(curRole));
        Scribe_Values.Look(ref nextRoleChangeableTick, nameof(nextRoleChangeableTick), -1);

        Scribe_Values.Look(ref residenceStartTick, nameof(residenceStartTick), -1);
        Scribe_Values.Look(ref ResignationTick, nameof(ResignationTick), -1);
    }

    private ResidentKnightRecord() : base() { }
    public ResidentKnightRecord(Pawn knight, KnightRecord knightRecord) : base(knight)
    {
        this.knightRecord = knightRecord;
        residenceStartTick = Find.TickManager.TicksGame;
        if (RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            ResignationTick = Find.TickManager.TicksGame + 4 * 60 * 60000;
        }
        else
        {
            ResignationTick = Find.TickManager.TicksGame + 2 * 60 * 60000;
        }

        try
        {
            /*
            ResidentKnightAcademicDef initAcademicDef;
            if (Personality != KnightPersonality.None && OrderDefDataBase.ResidentKnightAcademicGroupByPersonality.TryGetValue(Personality, out List<ResidentKnightAcademicDef> potentialAcademics))
            {
                initAcademicDef = potentialAcademics.RandomElement();
            }
            else
            {
                initAcademicDef = DefDatabase<ResidentKnightAcademicDef>.AllDefsListForReading
                    .Where(d => d.academicType == ResidentKnightAcademicDef.AcademicType.Geneal)
                    .RandomElement();
            }
            UpgradeAcademicLevel(initAcademicDef, usePoints: false);
            */
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                $"initialize {nameof(ResidentKnightAcademicDef)}.",
                typeName: nameof(ResidentKnightRecord),
                methodName: nameof(ResidentKnightRecord),
                needStackTrace: true);
        }

        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentKnightRecord));
    }

    public override string ToString()
    {
        return $"Branch: {Branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, Role: {CurRole} ";
    }

    public void PostponeResignation(int postponeDays)
    {
        ResignationTick += (postponeDays * 60000);
        ResidentKnightsManager.Instance.MinResignationDays.MarkDirty();
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

    public void ChangeRole(ResidentKnightRoleDef newRole)
    {
        if (newRole == curRole)
        {
            return;
        }
        ResidentKnightRoleDef oldRole = curRole;
        curRole = newRole;

        oldRole?.RoleWorker.PostDeactiveRole(Pawn);

        if (newRole is null)
        {
            nextRoleChangeableTick = -1;
        }
        else
        {
            nextRoleChangeableTick = Find.TickManager.TicksGame + newRole.positionChangeCDDays * 60000;
            newRole.RoleWorker.PostActiveRole(Pawn);
        }
    }

    public void PostRemoved()
    {
        pawn.SetFaction(RatkinOrder.Faction);
        RatkinOrder.JointPatrolManager.OnResidentKnightRemoved(this);
        if (pawn.Spawned)
        {
            LordMaker.MakeNewLord(RatkinOrder.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Walk, canDefendSelf: true), pawn.Map, startingPawns: [pawn]);
        }
    }

    public override string GetUniqueLoadID() => $"{nameof(ResidentKnightRecord)}_{loadID}";
}