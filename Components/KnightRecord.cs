using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[Flags]
public enum KnightPersonality : byte
{
    None = 0,
    Courage = 1, //勇气
    Tenacity = 2, //坚毅
    Compassion = 4, //怜悯
    Oath = 8, //誓言
    Justice = 16 //正义
}


public class KnightRecord : IExposable
{
    private RatkinOrder ratkinOrder;
    private Branch branch;
    private bool isCommander;
    private KnightPersonality personality = KnightPersonality.None;

    public RatkinOrder RatkinOrder => ratkinOrder;
    public Branch Branch => branch;
    public bool IsCommander => isCommander;
    public KnightPersonality Personality => personality;

    public KnightRecord() { }
    public KnightRecord(Branch branch, KnightPersonality personality = KnightPersonality.None, bool isCommander = true) : this(branch.RatkinOrder, branch, personality, isCommander) { }
    public KnightRecord(RatkinOrder ratkinOrder, Branch branch = null, KnightPersonality personality = KnightPersonality.None, bool isCommander = true)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        if (branch is not null && branch.RatkinOrder != ratkinOrder)
        {
            throw new ArgumentException();
        }
        this.branch = branch;
        this.personality = personality == KnightPersonality.None ? KnightPersonalityUtility.GetRandomAvailablePersonality() : personality;
        this.isCommander = isCommander;
    }


    public void ExposeData()
    {
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref isCommander, "isCommander", defaultValue: false);
        Scribe_Values.Look(ref personality, "personality", defaultValue: KnightPersonality.None);
    }
}