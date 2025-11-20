using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class KnightPawnsManager
{
    private static readonly Dictionary<Pawn, KnightRecord> knights = new(32);
    public static void ClearStaticCache()
    {
        knights.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanBeKnight(this Pawn pawn) => pawn is not null && pawn.RaceProps.Humanlike;
    public static void RegisterKnight(Pawn pawn, KnightRecord knightRecord)
    {
        if (!pawn.CanBeKnight())
        {
            Log.Error($"[OARO] Failed to register pawn ({pawn}) to KnightPawnsManager: this pawn cannot be a knight.");
            return;
        }
        knights[pawn] = knightRecord;
    }

    public static bool DeregisterKnight(Pawn pawn) => knights.Remove(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsKnight(this Pawn pawn) => CanBeKnight(pawn) && knights.ContainsKey(pawn);

    public static bool IsKnightCommander(this Pawn pawn)
    {
        if (CanBeKnight(pawn) && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.IsCommander;
        }
        return false;
    }

    public static KnightRecord GetKnightRecord(this Pawn pawn)
    {
        if (knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record;
        }
        return null;
    }

    public static bool TryGetKnightRecord(this Pawn pawn, out KnightRecord record)
    {
        record = null;
        return CanBeKnight(pawn) && knights.TryGetValue(pawn, out record);
    }

    public static RatkinOrder GetKnightOrder(this Pawn pawn)
    {
        if (CanBeKnight(pawn) && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.RatkinOrder;
        }
        return null;
    }

    public static Branch GetKnightBranch(this Pawn pawn)
    {
        if (CanBeKnight(pawn) && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.Branch;
        }
        return null;
    }

    public static bool IsKnightOfOrder(this Pawn pawn, RatkinOrder ratkinOrder)
    {
        if (CanBeKnight(pawn) && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.RatkinOrder == ratkinOrder;
        }
        return false;
    }

    public static bool IsKnightOfBranch(this Pawn pawn, Branch branch)
    {
        if (CanBeKnight(pawn) && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.Branch == branch;
        }
        return false;
    }

    public static Hediff_Knight GetKnightHediff(this Pawn pawn)
    {
        if (!pawn.CanBeKnight())
        {
            return null;
        }

        return pawn.health.hediffSet.GetFirstHediffOfDef(OARO_HediffDefOf.OARO_Hediff_OrderKnight) as Hediff_Knight;
    }
}