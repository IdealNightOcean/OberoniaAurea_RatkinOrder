using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻人员
/// </summary>
public class ResidentPawn : IExposable, ILoadReferenceable
{
    public const int PendingRemovalGracePeriodTicks = 5 * 60000;

    protected int loadID = -1;
    public int LoadID => loadID;

    protected Pawn pawn;
    public Pawn Pawn => pawn;

    private ResidentPawnState? forceState;
    public ResidentPawnState CurState
    {
        get
        {
            if (pawn.DestroyedOrNull())
            {
                return ResidentPawnState.ForceRemove;
            }
            if (forceState.HasValue)
            {
                return forceState.Value;
            }
            if (removalTick > 0)
            {
                if (Find.TickManager.TicksGame >= removalTick)
                {
                    forceState = ResidentPawnState.ForceRemove;
                    return ResidentPawnState.ForceRemove;
                }
                else
                {
                    return ResidentPawnState.PendingRemoval;
                }
            }
            if (IsDisabled)
            {
                return ResidentPawnState.Disabled;
            }
            return ResidentPawnState.Normal;
        }
    }

    protected AcademicHandler academicHandler;
    public AcademicHandler AcademicHandler => academicHandler;

    private int removalTick = -1;
    public int RemovalTick => removalTick;

    protected virtual bool IsDisabled => pawn.Dead || pawn.RaceProps.IsAnomalyEntity;

    public void SetForceState(ResidentPawnState? state) => forceState = state;

    public void CheckPendingRemoval()
    {
        if (CurState == ResidentPawnState.ForceRemove)
        {
            return;
        }

        bool shouldPendingRemoval = pawn.Dead || pawn.Faction.IsPlayerSafe();
        if (removalTick >= 0 && !shouldPendingRemoval)
        {
            removalTick = -1;
            return;
        }

        if (removalTick < 0 && shouldPendingRemoval)
        {
            removalTick = Find.TickManager.TicksGame + PendingRemovalGracePeriodTicks;
        }
    }

    protected ResidentPawn() { }
    public ResidentPawn(Pawn pawn)
    {
        this.pawn = pawn ?? throw new System.ArgumentNullException(nameof(pawn));

        academicHandler = new AcademicHandler(this);

        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentPawn));
    }

    public ResidentPawn(ResidentKnight residentKnight)
    {
        this.pawn = residentKnight.Pawn ?? throw new System.ArgumentNullException(nameof(residentKnight.Pawn));
        this.academicHandler = residentKnight.AcademicHandler ?? new(this);
        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentPawn));
    }

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);
        Scribe_References.Look(ref pawn, nameof(pawn));

        Scribe_Deep.Look(ref academicHandler, nameof(academicHandler), ctorArgs: this);

        Scribe_Values.Look(ref removalTick, nameof(removalTick), -1);
    }

    public virtual string GetUniqueLoadID() => $"{nameof(ResidentPawn)}_{loadID}";
}
