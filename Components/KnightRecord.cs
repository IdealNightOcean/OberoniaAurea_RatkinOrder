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

    private static readonly PersonalityType[] personalityTypesArr = (PersonalityType[])Enum.GetValues(typeof(PersonalityType));
    public static PersonalityType GetRandomAvailablePersonality() => personalityTypesArr[Rand.Range(1, personalityTypesArr.Length)];

    private RatkinOrder ratkinOrder;
    private Branch branch;
    private bool isCommander;
    private PersonalityType personality = PersonalityType.None;

    public RatkinOrder RatkinOrder => ratkinOrder;
    public Branch Branch => branch;
    public bool IsCommander => isCommander;
    public PersonalityType Personality => personality;

    public KnightRecord() { }
    public KnightRecord(Branch branch, PersonalityType personality = PersonalityType.None, bool isCommander = true) : this(branch.RatkinOrder, branch, personality, isCommander) { }
    public KnightRecord(RatkinOrder ratkinOrder, Branch branch = null, PersonalityType personality = PersonalityType.None, bool isCommander = true)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        if (branch is not null && branch.RatkinOrder != ratkinOrder)
        {
            throw new ArgumentException();
        }
        this.branch = branch;
        this.personality = personality == PersonalityType.None ? GetRandomAvailablePersonality() : personality;
        this.isCommander = isCommander;
    }


    public void ExposeData()
    {
        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref isCommander, "isCommander", defaultValue: false);
        Scribe_Values.Look(ref personality, "personality", defaultValue: PersonalityType.None);
    }
}