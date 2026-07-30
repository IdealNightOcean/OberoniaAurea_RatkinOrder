using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld.Planet;
using System;
using System.Linq;
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

    public string Source;
    public string Destination;

    public Branch Branch => branch;
    public RatkinOrder RatkinOrder => branch?.RatkinOrder;

    private AroundKnightGroup() { }
    public AroundKnightGroup(Branch branch)
    {
        this.branch = branch;
        MemberCount = Rand.RangeInclusive(2, 5);
        TravelTicks = GenMath.RoundTo(Rand.RangeInclusive(15000, 2 * 60000), 2500);
        DaysToExpired = Rand.RangeInclusive(3, 7);
        CurBusyLevel = Gen.RandomEnumValue<BusyLevel>(disallowFirstValue: false);
        InitRoute();
    }

    private void InitRoute()
    {
        try
        {
            Settlement[] route = Find.WorldObjects.Settlements.TakeRandom(2).ToArray();
            Source = route[0].Name;
            Destination = route[1].Name;
        }
        catch (Exception ex)
        {
            Source = KeyLibrary_Misc.ErrorTip;
            Destination = KeyLibrary_Misc.ErrorTip; ;
            ModUtility.LogExceptionError(ex,
                errorDesc: $"initiate {nameof(AroundKnightGroup)}'s {Source} and {Destination}.",
                typeName: nameof(AroundKnightGroup),
                methodName: nameof(InitRoute),
                needStackTrace: true);
        }
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref branch, nameof(branch));
        Scribe_Values.Look(ref MemberCount, nameof(MemberCount), 0);
        Scribe_Values.Look(ref TravelTicks, nameof(TravelTicks), 0);
        Scribe_Values.Look(ref DaysToExpired, nameof(DaysToExpired), 0);
        Scribe_Values.Look(ref CurBusyLevel, nameof(CurBusyLevel), BusyLevel.Busy);
    }

    public override string ToString()
    {
        return $"Branch: {branch.Name} - MemberCount: {MemberCount}\nTravelTicks: {TravelTicks} - DaysToExpired: {DaysToExpired} - CurBusyLevel: {CurBusyLevel}";
    }

    public static bool Validate(AroundKnightGroup aroundKnights)
    {
        return aroundKnights is not null && aroundKnights.branch.IsValid();
    }
}
