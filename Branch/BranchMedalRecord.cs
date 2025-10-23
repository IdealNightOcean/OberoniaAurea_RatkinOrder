using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct BranchMedalRecord : IExposable
{
    /// <summary>
    /// 勋章类型
    /// </summary>
    [Flags]
    public enum BranchMedalType : byte
    {
        None = 0,
        Courage = 1, // 勇气
        Tenacity = 2, //坚韧
        Rescue = 4, //援护
        Justice = 8 //公义
    }

    /// <summary>
    /// 应该只包含单一枚举，不可用于组合枚举
    /// </summary>
    public BranchMedalType Type;
    public short Count;
    public int FirstGotTick;

    public BranchMedalRecord()
    {
        Type = BranchMedalType.None;
        Count = 1;
        FirstGotTick = -1;
    }

    /// <summary>
    /// None类型勋章和非正数勋章无效
    /// </summary>
    public readonly bool Validate() => Type != BranchMedalType.None && Count > 0;

    public void ExposeData()
    {
        Scribe_Values.Look(ref Type, "type", defaultValue: BranchMedalType.None);
        Scribe_Values.Look(ref Count, "Count", (short)-1);
        Scribe_Values.Look(ref FirstGotTick, "FirstGotTick", -1);
    }
}