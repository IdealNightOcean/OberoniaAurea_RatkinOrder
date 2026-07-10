using OberoniaAurea_Frame;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_VirtueTrainSuccessChance(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override bool PostTransModify(ResidentKnightStatRequestData requestData, ref StatComputeState curValue, bool resultOnly = true, StringBuilder explanation = null)
    {
        if (!TryCastRequestData<ResidentKnightStatRequestData_ResidentKnightStudy>(requestData, out ResidentKnightStatRequestData_ResidentKnightStudy studyData))
        {
            curValue.IsConverged = true;
            return false;
        }

        Branch branch = studyData.Target.Branch;

        KnightChivalryDef virtueChivalry = studyData.OtherChivalry;
        float curStepChange;
        if (virtueChivalry.IsSameDefNonNullable(branch.HonorDef?.chivalry))
        {
            curStepChange = 0.2f;
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BranchResident_VirtueTrain_SameChivalryWithHonor"
                    .Translate(OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef))
                    .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
        {
            if (!virtueChivalry.IsSameDefNonNullable(tradition.Def.chivalry))
                continue;

            curStepChange = (tradition.Level * 0.05f);
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BranchResident_VirtueTrain_SameChivalryWithHonor"
                    .Translate(tradition.Def.Named(OARO_KeyLibrary_FormatArgName.TRADITIONDEF),
                               tradition.Level.Named(KeyLibrary_FormatArgName.Level),
                               OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef))
                    .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        if (branch.MedalHandler.MedalRecords.TryGetValue(virtueChivalry, out BranchMedalRecord medalRecord))
        {
            curStepChange = (medalRecord.Count * 0.02f);
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BranchResident_VirtueTrain_SameChivalryWithMedal"
                    .Translate(virtueChivalry.Named(KeyLibrary_FormatArgName.DEF),
                               medalRecord.Count.Named(KeyLibrary_FormatArgName.Count),
                               OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef))
                    .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        if (virtueChivalry.IsSameDefNonNullable(studyData.Target.Chivalry))
        {
            curStepChange = 0.3f;
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                  text: "OARO_BranchResident_VirtueTrain_SameChivalryWithKnight"
                  .Translate(OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef))
                  .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                  separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        if (virtueChivalry.IsSameDefNonNullable(OARO_ModDefOf.OARO_Oath))
        {
            int totalMedalsCost = studyData.MedalsCost.Values.Sum();
            curStepChange = totalMedalsCost * 0.01f;
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                  text: "OARO_BranchResident_VirtueTrain_Oath_MedalsCost"
                  .Translate(totalMedalsCost.Named(KeyLibrary_FormatArgName.Count),
                             OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef))
                  .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                  separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return true;
    }
}