using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestParentFactionFinder_Ratkin : MercyQuestParentFactionFinder
{
    public override Faction FindParentFaction(MercyQuestDef mercyDef, FactionValidationParams? factionParams = null, FactionDef fixedParentFactionDef = null)
    {
        factionParams ??= FactionValidationParams.NonHostileNormalFaction;
        if (fixedParentFactionDef is not null && fixedParentFactionDef.IsRatkinFaction())
        {
            return OberoniaAurea_Frame.Utility.OAFrame_FactionUtility.RandomAvailableFactionOfDef(
                def: fixedParentFactionDef,
                validationParams: factionParams.Value);
        }
        else
        {
            return OberoniaAurea_Frame.Utility.OAFrame_FactionUtility.RandomAvailableFactionOf(
                validationParams: factionParams.Value,
                predicater: f => f.IsRatkinFaction());
        }
    }
}