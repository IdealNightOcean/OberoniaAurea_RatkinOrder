using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentPawn : IExposable, ILoadReferenceable
{
    public const int PendingRemovalGracePeriodTicks = 5 * 60000;

    protected int loadID = -1;
    public int LoadID => loadID;

    protected Pawn pawn;
    public Pawn Pawn => pawn;

    protected AcademicHandler academicHandler;
    public AcademicHandler AcademicHandler => academicHandler;

    private int pendingRemovalTick = -1;
    public int PendingRemovalTick => pendingRemovalTick;

    public virtual bool IsValid => !pawn.DestroyedOrNull() && !pawn.Dead;

    public bool ShouldRemove
    {
        get
        {
            if (pawn.DestroyedOrNull())
            {
                return true;
            }

            if (pendingRemovalTick > 0)
            {
                return Find.TickManager.TicksGame >= pendingRemovalTick;
            }

            return false;
        }
    }

    public void CheckPendingRemoval()
    {
        bool shouldPendingRemoval = pawn.Dead || pawn.Faction.IsPlayerSafe();
        if (pendingRemovalTick >= 0 && !shouldPendingRemoval)
        {
            pendingRemovalTick = -1;
            return;
        }

        if (pendingRemovalTick < 0 && shouldPendingRemoval)
        {
            pendingRemovalTick = Find.TickManager.TicksGame + PendingRemovalGracePeriodTicks;
        }
    }

    protected ResidentPawn() { }
    public ResidentPawn(Pawn pawn)
    {
        this.pawn = pawn;

        academicHandler = new AcademicHandler();

        loadID = UniqueIDManager.GetUniqueID(nameof(ResidentPawn));
    }

    public virtual void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);
        Scribe_References.Look(ref pawn, nameof(pawn));

        Scribe_Deep.Look(ref academicHandler, nameof(academicHandler));

        Scribe_Values.Look(ref pendingRemovalTick, nameof(pendingRemovalTick), -1);
    }

    public virtual string GetUniqueLoadID() => $"{nameof(ResidentPawn)}_{loadID}";
}
