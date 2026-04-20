namespace OberoniaAurea.RatkinOrder;

public static class KnightChivalryUtility
{
    /// <summary>
    /// 检查两个骑士精神是否共鸣
    /// </summary>
    public static bool IsChivalryResonate(this KnightChivalryDef chivalry, KnightChivalryDef other)
    {
        if (chivalry is null || other is null)
            return false;
        if (chivalry == other)
            return true;
        if (chivalry.ResonateChivalriesSet.Contains(other))
            return true;
        return false;
    }

    /// <summary>
    /// 检查两个骑士是否精神共鸣
    /// </summary>
    public static bool IsChivalryResonate(this KnightRecord knight, KnightRecord other)
    {
        if (knight is null || other is null)
            return false;

        KnightChivalryDef knightChivalry = knight.Chivalry;
        KnightChivalryDef otherChivalry = other.Chivalry;

        if (knightChivalry is null || otherChivalry is null)
            return false;

        if (knightChivalry == otherChivalry)
            return true;

        if (knightChivalry.ResonateChivalriesSet.Contains(otherChivalry))
            return true;

        return false;
    }

    /// <summary>
    /// 检查两个常驻骑士是否精神共鸣
    /// </summary>
    public static bool IsChivalryResonate(this ResidentKnight knight, ResidentKnight other)
    {
        if (knight is null || other is null)
            return false;

        KnightChivalryDef knightChivalry = knight.Chivalry;
        KnightChivalryDef otherChivalry = other.Chivalry;

        if (knightChivalry is null || otherChivalry is null)
            return false;

        if (knightChivalry == otherChivalry)
            return true;

        if (knightChivalry.ResonateChivalriesSet.Contains(otherChivalry))
            return true;

        return false;
    }
}