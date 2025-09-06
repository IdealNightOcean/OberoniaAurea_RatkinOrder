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
    public Squad Squad => branch?.Squad;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref MemberCount, "MemberCount");
        Scribe_Values.Look(ref TravelTicks, "TravelTicks");
        Scribe_Values.Look(ref DaysToExpired, "DaysToExpired");
        Scribe_Values.Look(ref CurBusyLevel, "CurBusyLevel");
    }

    public static bool Validate(AroundKnightGroup aroundKnights)
    {
        return aroundKnights is not null && aroundKnights.branch is not null;
    }
}
