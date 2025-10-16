using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

// xml相关
public class BranchFacilityLevelStage
{
    public int silverCost;
    public int constructionDays;

    public List<string> effectFlags;
    public List<BranchStatModifier> statModifies;
}