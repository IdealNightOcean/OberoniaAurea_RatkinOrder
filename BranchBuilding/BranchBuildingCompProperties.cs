using System;
using System.Collections.Generic;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingCompProperties
{
    private static readonly Type defaultType = typeof(BranchBuildingComp);

    public Type compClass = defaultType;

    public BranchBuildingCompProperties() { }
    public BranchBuildingCompProperties(Type compClass) => this.compClass = compClass;

    public virtual IEnumerable<string> ConfigErrors(BranchBuildingDef parentDef)
    {
        if (compClass is null)
        {
            yield return parentDef.defName + " has CompProperties with null compClass.";
        }
    }
}