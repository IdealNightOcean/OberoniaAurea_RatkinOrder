using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnight : IExposable, IEquatable<ResidentKnight>
{
    private Pawn pawn;
    private RatkinOrder ratkinOrder;
    private ResidentKnightRoleDef roleDef;
    private int lastPositionChangeTick = -1;

    public Pawn Pawn => pawn;
    public RatkinOrder RatkinOrder => ratkinOrder;
    public ResidentKnightRoleDef RoleDef => roleDef;

    public bool IsActive => roleDef is not null;
    public bool CanChangePositionNow
    {
        get
        {
            if (roleDef is null || lastPositionChangeTick < 0)
            {
                return true;
            }
            return Find.TickManager.TicksGame > lastPositionChangeTick + roleDef.positionChangeCDDays * 60000;
        }
    }

    private ResidentKnight() { }
    public ResidentKnight(Pawn pawn, RatkinOrder ratkinOrder)
    {
        this.pawn = pawn;
        this.ratkinOrder = ratkinOrder;
        roleDef = null;
        lastPositionChangeTick = -1;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref roleDef, "roleDef");
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_Values.Look(ref lastPositionChangeTick, "lastPositionChangeTick", -1);
    }

    public void ChangePosition(ResidentKnightRoleDef roleDef)
    {
        if (this.roleDef == roleDef)
        {
            return;
        }

        this.roleDef?.RoleWorker.PostDeactiveRole(pawn);
        this.roleDef = roleDef;
        this.roleDef?.RoleWorker.PostActiveRole(pawn);
        lastPositionChangeTick = Find.TickManager.TicksGame;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not ResidentKnight other)
        {
            return false;
        }

        return roleDef == other.roleDef && pawn == other.pawn;
    }

    public bool Equals(ResidentKnight other)
    {
        if (this == other)
        {
            return true;
        }
        if (other is null)
        {
            return false;
        }

        return roleDef == other.roleDef || pawn == other.pawn;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (roleDef?.GetHashCode() ?? 0);
            hash = hash * 31 + (pawn?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public static bool operator ==(ResidentKnight left, ResidentKnight right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ResidentKnight left, ResidentKnight right)
    {
        return !Equals(left, right);
    }
}