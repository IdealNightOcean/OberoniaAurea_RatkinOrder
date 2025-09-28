using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchMedalRecord : IExposable
{
    public BranchMedalType type;
    public short count;
    public int firstGotTick;

    public BranchMedalRecord()
    {
        type = BranchMedalType.None;
        count = 1;
        firstGotTick = -1;
    }

    /// <summary>
    /// None类型勋章和非正数勋章无效
    /// </summary>
    public static bool Validate(BranchMedalRecord record)
    {
        return record.type != BranchMedalType.None && record.count > 0;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref type, "type", defaultValue: BranchMedalType.None);
        Scribe_Values.Look(ref count, "count", (short)-1);
        Scribe_Values.Look(ref firstGotTick, "firstGotTick", -1);
    }
}