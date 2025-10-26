using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightRecord : IExposable
{
    [Flags]
    public enum PersonalityType : byte
    {
        None = 0,
        Courage = 1, //勇气
        Tenacity = 2, //坚毅
        Compassion = 4, //怜悯
        Oath = 8, //誓言
        Justice = 16 //正义
    }

    public RatkinOrder RatkinOrder;
    public Branch Branch;
    public bool IsCommander;
    public PersonalityType Personality;

    public void ExposeData()
    {
        Scribe_References.Look(ref RatkinOrder, "RatkinOrder");
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref IsCommander, "IsCommander", defaultValue: false);
        Scribe_Values.Look(ref Personality, "Personality", defaultValue: PersonalityType.None);
    }
}