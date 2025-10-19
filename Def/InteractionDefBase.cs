using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class InteractionDefBase : Def
{
    public string cdRecordKey;
    public int cdDays = -1;

    public EsteemHandler.RelationshipKind floorRelationship = EsteemHandler.RelationshipKind.Stranger;
    public int floorEsteem = -1;

    public int needRecommendation = -1;
    public int needSilver = -1;

}