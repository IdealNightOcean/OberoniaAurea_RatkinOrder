using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder.Utility;

public static class TickUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int YearPassed() => Find.TickManager.TicksGame / 36000000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DayPassed() => Find.TickManager.TicksGame / 60000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHashIntervalTick(int tickHashOffset, int interval)
    {
        return (Find.TickManager.TicksGame + tickHashOffset) % interval == 0;
    }
}