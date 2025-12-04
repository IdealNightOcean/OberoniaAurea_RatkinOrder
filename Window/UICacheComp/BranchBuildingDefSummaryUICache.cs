using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchBuildingDefSummaryUICache
{
    public BranchBuildingDef BuildingDef { get; }
    public int SilverCost { get; }
    public int TimeCost { get; }

    private List<string> baseEffectDesc;
    private List<string> advancedEffectDesc;

    public List<string> BaseEffectDesc => baseEffectDesc ??= BuildingDef.GetBaseEffectDescriptions().ToList();
    public List<string> AdvancedEffectDesc => advancedEffectDesc ??= BuildingDef.GetAdvancedEffectDescriptions().ToList();

    private string baseEffectDescJoint;
    public string BaseEffectDescJoint => baseEffectDescJoint ??= JointEffectDesc(BaseEffectDesc);

    private string advancedEffectDescJoint;
    public string AvancedEffectDescJoint => advancedEffectDescJoint ??= JointEffectDesc(AdvancedEffectDesc);

    public BranchBuildingDefSummaryUICache() { }
    public BranchBuildingDefSummaryUICache(BranchBuildingDef buildingDef, Branch branch)
    {
        BuildingDef = buildingDef ?? throw new ArgumentNullException(nameof(buildingDef));
        SilverCost = branch.GetBuildingSilverCost(buildingDef);
        TimeCost = branch.GetBuildingTimeCost(buildingDef);
    }
    private string JointEffectDesc(List<string> effectDesc)
    {
        if (effectDesc.NullOrEmpty())
        {
            return string.Empty;
        }
        StringBuilder sb = new();
        for (int i = 0; i < effectDesc.Count; i++)
        {
            sb.AppendLine(effectDesc[i]);
        }
        return sb.ToString();
    }
}