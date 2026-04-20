using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class KnightVirtueUtility
{
    public const float MentorshipVirtueChance_Base = 0.04f;
    public const float MentorshipVirtueChance_Resonate = 0.10f;

    public static bool CanAcquireVirtue(this ResidentKnight knight, KnightVirtueDef virtueDef)
    {
        if (knight is null || virtueDef is null)
            return false;

        KnightVirtueHandler virtueHandler = knight.KnightVirtueHandler;

        if (virtueHandler.TotalVirtueCount >= virtueHandler.CurVirtueCountLimit)
            return false;

        if (virtueHandler.HasVirtue(virtueDef))
            return false;

        return knight.IsVirtueUnlockedForKnight(virtueDef);
    }

    public static bool IsVirtueUnlockedForKnight(this ResidentKnight knight, KnightVirtueDef virtueDef)
    {
        if (knight?.Pawn is null)
        {
            return false;
        }

        switch (virtueDef.virtueType)
        {
            case KnightVirtueType.Normal: return true;
            case KnightVirtueType.Academic:
                {
                    if (virtueDef.unlockOnAcademicLevel <= 0 || virtueDef.relatedAcademicDef is null)
                        return true;

                    return knight.AcademicHandler.GetAcademicLevel(virtueDef.relatedAcademicDef) >= virtueDef.unlockOnAcademicLevel;
                }
            default: return true;
        }
    }

    public static IEnumerable<KnightVirtueDef> GetAllAvailableVirtues(ResidentKnight knight)
    {
        if (knight.KnightVirtueHandler.TotalVirtueCount >= knight.KnightVirtueHandler.CurVirtueCountLimit)
            yield break;

        foreach (KnightVirtueDef virtueDef in DefDatabase<KnightVirtueDef>.AllDefsListForReading)
        {
            if (knight.CanAcquireVirtue(virtueDef))
                yield return virtueDef;
        }
    }

    public static KnightVirtueDef GetRandomAvailableVirtue(ResidentKnight knight)
    {
        return GetAllAvailableVirtues(knight).RandomElementWithFallback(null);
    }

    /// <summary>
    /// 获取授导骑士可以传授给学生的美德列表
    /// </summary>
    public static IEnumerable<KnightVirtueDef> GetTeachableVirtues(ResidentKnight teacher, ResidentKnight student)
    {
        if (teacher is null || student is null)
            yield break;

        foreach (KnightVirtue virtue in teacher.KnightVirtueHandler.Virtues)
        {
            if (student.CanAcquireVirtue(virtue.Def))
                yield return virtue.Def;
        }
    }

    public static int GetRandomNewVirtueLevel_Daily(ResidentKnight knight)
    {
        float virtueStatValue = knight.KnightVirtueHandler.VirtueStatValueCache.GetCachedResult();
        (int, float)[] weightSelector =
            [
                (1, 60f),
                (2, 20f + virtueStatValue * 2f),
                (3, virtueStatValue * 2f)
            ];

        return weightSelector.RandomElementByWeight(p => p.Item2).Item1;
    }

    public static int GetRandomNewVirtueLevel_MentalBreak(ResidentKnight knight)
    {
        float virtueStatValue = knight.KnightVirtueHandler.VirtueStatValueCache.GetCachedResult();
        (int, float)[] weightSelector =
            [
                (1, 30f),
                (2, 20f + virtueStatValue * 2f),
                (3, virtueStatValue * 3f)
            ];

        return weightSelector.RandomElementByWeight(p => p.Item2).Item1;
    }

    /// <summary>
    /// 获取骑士授导获取美德的等级权重
    /// </summary>
    /// <param name="isResonate">是否精神共鸣</param>
    public static int GetRandomNewVirtueLevel_Mentorship(ResidentKnight teacher, bool isResonate)
    {
        float teacherVirtueStat = teacher?.KnightVirtueHandler?.VirtueStatValueCache.GetCachedResult() ?? 0f;
        int resonateBonus = isResonate ? 1 : 0;

        (int, float)[] weightSelector =
            [
                (1,  20f),
                (2, 20f + teacherVirtueStat * 2f + resonateBonus * 10f),
                (3, teacherVirtueStat * 4f + resonateBonus * 20f),
            ];

        return weightSelector.RandomElementByWeight(p => p.Item2).Item1;
    }

    public static IEnumerable<KnightVirtueDef> GetAllUpgradableVirtues(ResidentKnight knight, Predicate<KnightVirtueDef> predicate = null)
    {
        if (knight is null)
            yield break;

        KnightVirtueHandler virtueHandler = knight.KnightVirtueHandler;

        if (predicate is null)
        {
            foreach (KnightVirtue virtue in virtueHandler.Virtues)
            {
                if (virtue.Level < virtue.Def.maxLevel)
                    yield return virtue.Def;
            }
        }
        else
        {
            foreach (KnightVirtue virtue in virtueHandler.Virtues)
            {
                if (virtue.Level < virtue.Def.maxLevel && predicate(virtue.Def))
                    yield return virtue.Def;
            }
        }
    }

    public static KnightVirtueDef GetRandomUpgradableVirtue(ResidentKnight knight, Predicate<KnightVirtueDef> predicate = null)
    {
        return GetAllUpgradableVirtues(knight, predicate).RandomElementWithFallback(null);
    }
}