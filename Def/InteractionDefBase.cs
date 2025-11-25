using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class InteractionDefBase : Def
{
    public bool hasCoolDown;
    public bool useDefaultCD;
    public int defaultCdDays = -1;

    public EsteemHandler.RelationshipKind floorRelationship = EsteemHandler.RelationshipKind.Stranger;
    public int floorEsteem = -1;

    public int needRecommendation = -1;
    public int needSilver = -1;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (!hasCoolDown && useDefaultCD)
        {
            useDefaultCD = false;
            yield return $"'{nameof(useDefaultCD)}' disabled because '{nameof(hasCoolDown)}' is false.";
        }
        if (useDefaultCD && defaultCdDays < 0)
        {
            defaultCdDays = 0;
            yield return $"'{nameof(defaultCdDays)}' was negative. Set to '0'.";
        }
    }
}