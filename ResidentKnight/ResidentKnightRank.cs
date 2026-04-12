using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻骑士阶位
/// </summary>
public enum ResidentKnightRank : byte
{
    /// <summary>
    /// 常规
    /// </summary>
    Regular,
    /// <summary>
    /// 精英
    /// </summary>
    Elite,
    /// <summary>
    /// 荣誉
    /// </summary>
    Honor,
    /// <summary>
    /// 冠位
    /// </summary>
    Crown
}

public static class ResidentKnightRankExtensions
{
    public static ResidentKnightRank OffsetBy(this ResidentKnightRank rank, int offset) => (ResidentKnightRank)Mathf.Clamp((int)rank + offset, 0, 3);

    public static Color GetColor(this ResidentKnightRank rank)
    {
        return rank switch
        {
            ResidentKnightRank.Regular => new Color(0.3f, 0.9f, 0.39f),
            ResidentKnightRank.Elite => new Color(0.3f, 0.51f, 0.9f),
            ResidentKnightRank.Honor => new Color(0.69f, 0.3f, 0.9f),
            ResidentKnightRank.Crown => new Color(1f, 0.65f, 0f),
            _ => Color.white
        };
    }

    public static string GetLabel(this ResidentKnightRank rank)
    {
        return rank switch
        {
            ResidentKnightRank.Regular => $"OARO_ResidentKnightRank_{ResidentKnightRank.Regular}".Translate().Colorize(new Color(0.3f, 0.9f, 0.39f)),
            ResidentKnightRank.Elite => $"OARO_ResidentKnightRank_{ResidentKnightRank.Elite}".Translate().Colorize(new Color(0.3f, 0.51f, 0.9f)),
            ResidentKnightRank.Honor => $"OARO_ResidentKnightRank_{ResidentKnightRank.Honor}".Translate().Colorize(new Color(0.69f, 0.3f, 0.9f)),
            ResidentKnightRank.Crown => $"OARO_ResidentKnightRank_{ResidentKnightRank.Crown}".Translate().Colorize(new Color(1f, 0.65f, 0f)),
            _ => "ERROR (；′⌒`)".Colorize(ColorLibrary.RedReadable)
        };
    }
}