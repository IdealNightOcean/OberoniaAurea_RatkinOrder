using Verse;

namespace OberoniaAurea.RatkinOrder;

public class AroundKnightGroup : IExposable
{
    public enum BusyLevel : byte
    {
        Leisure,
        Busy,
        VeryBusy
    }

    private Branch branch;

    public int MemberCount;
    public int TravelTicks;
    public int DaysToExpired;
    public BusyLevel CurBusyLevel;

    public Branch Branch => branch;
    public BranchSquad Squad => branch?.Squad;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;

    private AroundKnightGroup() { }
    public AroundKnightGroup(Branch branch)
    {
        this.branch = branch;
        MemberCount = Rand.RangeInclusive(2, 5);
        TravelTicks = GenMath.RoundTo(Rand.RangeInclusive(15000, 2 * 60000), 2500);
        DaysToExpired = Rand.RangeInclusive(3, 7);
        CurBusyLevel = Gen.RandomEnumValue<BusyLevel>(disallowFirstValue: false);
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref MemberCount, "MemberCount", 0);
        Scribe_Values.Look(ref TravelTicks, "TravelTicks", 0);
        Scribe_Values.Look(ref DaysToExpired, "DaysToExpired", 0);
        Scribe_Values.Look(ref CurBusyLevel, "CurBusyLevel", BusyLevel.Busy);
    }

    public override string ToString()
    {
        return $"Branch: {branch.Name} - MemberCount: {MemberCount}\nTravelTicks: {TravelTicks} - DaysToExpired: {DaysToExpired} - CurBusyLevel: {CurBusyLevel}";
    }

    public static bool Validate(AroundKnightGroup aroundKnights)
    {
        return aroundKnights is not null && aroundKnights.branch is not null;
    }
}
