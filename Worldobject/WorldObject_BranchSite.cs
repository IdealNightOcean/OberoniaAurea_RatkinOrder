using RimWorld;
using RimWorld.Planet;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_BranchSite : WorldObject, INameableWorldObject
{
    private string name;
    public string Name
    {
        get
        {
            if (string.IsNullOrEmpty(name))
            {
                name = BranchSiteComp.Branch.Name;
            }
            return name;
        }
        set { name = value; }
    }

    public override string Label => Name;

    private WorldObjectComp_BranchSite branchSiteComp;
    public WorldObjectComp_BranchSite BranchSiteComp => branchSiteComp ??= GetComponent<WorldObjectComp_BranchSite>();

}