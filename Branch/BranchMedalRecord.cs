using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchMedalRecord : IExposable
{
    public short Count;
    public int FirstGotTick;

    /// <summary>
    /// None类型勋章和非正数勋章无效
    /// </summary>
    public readonly bool Validate() => Count > 0;

    public override string ToString() => $"Count: {Count}, FirstGotTick: {FirstGotTick}";

    public void ExposeData()
    {
        Scribe_Values.Look(ref Count, "Count", (short)-1);
        Scribe_Values.Look(ref FirstGotTick, "FirstGotTick", -1);
    }
}