using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 分部印记记录
/// </summary>
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
        Scribe_Values.Look(ref Count, nameof(Count), 0);
        Scribe_Values.Look(ref FirstGotTick, nameof(FirstGotTick), 0);
    }
}