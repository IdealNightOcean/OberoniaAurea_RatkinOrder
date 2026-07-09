using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class BranchStatWorker_ConstructionCost<T>(BranchStatDef statDef) : BranchStatWorker(statDef) where T : BranchConstructionDef, new()
{
    public override bool PostTransModify(BranchStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (!TryCastRequestData<BranchStatRequestData_BranchConstruction<T>>(requestData, out BranchStatRequestData_BranchConstruction<T> constructionData))
        {
            curValue.IsConverged = true;
            return false;
        }

        Branch branch = constructionData.Target;
        T constructionDef = constructionData.ConstructionDef;

        float factor = branch.GetStatValue(BranchStatDefOf.OARO_ConstructionCostFactor);

        if (factor != 1f)
        {
            curValue.Value *= factor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BuildSilverCost_CostFactor"
                    .Translate(OARO_StatExplanationUtility.FactorNamedArgument(factor, constructionData.StatDef))
                    .ColorizeStrByFactor(factor, reverse: constructionData.StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        factor = 1f + branch.StoresReserveHandler.GetReserveCostReduce(constructionDef);
        if (factor != 1f)
        {
            curValue.Value *= factor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BuildSilverCost_ReserveReduction"
                    .Translate(OARO_StatExplanationUtility.FactorNamedArgument(factor, StatDef))
                    .ColorizeStrByFactor(factor, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return true;
    }
}