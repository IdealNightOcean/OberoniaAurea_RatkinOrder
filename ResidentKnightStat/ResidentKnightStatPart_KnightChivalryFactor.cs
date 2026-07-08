using OberoniaAurea_Frame;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightStatPart_KnightChivalryFactor : ResidentKnightStatPart
{
    public float factor = 1f;
    public KnightChivalryDef chivalryDef;

    public override bool PostTransModify(ResidentKnightStatRequestData requestData,
                                         ref float curValue,
                                         bool resultOnly = true,
                                         StringBuilder explanation = null)
    {
        if (chivalryDef is null || chivalryDef != requestData.Target.Chivalry)
            return false;

        curValue *= factor;
        if (!resultOnly)
        {
            explanation.AppendLineWithSeparator(
                text: "OARO_ChangeFactor_KnightHasChivalry".Translate(
                    chivalryDef.Named(OARO_KeyLibrary_FormatArgName.CHIVALRY),
                    OAFrame_TextUtility.ColoredFloatNamedArgument(factor, KeyLibrary_FormatArgName.Factor, originPoint: 1f)),
                separator: KeyLibrary_Misc.SpaceCap4);
        }

        return true;

    }
}
