using OberoniaAurea_Frame;
using RimWorld;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 美德培养
/// </summary>
public class BranchResident_ResidentKnight_VirtueTrain : BranchResident_ResidentKnightStudy
{
    private KnightVirtueDef virtueDef;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref virtueDef, nameof(virtueDef));
    }

    public override void EndResidency()
    {
        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight residentKnight))
            return;

        if (virtueDef is null)
            return;

        StringBuilder extraTextSB = new(64);
        if (Rand.Chance(KnightDiaryUtility.DiaryGenerationChance))
        {
            Book diary = KnightDiaryUtility.GenerateKnightDiary(this, residentKnight);
            if (diary is not null)
            {
                extraTextSB.AppendLine("OARO_ResidentKnightStudy_DiaryGain".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN)));
                pawn.inventory.TryAddAndUnforbid(diary);
            }
        }

        float successChance = GetTrainSuccessChance(residentKnight: residentKnight,
                                                    resultOnly: true,
                                                    explanation: out _);
        if (Rand.Chance(successChance))
        {
            SuccessOutcome(residentKnight, extraTextSB.ToString());
        }
        else
        {
            Messages.Message(
                text: "OARO_Message_VirtueTrainFailed".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN)),
                lookTargets: pawn,
                def: MessageTypeDefOf.NegativeEvent);
        }


    }

    private void SuccessOutcome(ResidentKnight residentKnight, string extraText)
    {

        int level = GetTrainOutcomeLevel(resultOnly: true, explanation: out _);
        string reason = "OARO_KnightVirtueGainReason_VirtueTrain".Translate(branch.NameColored.Named(OARO_KeyLibrary_FormatArgName.BranchName));

        KnightVirtueHandler virtueHandler = residentKnight.VirtueHandler;
        if (virtueHandler.HasVirtue(virtueDef))
        {
            virtueHandler.UpgradeVirtueTo(virtueDef, level, reason);
        }
        else
        {
            virtueHandler.TryAddVirtue(virtueDef, level, reason);
        }


        StringBuilder letterTextSB = new("OARO_LetterText_VirtueTrainSuccess".Translate(
                                            pawn.Named(KeyLibrary_FormatArgName.PAWN),
                                            virtueDef.Named(OARO_KeyLibrary_FormatArgName.VIRTUEDEF),
                                            level.Named(KeyLibrary_FormatArgName.Level)));
        letterTextSB.AppendLine();
        letterTextSB.AppendLine(extraText);

        OrderLetterUtility.ReceiveLetter(
            label: "OARO_LetterLabel_VirtueTrainSuccess".Translate(pawn.Named(KeyLibrary_FormatArgName.PAWN)),
            text: letterTextSB.ToTaggedString(),
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: branch.RatkinOrder,
            relatedBranch: branch,
            sender: branch.NameColored,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);
    }

    private void FailureOutcome(ResidentKnight residentKnight, string extraText)
    {
        //目前先不设计失败的具体后果，后续可以考虑增加一些负面效果
    }

    public float GetTrainSuccessChance(ResidentKnight residentKnight, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        StringBuilder expSB = resultOnly ? null : new(64);

        KnightChivalryDef virtueChivalry = virtueDef.chivalry;
        float successChance = 0f;

        float curStepChange = 0.2f;
        successChance += curStepChange;
        if (!resultOnly)
        {
            expSB.AppendLine("OARO_BranchResident_VirtueTrain_Base".Translate(curStepChange.ToStringPercent("F0").Named(KeyLibrary_FormatArgName.Chance)));
        }

        if (virtueChivalry.IsSameDefNonNullable(branch.HonorDef?.chivalry))
        {
            curStepChange = 0.2f;
            successChance += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_BranchResident_VirtueTrain_SameChivalryWithHonor".Translate(curStepChange.ToStringPercent("F0").Named(KeyLibrary_FormatArgName.Offset)));
            }
        }

        foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
        {
            if (!virtueChivalry.IsSameDefNonNullable(tradition.Def.chivalry))
                continue;

            curStepChange = (tradition.Level * 0.05f);
            successChance += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ChangeOffset_BranchTraditionDetail".Translate(
                    tradition.Def.Named(OARO_KeyLibrary_FormatArgName.TRADITIONDEF),
                    tradition.Level.Named(KeyLibrary_FormatArgName.Level),
                    curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Offset)));
            }
        }

        if (branch.MedalHandler.MedalRecords.TryGetValue(virtueChivalry, out BranchMedalRecord medalRecord))
        {
            curStepChange = (medalRecord.Count * 0.02f);
            successChance += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_BranchResident_VirtueTrain_SameChivalryWithMedal".Translate(
                    virtueChivalry.Named(KeyLibrary_FormatArgName.DEF),
                    medalRecord.Count.Named(KeyLibrary_FormatArgName.Count),
                    curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Offset)));
            }
        }

        if (virtueChivalry.IsSameDefNonNullable(residentKnight.Chivalry))
        {
            curStepChange = 0.3f;
            successChance += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_BranchResident_VirtueTrain_SameChivalryWithKnight".Translate(curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Offset)));
            }
        }

        if (virtueChivalry.IsSameDefNonNullable(OARO_ModDefOf.OARO_Oath))
        {
            int totalMedalsCost = medalsCost.Values.Sum();
            curStepChange = totalMedalsCost * 0.01f;
            successChance += curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_BranchResident_VirtueTrain_Oath_MedalsCost".Translate(
                    totalMedalsCost.Named(KeyLibrary_FormatArgName.Count),
                    curStepChange.ToStringWithSign("F0").Named(KeyLibrary_FormatArgName.Offset)));
            }
        }

        if (residentKnight.EffectTags.HasTag(KeyLibrary_EffectTag.StudyElite))
        {
            curStepChange = 2f;
            successChance *= curStepChange;
            if (!resultOnly)
            {
                expSB.AppendLine("OARO_ChangeFactor_PawnEffectTag".Translate(
                    KeyLibrary_EffectTag.StudyElite.Named(OARO_KeyLibrary_FormatArgName.EffectTag),
                    curStepChange.ToStringPercent("F0").Named(KeyLibrary_FormatArgName.Factor)));
            }
        }

        successChance = Mathf.Clamp01(successChance);
        if (!resultOnly)
        {
            expSB.AppendLine();
            expSB.AppendLine("OARO_BranchResident_VirtueTrain_SuccessChance".Translate(successChance.ToStringPercent("F0").Named(KeyLibrary_FormatArgName.Chance)));
            explanation = expSB.ToString();
        }
        return successChance;
    }

    public int GetTrainOutcomeLevel(bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        KnightChivalryDef virtueChivalry = virtueDef.chivalry;

        int totalMedalsCost = medalsCost.Values.Sum();
        bool sameChivalryWithHonor = virtueChivalry.IsSameDefNonNullable(branch.HonorDef?.chivalry);
        bool isOathVirtue = virtueChivalry.IsSameDefNonNullable(OARO_ModDefOf.OARO_Oath);

        medalsCost.TryGetValue(virtueChivalry, out int medalsHasSameChivalry);

        int branchTraditionLevelHasSameChivalry = 0;
        foreach (BranchTradition tradition in branch.TraditionHandler.Traditions)
        {
            if (!virtueChivalry.IsSameDefNonNullable(tradition.Def.chivalry))
                continue;

            branchTraditionLevelHasSameChivalry += tradition.Level;
        }

        (int, float)[] levelWeightPair = new (int, float)[4];
        float curLevelWeight = 0f;

        //等级1
        curLevelWeight = 120f;
        curLevelWeight -= (branchTraditionLevelHasSameChivalry * 10f);
        if (sameChivalryWithHonor)
            curLevelWeight -= 20f;

        curLevelWeight = Mathf.Max(0f, curLevelWeight);
        levelWeightPair[0] = (1, curLevelWeight);

        //等级2
        curLevelWeight = 40f;
        curLevelWeight += (branchTraditionLevelHasSameChivalry * 5f);
        curLevelWeight += (totalMedalsCost * (isOathVirtue ? 4f : 2f));
        if (sameChivalryWithHonor)
            curLevelWeight += 10f;

        curLevelWeight = Mathf.Max(0f, curLevelWeight);
        levelWeightPair[1] = (2, curLevelWeight);

        //等级3
        curLevelWeight = -30f;
        curLevelWeight += (branchTraditionLevelHasSameChivalry * 20f);
        curLevelWeight += (totalMedalsCost * (isOathVirtue ? 4f : 1f));
        curLevelWeight += (medalsHasSameChivalry * 3f);
        if (sameChivalryWithHonor)
            curLevelWeight += 25f;

        curLevelWeight = Mathf.Max(0f, curLevelWeight);
        levelWeightPair[2] = (3, curLevelWeight);

        //等级4
        curLevelWeight = -90f;
        curLevelWeight += (branchTraditionLevelHasSameChivalry * 20f);
        curLevelWeight += (totalMedalsCost * 4f);
        if (sameChivalryWithHonor)
            curLevelWeight += 25f;

        curLevelWeight = Mathf.Max(0f, curLevelWeight);
        levelWeightPair[3] = (4, curLevelWeight);

        float totalWeight = levelWeightPair.Sum(pair => pair.Item2);

        if (!resultOnly)
        {
            StringBuilder expSB = new(32);
            for (int i = 0; i < levelWeightPair.Length; i++)
            {
                expSB.AppendLine($"OARO_BranchResident_VirtueTrain_LevelChance".Translate
                    (
                        levelWeightPair[i].Item1.Named(KeyLibrary_FormatArgName.Level),
                        (levelWeightPair[i].Item2 / totalWeight).ToStringPercent("0.##").Named(KeyLibrary_FormatArgName.Chance)
                    ));
            }
            explanation = expSB.ToString();
        }

        return levelWeightPair.RandomElementByWeight(pair => pair.Item2).Item1;
    }

}