using OberoniaAurea_Frame;
using RimWorld;

namespace OberoniaAurea.RatkinOrder;

public class MercyQuestParentFactionFinder_RatkinOrder : MercyQuestParentFactionFinder
{
    public override Faction FindParentFaction(MercyQuestDef mercyDef, FactionValidationParams? factionParams = null, FactionDef fixedParentFactionDef = null)
    {
        factionParams ??= FactionValidationParams.NonHostileNormalFaction;
        return OAFrame_FactionUtility.RandomAvailableFactionOf(
            validationParams: factionParams.Value,
            predicater: delegate (Faction f)
            {
                if (fixedParentFactionDef is not null && f.def != fixedParentFactionDef)
                {
                    return false;
                }
                return RatkinOrderManager.Instance.FactionHasRatkinOrder(f);
            });
    }
}