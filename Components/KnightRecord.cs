using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightRecord : IExposable
{
    public RatkinOrder RatkinOrder;
    public Branch Branch;
    public bool IsCommander;

    public void ExposeData()
    {
        Scribe_References.Look(ref RatkinOrder, "RatkinOrder");
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref IsCommander, "IsCommander", defaultValue: false);
    }
}