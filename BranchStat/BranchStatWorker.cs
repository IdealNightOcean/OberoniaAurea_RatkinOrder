using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatWorker(BranchStatDef statDef) : StatWorker<BranchStatDef, Branch, BranchStatRequestData>(statDef)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override float GetNewStatValue(BranchStatRequestData requestData, float? baseValueOverride)
    {
        return requestData.GetNewStatValue(baseValueOverride);
    }

    public override bool PartPostTransModify(BranchStatRequestData requestData,
                                    ref float curValue,
                                    bool resultOnly = true,
                                    StringBuilder explanation = null)
    {
        if (StatDef.statParts is not null)
        {
            List<BranchStatPart> parts = StatDef.statParts;
            bool hasModified = false;
            for (int i = 0; i < parts.Count; i++)
            {
                bool partModified = parts[i].PostTransModify(requestData: requestData, curValue: ref curValue);
                hasModified = hasModified || partModified;
            }

            return hasModified;
        }
        return false;
    }
}