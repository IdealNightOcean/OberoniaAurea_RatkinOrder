using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士日记工具类
/// </summary>
public static class KnightDiaryUtility
{
    public const float DiaryGenerationChance = 0.2f;


    public static Book GenerateKnightDiary(BranchResident_ResidentKnightStudy record, ResidentKnight residentKnight)
    {
        if (record is null || residentKnight?.Pawn is null)
            return null;

        int medalsCost = record.MedalsCost.Values.Sum();
        Book diary = (Book)ThingMaker.MakeThing(OARO_ThingDefOf.OARO_KnightDiary);
        QualityCategory quality = DetermineDiaryQuality(residentKnight, medalsCost, resultOnly: true, out _);
        diary.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Outsider);

        return diary;
    }

    /// <summary>
    /// 确定骑士日记品质
    /// </summary>
    public static QualityCategory DetermineDiaryQuality(ResidentKnight knight, int medalsCost, bool resultOnly, out string explanation)
    {
        explanation = string.Empty;
        float virtueStat = knight.KnightVirtueHandler?.VirtueStatValueCache.GetCachedResult() ?? 0f;

        List<(QualityCategory quality, float weight)> qualityWeights = new(7);

        float awfulWeight = Mathf.Max(0f, 40f - virtueStat * 2f - medalsCost * 2f);
        awfulWeight = Mathf.Max(awfulWeight, 0f);
        qualityWeights.Add((QualityCategory.Awful, awfulWeight));

        float poorWeight = Mathf.Max(0f, 30f - virtueStat - medalsCost);
        poorWeight = Mathf.Max(poorWeight, 0f);
        qualityWeights.Add((QualityCategory.Poor, poorWeight));

        float normalWeight = Mathf.Max(0f, 40f - virtueStat - medalsCost);
        normalWeight = Mathf.Max(normalWeight, 0f);
        qualityWeights.Add((QualityCategory.Normal, normalWeight));

        float goodWeight = Mathf.Max(0f, 10f + virtueStat);
        goodWeight = Mathf.Max(goodWeight, 0f);
        qualityWeights.Add((QualityCategory.Good, goodWeight));

        float excellentWeight = Mathf.Max(0f, virtueStat * 2f);
        excellentWeight = Mathf.Max(excellentWeight, 0f);
        qualityWeights.Add((QualityCategory.Excellent, excellentWeight));

        float masterworkWeight = Mathf.Max(0f, -20f + virtueStat * 4f);
        masterworkWeight = Mathf.Max(masterworkWeight, 0f);
        qualityWeights.Add((QualityCategory.Masterwork, masterworkWeight));

        float legendaryWeight = Mathf.Max(0f, -80f + virtueStat * 8f);
        legendaryWeight = Mathf.Max(legendaryWeight, 0f);
        qualityWeights.Add((QualityCategory.Legendary, legendaryWeight));

        if (!resultOnly)
        {
            float totalWeight = qualityWeights.Sum(p => p.weight);
            StringBuilder expSB = new(64);
            foreach ((QualityCategory quality, float weight) in qualityWeights)
            {
                expSB.AppendLine($"{quality.GetLabel()}: {(weight / totalWeight).ToStringPercent("0.##")}");
            }
        }

        return qualityWeights.RandomElementByWeight(p => p.weight).quality;
    }
}