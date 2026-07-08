using OberoniaAurea_Frame;
using RimWorld;
using System.Text;
using Verse;
using static OberoniaAurea.RatkinOrder.Branch;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatPart_BranchTypeFactor : BranchStatPart
{
    public BranchType branchType;
    public float factor;

    public override bool PostTransModify(BranchStatRequestData requestData,
                                     ref float curValue,
                                     bool resultOnly = true,
                                     StringBuilder explanation = null)
    {
        if (!requestData.Target.IsBranchOfType(branchType))
            return false;

        curValue *= factor;

        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeFactor_BranchTypeOf"
                .Translate($"OARO_BranchType_{branchType}".Translate().Named("HonorName"),
                           OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef))
                .ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;
    }
}
