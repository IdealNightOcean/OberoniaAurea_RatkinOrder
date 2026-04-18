using OberoniaAurea_Frame;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchResident_ResidentKnightStudy : BranchResident
{
    private Dictionary<BranchMedalDef, int> medalsCost = [];

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref medalsCost, nameof(medalsCost), LookMode.Def, LookMode.Value);
    }

    public override void StartResidency(Branch branch)
    {
        base.StartResidency(branch);
        OAFrame_PawnUtility.MakePawnJoinPlayer(pawn);
    }


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

        float curStepGain = medalsCost.Values.Sum() * 200f;
        meditationGain += curStepGain;
        if (!resultOnly)
        {
            expSB.AppendLine("OARO_ResidentKnightStudy_BaseMedal".Translate(curStepGain.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Change)));
        }

        if (branch.HonorDef is not null && medalsCost.TryGetValue(branch.HonorDef.medalDef, out int honorMedalCount))
        {
            curStepGain = honorMedalCount * 100f;
            meditationGain += curStepGain;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ResidentKnightStudy_HonorMedal".Translate(
                    branch.HonorDef.medalDef.Named(KeyLibrary_FormatArgName.HONORDEF),
                    curStepGain.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Change)));
            }
        }

        foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
        {
            if (!medalsCost.TryGetValue(tradition.Def.medalDef, out int traditionMedalCount))
                continue;

            curStepGain = tradition.Level * traditionMedalCount * 25f;
            meditationGain += curStepGain;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ResidentKnightStudy_TraditionMedal".Translate(
                    tradition.Def.medalDef.Named(KeyLibrary_FormatArgName.HONORDEF),
                    tradition.Level.Named(KeyLibrary_FormatArgName.Level),
                    curStepGain.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Change)));
            }
        }

        float esteemFactor = 1f + branch.RatkinOrder.Esteem / 4 * 0.01f;
        if (esteemFactor > 1f)
        {
            meditationGain *= esteemFactor;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ResidentKnightStudy_TraditionMedal".Translate(esteemFactor.ToStringPercent("F0").Named(KeyLibrary_FormatArgName.Factor)));
            }
        }

        if (!resultOnly)
        {
            explanation = expSB.ToString();
        }

        return meditationGain;
    }
}