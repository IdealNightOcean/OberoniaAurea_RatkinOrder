using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class KnightPersonalityUtility
{
    public static KnightPersonality GetRandomAvailablePersonality() => EnumArraryLibrary.KnightPersonalitiesArr[Rand.Range(1, EnumArraryLibrary.KnightPersonalitiesArr.Length)];
    public static IEnumerable<KnightPersonality> GetContainedPersonalities(KnightPersonality personality)
    {
        KnightPersonality[] knightPersonalitiesArr = EnumArraryLibrary.KnightPersonalitiesArr;
        for (int i = 1; i < knightPersonalitiesArr.Length; i++)
        {
            if ((personality & knightPersonalitiesArr[i]) != 0)
            {
                yield return knightPersonalitiesArr[i];
            }
        }
    }

    /// <summary>
    /// 是否为相互共鸣个性
    /// </summary>
    public static bool IsResonatePersonality(KnightPersonality personality, KnightPersonality other)
    {
        return personality switch
        {
            KnightPersonality.None => false,
            KnightPersonality.Courage => (other & (KnightPersonality.Tenacity | KnightPersonality.Oath)) != 0,
            KnightPersonality.Tenacity => (other & (KnightPersonality.Courage | KnightPersonality.Compassion)) != 0,
            KnightPersonality.Compassion => (other & (KnightPersonality.Tenacity | KnightPersonality.Justice)) != 0,
            KnightPersonality.Oath => (other & (KnightPersonality.Courage | KnightPersonality.Justice)) != 0,
            KnightPersonality.Justice => (other & (KnightPersonality.Compassion | KnightPersonality.Oath)) != 0,
            _ => false,
        };
    }

    /// <summary>
    /// 获取共鸣个性
    /// </summary>
    public static KnightPersonality GetResonatePersonality(KnightPersonality personality)
    {
        return personality switch
        {
            KnightPersonality.None => KnightPersonality.None,
            KnightPersonality.Courage => KnightPersonality.Tenacity | KnightPersonality.Oath,
            KnightPersonality.Tenacity => KnightPersonality.Courage | KnightPersonality.Compassion,
            KnightPersonality.Compassion => KnightPersonality.Tenacity | KnightPersonality.Justice,
            KnightPersonality.Oath => KnightPersonality.Courage | KnightPersonality.Justice,
            KnightPersonality.Justice => KnightPersonality.Compassion | KnightPersonality.Oath,
            _ => KnightPersonality.None,
        };
    }
}
