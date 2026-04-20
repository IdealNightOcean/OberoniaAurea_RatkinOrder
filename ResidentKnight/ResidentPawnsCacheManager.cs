using NightOcean;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻人员缓存管理器 - 负责管理常驻骑士相关的各种缓存数据，提供便捷的接口供其他系统查询和使用
/// </summary>
public class ResidentPawnsCacheManager
{
    public ResidentPawnsManager Parent { get; }
    private IReadOnlyList<ResidentKnight> ResidentKnights => Parent.ResidentKnights;

    public LazyMutable<HashSet<KnightChivalryDef>> AllHasChivalriesDefs { get; }
    public LazyMutable<int> InstructorKnightsCount { get; }

    public LazyMutableCollection<Dictionary<KnightChivalryDef, int>, KeyValuePair<KnightChivalryDef, int>> KnightsWithChivalryCount { get; }
    public LazyMutableCollection<List<Pawn>, Pawn> KnightsHasUnusedTraitSlot { get; }
    public LazyMutableCollection<List<Pawn>, Pawn> KnightsApproachingResignation { get; }

    private bool AnyResidentKnights => ResidentKnights is not null && ResidentKnights.Count > 0;
    public ResidentPawnsCacheManager(ResidentPawnsManager parent)
    {
        Parent = parent;
        AllHasChivalriesDefs = new(refreshFunc: delegate
        {
            HashSet<KnightChivalryDef> knightChivalryDefs = [];
            if (AnyResidentKnights)
                return knightChivalryDefs;

            foreach (ResidentKnight residentKnight in ResidentKnights)
            {
                if (residentKnight?.Chivalry is not null)
                {
                    knightChivalryDefs.Add(residentKnight.Chivalry);
                }
            }
            return knightChivalryDefs;
        });

        InstructorKnightsCount = new(refreshFunc: () => ResidentKnights.Where(r => r?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_Instructor).Count());
        KnightsWithChivalryCount = new(refreshFunc: RefreshKnightsWithChivalryCount);
        KnightsHasUnusedTraitSlot = new(refreshFunc: RefreshKnightsHasUnusedTraitSlot);
        KnightsApproachingResignation = new(refreshFunc: RefreshKnightsApproachingResignation);
    }

    public void TickHour()
    {
        KnightsApproachingResignation.MarkDirty();
    }

    public void OnKnightsChanged()
    {
        AllHasChivalriesDefs.MarkDirty();
        InstructorKnightsCount.MarkDirty();
        KnightsWithChivalryCount.MarkDirty();
        KnightsHasUnusedTraitSlot.MarkDirty();
        KnightsApproachingResignation.MarkDirty();
    }

    public void OnPawnsChanged()
    {

    }

    public void Notify_KnightVirtuesChanged()
    {
        KnightsHasUnusedTraitSlot.MarkDirty();
    }

    private IEnumerable<KeyValuePair<KnightChivalryDef, int>> RefreshKnightsWithChivalryCount()
    {
        if (!AnyResidentKnights)
            yield break;

        foreach (IGrouping<KnightChivalryDef, ResidentKnight> group in ResidentKnights.GroupBy(k => k.Chivalry))
        {
            if (group.Key is not null)
            {
                yield return new KeyValuePair<KnightChivalryDef, int>(group.Key, group.Count());
            }
        }
    }

    private IEnumerable<Pawn> RefreshKnightsHasUnusedTraitSlot()
    {
        if (!AnyResidentKnights)
            yield break;

        foreach (ResidentKnight record in ResidentKnights)
        {
            if (record.KnightVirtueHandler.HasUnusedTraitSlot)
            {
                yield return record.Pawn;
            }
        }
    }

    private IEnumerable<Pawn> RefreshKnightsApproachingResignation()
    {
        if (!AnyResidentKnights)
            yield break;

        int ticksGame = Find.TickManager.TicksGame;
        foreach (ResidentKnight record in ResidentKnights)
        {
            if (record.ResignationTick > 0)
            {
                float resignationDays = Mathf.Max(0f, (record.ResignationTick - ticksGame) / 60000f);
                if (resignationDays < 15f)
                {
                    yield return record.Pawn;
                }
            }
        }
    }
}