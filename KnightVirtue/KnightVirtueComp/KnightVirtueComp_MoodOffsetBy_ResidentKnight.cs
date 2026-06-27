namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueComp_MoodOffsetBy_ResidentKnight : KnightVirtueComp_MoodOffsetByValue
{
    protected override float GetValueForStat() => ResidentPawnsManager.Instance.KnightsCount;
}
