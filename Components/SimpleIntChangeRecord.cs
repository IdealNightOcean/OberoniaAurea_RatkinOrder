using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public struct SimpleIntChangeRecord : IExposable
{
    public int change;
    public string explain;

    public SimpleIntChangeRecord() { }
    public SimpleIntChangeRecord(int change, string explain)
    {
        this.change = change;
        this.explain = explain;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref change, nameof(change), 0);
        Scribe_Values.Look(ref explain, nameof(explain), KeyLibrary_Misc.ErrorTip);
    }
}