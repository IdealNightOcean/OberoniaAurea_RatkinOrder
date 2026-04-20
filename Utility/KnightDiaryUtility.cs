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

    /// <summary>
    /// 尝试生成骑士日记
    /// </summary>
    /// <param name="knight">骑士记录</param>
    /// <param name="medalsCost">消耗的勋章数量</param>
    /// <returns>是否成功生成</returns>
    public static bool TryGenerateKnightDiary(ResidentKnight knight, int medalsCost)
    {
        if (knight?.Pawn is null)
            return false;

        if (!Rand.Chance(DiaryGenerationChance))
            return false;

        QualityCategory quality = DetermineDiaryQuality(knight, medalsCost, resultOnly: true, out _);
        Thing diary = MakeKnightDiary(knight, quality, medalsCost);

        if (diary is null)
            return false;

        GenPlace.TryPlaceThing(diary, knight.Pawn.Position, knight.Pawn.Map, ThingPlaceMode.Near);

        Messages.Message(
            text: "OARO_Message_KnightDiaryGenerated".Translate(
                knight.Pawn.Named(KeyLibrary_FormatArgName.PAWN),
                quality.GetLabel().Named(KeyLibrary_FormatArgName.Quality)),
            lookTargets: diary,
            def: MessageTypeDefOf.PositiveEvent);

        return true;
    }

    /// <summary>
    /// 确定骑士日记品质
    /// </summary>
    private static QualityCategory DetermineDiaryQuality(ResidentKnight knight, int medalsCost, bool resultOnly, out string explanation)
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

    /// <summary>
    /// 制作骑士日记物品
    /// </summary>
    private static Thing MakeKnightDiary(ResidentKnight knight, QualityCategory quality, int medalsCost)
    {
        throw new System.NotImplementedException("骑士日记物品尚未实现");
        /*
        ThingDef diaryDef = ThingDefOf.Book;
        if (diaryDef is null)
            return null;

        Thing diary = ThingMaker.MakeThing(diaryDef);
        diary.TryGetComp<CompQuality>()?.SetQuality(quality, ArtGenerationContext.Colony);

        CompBook bookComp = diary.TryGetComp<CompBook>();
        if (bookComp is not null)
        {
            bookComp.InitializeBook();
        }

        return diary;
        */
    }

    /// <summary>
    /// 获取骑士日记的修行点数效果
    /// </summary>
    public static float GetMeditationPointsPerHour(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 15f,
            QualityCategory.Poor => 18f,
            QualityCategory.Normal => 21f,
            QualityCategory.Good => 24f,
            QualityCategory.Excellent => 27f,
            QualityCategory.Masterwork => 30f,
            QualityCategory.Legendary => 35f,
            _ => 15f
        };
    }

    /// <summary>
    /// 获取骑士日记的技能增长效果
    /// </summary>
    public static float GetSkillXPGainPerHour(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 0f,
            QualityCategory.Poor => 0f,
            QualityCategory.Normal => 150f,
            QualityCategory.Good => 210f,
            QualityCategory.Excellent => 270f,
            QualityCategory.Masterwork => 330f,
            QualityCategory.Legendary => 390f,
            _ => 0f
        };
    }

    /// <summary>
    /// 获取骑士日记的娱乐倍率
    /// </summary>
    public static float GetJoyGainFactor(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 0.3f,
            QualityCategory.Poor => 0.35f,
            QualityCategory.Normal => 0.4f,
            QualityCategory.Good => 0.45f,
            QualityCategory.Excellent => 0.5f,
            QualityCategory.Masterwork => 0.55f,
            QualityCategory.Legendary => 0.6f,
            _ => 0.3f
        };
    }

    /// <summary>
    /// 获取骑士日记的技能等级上限
    /// </summary>
    public static int GetSkillLevelCap(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Normal => 8,
            QualityCategory.Good => 10,
            QualityCategory.Excellent => 12,
            QualityCategory.Masterwork => 14,
            QualityCategory.Legendary => 16,
            _ => 0
        };
    }
}
