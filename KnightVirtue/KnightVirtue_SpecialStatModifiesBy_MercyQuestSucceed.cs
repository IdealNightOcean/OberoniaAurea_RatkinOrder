namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_SpecialStatModifiesBy_MercyQuestSucceed : KnightVirtue_SpecialStatModifiesByValue
{
    protected override float ValueForStat
    {
        get
        {
            GlobalInteractionManager.InteractionRecord.TryGetTagValue(KeyLibrary_InteractRecord.MercyQuestSucceed, out float value);
            return value;
        }
    }
}
