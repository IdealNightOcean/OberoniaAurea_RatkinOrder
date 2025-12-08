using RimWorld;
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


public class KnightRecord : IExposable, ILoadReferenceable
{
    private int loadID = -1;
    public int LoadID => loadID;

    private RatkinOrder ratkinOrder;
    private Branch branch;
    private KnightPersonality personality = KnightPersonality.None;
    private bool isCommander;
    private bool isCombatant;

    public RatkinOrder RatkinOrder => ratkinOrder;
    public Branch Branch => branch;
    public KnightPersonality Personality => personality;
    public bool IsCommander => isCommander;
    public bool IsCombatant => isCombatant;
    public bool IsFriendly => ratkinOrder.Faction?.HostileTo(Faction.OfPlayer) is not true;

    public KnightRecord() { }
    public KnightRecord(Branch branch, KnightPersonality personality = KnightPersonality.None, bool isCombatant = false, bool isCommander = false) : this(branch.RatkinOrder, branch, personality, isCombatant, isCommander) { }
    public KnightRecord(RatkinOrder ratkinOrder, Branch branch = null, KnightPersonality personality = KnightPersonality.None, bool isCombatant = false, bool isCommander = false)
    {
        this.ratkinOrder = ratkinOrder ?? throw new ArgumentNullException(nameof(ratkinOrder));
        if (branch.IsValid() && branch.RatkinOrder != ratkinOrder)
        {
            throw new ArgumentException();
        }
        this.branch = branch;
        this.personality = personality == KnightPersonality.None ? KnightPersonalityUtility.GetRandomAvailablePersonality() : personality;
        this.isCommander = isCommander;
        this.isCombatant = isCombatant && branch is not null;

        loadID = UniqueIDManager.GetUniqueID(nameof(KnightRecord));
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, "loadID", -1);

        Scribe_References.Look(ref ratkinOrder, "ratkinOrder");
        Scribe_References.Look(ref branch, "branch");
        Scribe_Values.Look(ref personality, "personality", defaultValue: KnightPersonality.None);

        Scribe_Values.Look(ref isCommander, "isCommander", defaultValue: false);
        Scribe_Values.Look(ref isCombatant, "isCombatant", defaultValue: false);
    }

    public string GetUniqueLoadID() => $"{nameof(KnightRecord)}_{loadID}";
    public override string ToString() => $"{nameof(KnightRecord)}_{loadID}";
}