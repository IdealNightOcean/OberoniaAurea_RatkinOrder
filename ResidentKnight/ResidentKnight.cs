using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
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
    protected override bool IsDisabled => base.IsDisabled || !Branch.IsValid();

    private KnightRecord knightRecord;
    public KnightRecord KnightRecord => knightRecord;
    public RatkinOrder RatkinOrder => knightRecord.RatkinOrder;
    public Branch Branch => knightRecord.Branch;
    public KnightChivalryDef Chivalry => knightRecord.Chivalry;

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
    public KnightVirtueHandler VirtueHandler => knightVirtueHandler;

    private TagStrToInt effectTags = new(defaultValue: 0, removeWhenDefault: true);
    public TagStrToInt EffectTags => effectTags;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref knightRecord, nameof(knightRecord));

        Scribe_Values.Look(ref curRank, nameof(curRank), defaultValue: ResidentKnightRank.Regular);
        Scribe_Values.Look(ref meditationPoints, nameof(meditationPoints), defaultValue: 0f);

        Scribe_Defs.Look(ref curRole, nameof(curRole));
        Scribe_Values.Look(ref nextRoleChangeableTick, nameof(nextRoleChangeableTick), defaultValue: -1);

        Scribe_Values.Look(ref residenceStartTick, nameof(residenceStartTick), defaultValue: -1);
        Scribe_Values.Look(ref resignationTick, nameof(resignationTick), defaultValue: -1);

        Scribe_Deep.Look(ref academicHandler, nameof(academicHandler));
        Scribe_Deep.Look(ref knightVirtueHandler, nameof(knightVirtueHandler), ctorArgs: this);
    }

    private ResidentKnight() : base() { }
    public ResidentKnight(KnightRecord knightRecord)
    {
        this.knightRecord = knightRecord ?? throw new ArgumentNullException(nameof(knightRecord));
        this.pawn = knightRecord.Pawn ?? throw new ArgumentNullException(nameof(knightRecord.Pawn));

        academicHandler = new(this);
        knightVirtueHandler = new(this);

        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentKnight));
    }

    public void PostponeResignation(int postponeDays)
    {
        ResignationTick += (postponeDays * 60000);

        ResidentPawnsManager.CacheManager?.KnightsApproachingResignation?.MarkDirty();
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

    public void PostAdded()
    {
        residenceStartTick = Find.TickManager.TicksGame;
        if (RatkinOrder.ReformationManager.HasReformation(OrderReformationDefOf.OARO_ReformationPlaceholder))
        {
            ResignationTick = Find.TickManager.TicksGame + 4 * 60 * 60000;
        }
        else
        {
            ResignationTick = Find.TickManager.TicksGame + 2 * 60 * 60000;
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

    public void Notify_StimulatedBy(KnightRecord initiatorKnight)
    {
        if (Rand.Chance(0.1f))
        {
            KnightChivalryDef initiatorChivalry = initiatorKnight.Chivalry;
            KnightVirtueDef targetVirtue = KnightVirtueUtility.GetRandomUpgradableVirtue(this, v => initiatorChivalry.IsSameDefNonNullable(v.chivalry));
            if (targetVirtue is not null)
            {
                string reason = "OARO_VirtueUpgradeReason_KnightlyTalk".Translate(initiatorKnight.Pawn.Named(KeyLibrary_FormatArgName.PAWN));
                VirtueHandler.UpgradeVirtue(targetVirtue, upgrade: 1, reason: reason);
            }
        }

        VirtueHandler.Notify_StimulatedBy(initiatorKnight);
    }

    public void Notify_Stimulate(Pawn recipient)
    {
        VirtueHandler.Notify_Stimulate(recipient);
    }

    public override string ToString()
    {
        return $"Branch: {Branch.Name}, Rank: {CurRank}, MeditationPoints: {MeditationPoints}, Role: {CurRole} ";
    }
    public override string GetUniqueLoadID() => $"{nameof(ResidentKnight)}_{loadID}";
}