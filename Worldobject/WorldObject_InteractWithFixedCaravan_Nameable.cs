using OberoniaAurea_Frame;
using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class WorldObject_InteractWithFixedCaravan_Nameable : WorldObject_InteractWithFixedCaravanBase, INameableWorldObject
{
    public override bool HasName => name is not null;

    private string name;
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

    protected void SendWorkResolvedSignal(NamedArgument[] args = null)
    {
        if (args is null)
        {
            QuestUtility.SendQuestTargetSignals(questTags, "WorkResolved", this.Named("SUBJECT"));
        }
        else
        {
            NamedArgument[] extendedArgs = new NamedArgument[args.Length + 1];
            extendedArgs[0] = this.Named("SUBJECT");
            Array.Copy(args, 0, extendedArgs, 1, args.Length);
            QuestUtility.SendQuestTargetSignals(questTags, "WorkResolved", extendedArgs);
        }
    }
}
