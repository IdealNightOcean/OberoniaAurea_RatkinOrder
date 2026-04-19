using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightRecord : IExposable, ILoadReferenceable
{
    private int loadID = -1;
    public int LoadID => loadID;

    private Pawn pawn;
    private RatkinOrder ratkinOrder;
    private Branch branch;
    private KnightChivalryDef chivalry;
    private bool isCommander;
    private bool isCombatant;

    public Pawn Pawn => pawn;
    public RatkinOrder RatkinOrder => ratkinOrder;
    public Branch Branch => branch;
    public KnightChivalryDef Chivalry => chivalry;
    public bool IsCommander => isCommander;
    public bool IsCombatant => isCombatant;
    public bool IsFriendly => ratkinOrder.Faction?.HostileTo(Faction.OfPlayer) is not true;

    public KnightRecord() { }
    public KnightRecord(RatkinOrder ratkinOrder, Branch branch = null, KnightChivalryDef chivalry = null, bool isCombatant = false, bool isCommander = false)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        if (branch.IsValid() && branch.RatkinOrder != ratkinOrder)
        {
            throw new ArgumentException();
        }
        this.branch = branch;
        this.chivalry = chivalry ?? DefDatabase<KnightChivalryDef>.GetRandom();
        this.isCommander = isCommander;
        this.isCombatant = isCombatant && branch is not null;

        loadID = UniqueIDManager.GetUniqueID(nameof(KnightRecord));
    }

    public void BindPawn(Pawn pawn, bool forceReplace = false)
    {
        if (!forceReplace && this.pawn is not null)
        {
            throw new ArgumentException($"{nameof(this.pawn)} is not null.");
        }

        this.pawn = pawn;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);

        Scribe_References.Look(ref pawn, nameof(pawn));
        Scribe_References.Look(ref ratkinOrder, nameof(ratkinOrder));
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Defs.Look(ref chivalry, nameof(chivalry));

        Scribe_Values.Look(ref isCommander, nameof(isCommander), defaultValue: false);
        Scribe_Values.Look(ref isCombatant, nameof(isCombatant), defaultValue: false);
    }

    public string GetUniqueLoadID() => $"{nameof(KnightRecord)}_{loadID}";
    public override string ToString() => $"{nameof(KnightRecord)}_{loadID}";
}