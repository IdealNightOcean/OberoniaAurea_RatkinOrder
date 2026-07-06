using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Thought_Situational_OrderStationTraditions : Thought_Situational
{
    private static readonly Dictionary<KnightChivalryDef, int> traditionsWithChivalryCountCache = [];
    private static int totalTraditionsCountCache;
    private static int nextCacheUpdateTick;
    public static void ClearStaticCache()
    {
        traditionsWithChivalryCountCache.Clear();
        totalTraditionsCountCache = 0;
        nextCacheUpdateTick = 0;
    }

    protected override ThoughtState CurrentStateInternal()
    {
        if (totalTraditionsCountCache <= 0)
            return ThoughtState.Inactive;

        if (!pawn.Faction.IsPlayerSafe() || !pawn.CanBeKnight())
            return ThoughtState.Inactive;

        if (!ResidentPawnsManager.Instance.IsResidentColonist(pawn))
            return ThoughtState.Inactive;

        return base.CurrentStateInternal();
    }

    public override float MoodOffset()
    {
        float moodOffset = BaseMoodOffset;

        if (Find.TickManager.TicksGame < nextCacheUpdateTick)
            RefreshCacheIfNeeded();

        if (totalTraditionsCountCache <= 0)
            return moodOffset;

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight knight))
            return moodOffset;

        if (knight.Chivalry.IsSameDefNonNullable(OARO_ModDefOf.OARO_Oath))
        {
            return moodOffset + totalTraditionsCountCache;
        }
        else if (traditionsWithChivalryCountCache.TryGetValue(knight.Chivalry, out int count))
        {
            return moodOffset + 3 * count;
        }

        return moodOffset;
    }

    private static void RefreshCacheIfNeeded()
    {
        nextCacheUpdateTick = Find.TickManager.TicksGame + 10000;

        traditionsWithChivalryCountCache.Clear();
        totalTraditionsCountCache = OrderStationHandler.TraditionsManager.ActiveTraditionCount;
        foreach (OrderStationTraditionDef tradition in OrderStationHandler.TraditionsManager.ActiveTraditions)
        {
            if (tradition.Chivalry is not null)
            {
                if (!traditionsWithChivalryCountCache.ContainsKey(tradition.Chivalry))
                {
                    traditionsWithChivalryCountCache[tradition.Chivalry] = 0;
                }
                traditionsWithChivalryCountCache[tradition.Chivalry]++;
            }
        }
    }
}