using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointPatrolCaravanHelp_ThingHelpExtension : DefModExtension
{
    [MustTranslate]
    public string requestHelpReason;

    public ThingDef requireThing;
    public int requireCount;
}