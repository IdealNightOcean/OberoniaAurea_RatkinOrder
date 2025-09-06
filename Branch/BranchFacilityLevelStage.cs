using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

// xml相关
public class BranchFacilityLevelStage
{
    public BranchFacilityLevel level;
    public List<string> effectFlags;
    public List<BranchStatModifier> statModifies;

    public virtual void PostActive(Branch branch) { }

    public virtual void PostDeactive(Branch branch) { }

    public virtual void PostLoadInit(Branch branch) { }

}