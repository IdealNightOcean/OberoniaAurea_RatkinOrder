using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResident_ResidentKnight_MeditationStudy : BranchResident_ResidentKnightStudy
{

    public override void EndResidency(Branch branch)
    {
        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight residentKnight))
            return;


    }

    private float GetMeditationGain(Branch branch, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        StringBuilder expSB = resultOnly ? null : new(64);

        float meditationGain = 0f;

        float curStepChange = medalsCost.Values.Sum() * 200f;
        meditationGain += curStepChange;
        if (!resultOnly)
        {
            expSB.AppendLine("OARO_BranchResident_MeditationStudy_BaseMedal".Translate(curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Change)));
        }

        if (branch.HonorDef is not null && medalsCost.TryGetValue(branch.HonorDef.medalDef, out int honorMedalCount))
        {
            curStepChange = honorMedalCount * 100f;
            meditationGain += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_BranchResident_MeditationStudy_HonorMedal".Translate(
                    honorMedalCount.Named(KeyLibrary_FormatArgName.Count),
                    curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Offset)));
            }
        }

        foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
        {
            if (tradition.Def.medalDef is null || !medalsCost.TryGetValue(tradition.Def.medalDef, out int traditionMedalCount))
                continue;

            curStepChange = tradition.Level * traditionMedalCount * 25f;
            meditationGain += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ChangeOffset_BranchTraditionDetail".Translate(
                    tradition.Def.Named(KeyLibrary_FormatArgName.TRADITIONDEF),
                    tradition.Level.Named(KeyLibrary_FormatArgName.Level),
                    curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Change)));
            }
        }

        float esteemFactor = 1f + branch.RatkinOrder.Esteem / 4 * 0.01f;
        if (esteemFactor > 1f)
        {
            meditationGain *= esteemFactor;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ChangeFactor_Esteem".Translate(esteemFactor.ToStringPercent("F0")));
            }
        }

        if (!resultOnly)
        {
            expSB.AppendLine();
            expSB.AppendLine("OARO_BranchResident_MeditationStudy_MeditationGain".Translate(meditationGain.ToString("F0").Named(KeyLibrary_FormatArgName.Count)));
            explanation = expSB.ToString();
        }

        return meditationGain;
    }
}