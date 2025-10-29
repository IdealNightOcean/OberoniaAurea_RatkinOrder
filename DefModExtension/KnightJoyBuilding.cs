using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightJoyBuildingPersonality : DefModExtension
{
    public KnightPersonality personality = KnightPersonality.None;
    public override void ResolveReferences(Def parentDef)
    {
        if (personality != KnightPersonality.None && parentDef is ThingDef thingDef && thingDef.building is not null)
        {
            OrderDefDataBase.AddKnightJoyBuilding(thingDef, personality);
        }
    }
}