using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻骑士
/// </summary>
public class ResidentKnight : ResidentPawn
{
    public override bool IsValid => base.IsValid && Branch.IsValid();
    public bool ShouldTransToColonist => base.IsValid && !Branch.IsValid();

    private KnightRecord knightRecord;
    public KnightRecord KnightRecord => knightRecord;
    public RatkinOrder RatkinOrder => knightRecord.RatkinOrder;
    public Branch Branch => knightRecord.Branch;
    public KnightPersonality Personality => knightRecord.Personality;


    private ResidentKnightRank curRank;
    public ResidentKnightRank CurRank
    {
        get => curRank;
        set => curRank = value;
    }

    private float meditationPoints;
    public float MeditationPoints
    {
        get => meditationPoints;
        set => meditationPoints = Mathf.Max(0f, value);
    }


    private int residenceStartTick = -1;
    private int resignationTick = -1;
    public int ResignationTick
    {
        get => resignationTick;
        set => resignationTick = value;
    }


    private ResidentKnightRoleDef curRole;
    private int nextRoleChangeableTick = -1;
    public ResidentKnightRoleDef CurRole => curRole;
    public int NextRoleChangeableTick => nextRoleChangeableTick;


    private KnightVirtueHandler knightVirtueHandler;
    public KnightVirtueHandler KnightVirtueHandler => knightVirtueHandler;


    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref knightRecord, nameof(knightRecord));

        Scribe_Values.Look(ref curRank, nameof(curRank), ResidentKnightRank.Regular);
        Scribe_Values.Look(ref meditationPoints, nameof(meditationPoints), 0f);

        Scribe_Defs.Look(ref curRole, nameof(curRole));
        Scribe_Values.Look(ref nextRoleChangeableTick, nameof(nextRoleChangeableTick), -1);

        Scribe_Values.Look(ref residenceStartTick, nameof(residenceStartTick), -1);
        Scribe_Values.Look(ref resignationTick, nameof(resignationTick), -1);

        Scribe_Deep.Look(ref knightVirtueHandler, nameof(knightVirtueHandler));
    }

    private ResidentKnight() : base() { }
    public ResidentKnight(Pawn knight, KnightRecord knightRecord)
    {
        this.pawn = knight ?? throw new ArgumentNullException(nameof(knight));
        this.knightRecord = knightRecord ?? throw new ArgumentNullException(nameof(knightRecord));

        academicHandler = new AcademicHandler(knight, knightRecord);
        knightVirtueHandler = new();

        residenceStartTick = Find.TickManager.TicksGame;
        if (RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            ResignationTick = Find.TickManager.TicksGame + 4 * 60 * 60000;
        }
        else
        {
            ResignationTick = Find.TickManager.TicksGame + 2 * 60 * 60000;
        }


        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentKnight));
    }

    public void PostponeResignation(int postponeDays)
    {
        ResignationTick += (postponeDays * 60000);
        ResidentKnightsManager.Instance.MinResignationDays.MarkDirty();
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

    public void PostRemoved(ResidentKnightRemovalReason reason)
    {
        RatkinOrder?.JointPatrolManager.OnResidentKnightRemoved(this);

        Faction originalFaction = RatkinOrder.Faction;
        if (originalFaction is not null && (pawn.Faction is null || pawn.Faction.IsPlayerSafe()))
        {
            pawn.SetFaction(originalFaction);
            if (pawn.Spawned)
            {
                LordMaker.MakeNewLord(
                    faction: RatkinOrder.Faction,
                    lordJob: new LordJob_ExitMapBest(LocomotionUrgency.Walk, canDefendSelf: true),
                    map: pawn.Map,
                    startingPawns: [pawn]);
            }
        }
    }

    public override string ToString()
    {
        return $"Branch: {Branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, Role: {CurRole} ";
    }

    public override string GetUniqueLoadID() => $"{nameof(ResidentKnight)}_{loadID}";
}