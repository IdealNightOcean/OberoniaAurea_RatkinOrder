using System.Linq;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 课业进修
/// </summary>
public class BranchResident_ResidentKnight_MeditationStudy : BranchResident_ResidentKnightStudy
{
    public override void EndResidency()
    {
        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight residentKnight))
            return;

        float meditationGain = GetMeditationGain(resultOnly: false,
                                                 explanation: out string explanation);
        residentKnight.MeditationPoints += meditationGain;
        StringBuilder letterTextSB = new("OARO_MeditationStudyComplete_Meditation".Translate(
                                            pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                            meditationGain.ToString("F0").Named(KeyLibrary_FormatArgName.Count),
                                            explanation.Named(KeyLibrary_FormatArgName.Reason)));

        letterTextSB.AppendLine();

        float virtueGainChance = medalsCost.Values.Sum() * 0.02f;
        if (Rand.Chance(virtueGainChance))
        {
            KnightVirtueDef newVirtueDef = KnightVirtueUtility.GetRandomAvailableVirtue(residentKnight);
            if (newVirtueDef is not null)
            {
                letterTextSB.AppendLine("OARO_MeditationStudyComplete_VirtueGain".Translate(
                                            pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                            newVirtueDef.Named(KeyLibrary_FormatArgName.VIRTUEDEF)));
                residentKnight.VirtueHandler.TryAddVirtue(virtueDef: newVirtueDef,
                                                                level: 1,
                                                                reason: "OARO_KnightVirtueGainReason_MeditationStudy".Translate(branch.NameColored.Named(KeyLibrary_FormatArgName.BranchName)));
            }
        }

        if (Rand.Chance(KnightDiaryUtility.DiaryGenerationChance) || residentKnight.EffectTags.HasTag(KeyLibrary_EffectTag.StudyElite))
        {
            Book diary = KnightDiaryUtility.GenerateKnightDiary(this, residentKnight);
            if (diary is not null)
            {
                letterTextSB.AppendLine("OARO_ResidentKnightStudy_DiaryGain".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN)));
                pawn.inventory.TryAddAndUnforbid(diary);
            }
        }

        OrderLetterUtility.ReceiveLetter(
            label: "OARO_LetterLabel_MeditationStudyComplete".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN)),
            text: letterTextSB.ToTaggedString(),
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);
    }

    public float GetMeditationGain(bool resultOnly, out string explanation)
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

        if (branch.HonorDef is not null && medalsCost.TryGetValue(branch.HonorDef.chivalry, out int honorMedalCount))
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
            if (tradition.Def.chivalry is null || !medalsCost.TryGetValue(tradition.Def.chivalry, out int traditionMedalCount))
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

        if (ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight residentKnight))
        {
            if (residentKnight.EffectTags.HasTag(KeyLibrary_EffectTag.StudyElite))
            {
                meditationGain *= 2f;
                if (!resultOnly)
                {
                    expSB.AppendLine("OARO_ChangeFactor_PawnEffectTag".Translate(
                        KeyLibrary_EffectTag.StudyElite.Named(KeyLibrary_FormatArgName.EffectTag),
                        2f.ToStringPercent("F0").Named(KeyLibrary_FormatArgName.Factor)));
                }
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