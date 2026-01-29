using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentColonistRecord : IExposable, ILoadReferenceable
{

    private int loadID = -1;
    public int LoadID => loadID;

    private Pawn colonist;
    public Pawn Colonist => colonist;

    private HashSet<ResidentKnightRecord> teachers;


    public void ExposeData()
    {
        Scribe_Values.Look(ref loadID, nameof(loadID), -1);
        Scribe_References.Look(ref colonist, nameof(colonist));
        Scribe_Collections.Look(ref teachers, nameof(teachers), LookMode.Reference);

    }

    public string GetUniqueLoadID() => $"{nameof(ResidentColonistRecord)}_{loadID}";

}