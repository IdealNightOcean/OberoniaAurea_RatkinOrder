using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatWorker_AcademicPointsCost(ResidentKnightStatDef statDef) : ResidentKnightStatWorker(statDef)
{
    public override void PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (requestData is ResidentKnightStatRequestData_Academic academicRequestData)
        {
            KnightAcademicDef academicDef = academicRequestData.AcademicDef;
            if (academicDef.chivalry.IsSameDefNonNullable(academicRequestData.Knight.Chivalry))
            {
                curValue /= 2;
                if (!resultOnly)
                {
                    explanation.AppendLine("OARO_CostFactor_SameChivalry".Translate(academicDef.Named(KeyLibrary_FormatArgName.DEF),
                                                                                    academicDef.chivalry.Named(OARO_KeyLibrary_FormatArgName.CHIVALRY),
                                                                                    OAFrame_TextUtility.ColoredFloatNamedArgument(0.5f, KeyLibrary_FormatArgName.Factor)));
                }
            }

        }
    }
}
