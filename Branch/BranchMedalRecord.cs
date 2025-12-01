using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchMedalRecord : IExposable
{
    public int Count;
    public int FirstGotTick;

    /// <summary>
    /// None类型勋章和非正数勋章无效
    /// </summary>

    public override readonly string ToString() => $"Count: {Count}, FirstGotTick: {FirstGotTick}";

    public void ExposeData()
    {
        Scribe_Values.Look(ref Count, "Count", 0);
        Scribe_Values.Look(ref FirstGotTick, "FirstGotTick", 0);
    }
}