using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchMedalRecord : IExposable
{
    [Flags]
    public enum BranchMedalType : byte
    {
        None = 0,
        Courage = 1, // 勇气
        Tenacity = 2, //坚韧
        Rescue = 4, //援护
        Justice = 8 //公义
    }

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
    public readonly bool Validate() => type != BranchMedalType.None && count > 0;

    public void ExposeData()
    {
        Scribe_Values.Look(ref type, "type", defaultValue: BranchMedalType.None);
        Scribe_Values.Look(ref count, "count", (short)-1);
        Scribe_Values.Look(ref firstGotTick, "firstGotTick", -1);
    }
}