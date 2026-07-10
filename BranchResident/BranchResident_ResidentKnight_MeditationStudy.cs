using OberoniaAurea_Frame;
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

        ResidentKnightStatRequestData_ResidentKnightStudy requestData = new(residentKnight, ResidentKnightStatDefOf.OARO_MeditationStudyPointGain)
        {
            MedalsCost = medalsCost,
        };
        (string explanation, float? meditationGain) = ResidentKnightStatDefOf.OARO_MeditationStudyPointGain.GetStatModifyExplanation(requestData);
        if (!meditationGain.HasValue)
            return;

        residentKnight.MeditationPoints += meditationGain.Value;
        StringBuilder letterTextSB = new("OARO_MeditationStudyComplete_Meditation".Translate(
                                            pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                            meditationGain.Value.ToString("F0").Named(KeyLibrary_FormatArgName.Count),
                                            explanation.ToString().Named(KeyLibrary_FormatArgName.Reason)));

        letterTextSB.AppendLine();

        float virtueGainChance = medalsCost.Values.Sum() * 0.02f;
        if (Rand.Chance(virtueGainChance))
        {
            KnightVirtueDef newVirtueDef = KnightVirtueUtility.GetRandomAvailableVirtue(residentKnight);
            if (newVirtueDef is not null)
            {
                letterTextSB.AppendLine("OARO_MeditationStudyComplete_VirtueGain".Translate(
                                            pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                            newVirtueDef.Named(OARO_KeyLibrary_FormatArgName.VIRTUEDEF)));
                residentKnight.VirtueHandler.TryAddVirtue(virtueDef: newVirtueDef,
                                                                level: 1,
                                                                reason: "OARO_KnightVirtueGainReason_MeditationStudy".Translate(branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName)));
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
}