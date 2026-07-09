using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Thought_Situational_Virtue : Thought_SituationalSocial
{
    [Unsaved] private SimpleValueCache<int> cachedVirtue;

    public Thought_Situational_Virtue() : base()
    {
        cachedVirtue = new(cacheInterval: 30000,
                           defaultValue: 0,
                           checker: () => Mathf.RoundToInt(otherPawn?.GetStatValue(OARO_ModDefOf.OARO_Stat_PawnVirtue) ?? 0f));
    }

    public override float OpinionOffset()
    {
        if (ThoughtUtility.ThoughtNullified(pawn, def))
            return 0f;

        int offset = cachedVirtue.GetCachedResult() * 5;
        return Math.Max(offset, 0);
    }
}