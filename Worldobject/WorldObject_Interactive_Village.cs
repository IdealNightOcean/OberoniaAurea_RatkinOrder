using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_Interactive_Village : WorldObject_InteractiveBase, INameableWorldObject
{
    public override bool HasName => Faction is not null;

    private string name;
    public string Name
    {
        get => name ??= (Faction?.Name ?? def.label);
        set
        {
            if (Faction is not null)
            {
                Faction.Name = value;
            }
        }
    }

    public override string Label => Name;
    public override string LabelShort => Name;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref name, "name", null);
    }
}