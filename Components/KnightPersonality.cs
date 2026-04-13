using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[Flags]
public enum KnightPersonality : byte
{
    None = 0,
    Courage = 1, //勇气
    Tenacity = 2, //坚毅
    Compassion = 4, //怜悯
    Oath = 8, //誓言
    Justice = 16 //正义
}

public static class KnightPersonalityExtension
{
    public const int AvailablePersonalitiesCount = 5;
    public static KnightPersonality[] EnumsArrary => (KnightPersonality[])Enum.GetValues(typeof(KnightPersonality));
    public static KnightPersonality GetRandomAvailablePersonality() => EnumsArrary[Rand.Range(1, EnumsArrary.Length)];

    public static IEnumerable<KnightPersonality> GetContainedPersonalities(KnightPersonality personality)
    {
        for (int i = 1; i < EnumsArrary.Length; i++)
        {
            if ((personality & EnumsArrary[i]) != 0)
            {
                yield return EnumsArrary[i];
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

    public static string GetLabel(this KnightPersonality personality)
    {
        return $"OARO_KnightPersonality_{personality}".Translate();
    }

    public static Color GetColor(this KnightPersonality personality)
    {
        return personality switch
        {
            KnightPersonality.None => Color.white,
            KnightPersonality.Courage => ColorLibrary.Orange,
            KnightPersonality.Tenacity => Color.yellow,
            KnightPersonality.Compassion => Color.cyan,
            KnightPersonality.Oath => new Color(0.75f, 0.75f, 0.75f),
            KnightPersonality.Justice => Color.green,
            _ => Color.white,
        };
    }

    public static Texture2D GetColorTex(this KnightPersonality personality)
    {
        return personality switch
        {
            KnightPersonality.None => BaseContent.WhiteTex,
            KnightPersonality.Courage => IconLibrary.OrangeTex,
            KnightPersonality.Tenacity => BaseContent.YellowTex,
            KnightPersonality.Compassion => IconLibrary.CyanTex,
            KnightPersonality.Oath => IconLibrary.SilverTex,
            KnightPersonality.Justice => IconLibrary.GreenTex,
            _ => BaseContent.WhiteTex
        };
    }
}
