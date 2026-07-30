using OberoniaAurea.RatkinOrder.DataLibrary;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_StatModifiesBy_MercyQuestSucceed : KnightVirtueComp_StatModifiesByValue
{
    protected override float GetValueForStat()
    {
        GlobalInteractionManager.InteractionRecord.TryGetTagValue(KeyLibrary_InteractRecord.MercyQuestSucceed, out float value);
        return value;
    }
}
