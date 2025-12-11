using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightPreferredBuildingExtension : DefModExtension
{
    public KnightPersonality personality = KnightPersonality.None;
    public override void ResolveReferences(Def parentDef)
    {
        if (personality != KnightPersonality.None && parentDef is ThingDef thingDef && thingDef.building is not null)
        {
            OrderDefDataBase.AddKnightPreferBuilding(thingDef, personality);
        }
    }
}