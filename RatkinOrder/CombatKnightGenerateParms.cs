using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct CombatKnightGenerateParms
{
    public RatkinOrder RatkinOrder { get; private set; }
    public Branch Branch { get; private set; }
    public Map Map { get; private set; }
    public readonly bool IsValid => RatkinOrder is not null && Map is not null;
    public readonly Faction Faction => RatkinOrder?.Faction;
    public readonly bool IsFriendly => Faction?.HostileTo(Faction.OfPlayer) is not true;

    public PawnGroupKindDef PawnGroupKind { get; set; }
    public RaidStrategyDef RaidStrategy { get; set; }
    public PawnsArrivalModeDef RaidArrivalMode { get; set; }

    public int MemberCount { get; set; }
    public int CommanderCount { get; set; }
    public int NonKnightCount { get; set; }
    public float SupplyCost { get; set; }

    public CombatKnightGenerateParms()
    {
        PawnGroupKind = PawnGroupKindDefOf.Combat;
    }

    public CombatKnightGenerateParms(RatkinOrder ratkinOrder, Map map) : this()
    {
        RatkinOrder = ratkinOrder;
        Map = map;
    }

    public CombatKnightGenerateParms(Branch branch, Map map) : this()
    {
        Branch = branch;
        Map = map;
        RatkinOrder = branch.RatkinOrder;
    }
}