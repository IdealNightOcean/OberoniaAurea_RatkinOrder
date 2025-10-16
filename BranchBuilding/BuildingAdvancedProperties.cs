using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BuildingAdvancedProperties
{
    [MustTranslate]
    public string label;

    public int advancedPopulation;

    public List<string> effectFlags; //效果标志列表
    public List<BranchStatModifier> branchStatModifies; //属性修正列表
}
