using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_MeditationStudyPointGain(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override bool PrepareInitialBaseValue(ResidentKnightStatRequestData requestData,
                                                 ref StatComputeState curValue, float? baseValueOverride = null,
                                                 bool resultOnly = true, StringBuilder explanation = null)
    {
        if (!TryCastRequestData<ResidentKnightStatRequestData_ResidentKnightStudy>(requestData, out ResidentKnightStatRequestData_ResidentKnightStudy studyData))
        {
            curValue.IsConverged = true;
            return false;
        }

        curValue.Value = studyData.MedalsCost.Values.Sum() * 200f;
        float baseValue = baseValueOverride ?? StatDef.baseValue;
        curValue.Value = baseValue;
        if (!resultOnly)
        {
            explanation.AppendLine(StatDef.GetBaseValueExplanation(baseValue));
        }
        return true;
    }

    public override bool PostTransModify(ResidentKnightStatRequestData requestData, ref StatComputeState curValue, bool resultOnly = true, StringBuilder explanation = null)
    {
        if (!TryCastRequestData<ResidentKnightStatRequestData_ResidentKnightStudy>(requestData, out ResidentKnightStatRequestData_ResidentKnightStudy studyData))
        {
            curValue.IsConverged = true;
            return false;
        }

        IReadOnlyDictionary<KnightChivalryDef, int> medalsCost = studyData.MedalsCost;
        Branch branch = studyData.Target.Branch;

        float curStepChange = 0f;
        if (branch.HonorDef is not null && medalsCost.TryGetValue(branch.HonorDef.chivalry, out int honorMedalCount))
        {
            curStepChange = honorMedalCount * 100f;
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_BranchResident_MeditationStudy_HonorMedal"
                    .Translate(honorMedalCount.Named(KeyLibrary_FormatArgName.Count),
                               OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef, format: "F0"))
                    .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
        {
            if (tradition.Def.chivalry is null || !medalsCost.TryGetValue(tradition.Def.chivalry, out int traditionMedalCount))
                continue;

            curStepChange = tradition.Level * traditionMedalCount * 25f;
            curValue.Value += curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeOffset_BranchTraditionDetail"
                    .Translate(tradition.Def.Named(OARO_KeyLibrary_FormatArgName.TRADITIONDEF),
                               tradition.Level.Named(KeyLibrary_FormatArgName.Level),
                               OARO_StatExplanationUtility.OffsetNamedArgument(curStepChange, StatDef, format: "F0"))
                    .ColorizeStrByOffset(curStepChange, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        float esteemFactor = 1f + branch.RatkinOrder.Esteem / 4 * 0.01f;
        if (esteemFactor > 1f)
        {
            curValue.Value *= esteemFactor;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_Esteem"
                    .Translate(OARO_StatExplanationUtility.FactorNamedArgument(curStepChange, StatDef))
                    .ColorizeStrByFactor(esteemFactor, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }


        if (studyData.Target.EffectTags.HasTag(KeyLibrary_EffectTag.StudyElite))
        {
            curStepChange = 2f;
            curValue.Value *= curStepChange;
            if (!resultOnly)
            {
                explanation.AppendLineWithSeparator(
                    text: "OARO_ChangeFactor_PawnEffectTag"
                    .Translate(KeyLibrary_EffectTag.StudyElite.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                               OARO_StatExplanationUtility.FactorNamedArgument(curStepChange, StatDef))
                    .ColorizeStrByFactor(curStepChange, reverse: StatDef.reverse),
                    separator: KeyLibrary_Misc.SpaceCap4);
            }
        }

        return true;
    }
}
