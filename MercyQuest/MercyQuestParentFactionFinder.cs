using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public abstract class MercyQuestParentFactionFinder
{
    public abstract Faction FindParentFaction(MercyQuestDef mercyDef, FactionValidationParams? factionParams = null, FactionDef fixedParentFactionDef = null);
}