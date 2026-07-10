using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatPart_Factor_KnightChivalry : ResidentKnightStatPart
{
    public float factor = 1f;
    public KnightChivalryDef chivalryDef;

    public override bool PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref StatComputeState curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (chivalryDef is null || chivalryDef != requestData.Target.Chivalry)
            return false;

        curValue.Value *= factor;
        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeFactor_KnightHasChivalry"
                .Translate(
                    chivalryDef.Named(OARO_KeyLibrary_FormatArgName.CHIVALRY),
                    OARO_StatExplanationUtility.FactorNamedArgument(factor, requestData.StatDef))
                .ColorizeStrByFactor(factor, reverse: requestData.StatDef.reverse),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;

    }
}



