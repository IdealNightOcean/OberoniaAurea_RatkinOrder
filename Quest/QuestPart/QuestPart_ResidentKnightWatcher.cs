using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_ResidentKnightWatcher : QuestPart
{
    public Pawn Knight;
    public string ResignationSignal;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Knight, "Knight");
        Scribe_Values.Look(ref ResignationSignal, "ResignationSignal");
    }




    public override void Cleanup()
    {
        ResidentKnightsManager.Instance.DeregisterKnight(Knight);
        Knight = null;
        ResignationSignal = string.Empty;
    }
}