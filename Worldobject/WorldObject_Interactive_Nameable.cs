using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class WorldObject_Interactive_Nameable : WorldObject_InteractiveBase, INameableWorldObject
{
    public override bool HasName => name is not null;

    protected string name;
    public string Name
    {
        get => name ?? def.label;
        set
        {
            name = value;
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