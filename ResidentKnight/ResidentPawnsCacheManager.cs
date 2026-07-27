using NightOcean;
using NightOcean.Collection;
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
    /// <summary>
    /// 所属的常驻人员管理器
    /// </summary>
    public ResidentPawnsManager Parent { get; }
    private IReadOnlyList<ResidentKnight> ResidentKnights => Parent.ResidentKnights;
    private bool AnyResidentKnights => ResidentKnights is not null && ResidentKnights.Count > 0;

    private bool chivalriesCacheDirty = false;

    private readonly HashSet<KnightChivalryDef> allHasChivalriesDefs = [];
    private readonly Dictionary<KnightChivalryDef, int> knightsWithChivalryCount = [];

    /// <summary>
    /// 全部常驻骑士持有的所有骑士精神
    /// </summary>
    public IReadOnlyCollection<KnightChivalryDef> AllHasChivalriesDefs
    {
        get
        {
            if (chivalriesCacheDirty)
                RefreshChivalriesCache();
            return allHasChivalriesDefs;
        }
    }
    /// <summary>
    /// 各骑士精神对应的持有骑士数量
    /// </summary>
    public IReadOnlyDictionary<KnightChivalryDef, int> KnightsWithChivalryCount
    {
        get
        {
            if (chivalriesCacheDirty)
                RefreshChivalriesCache();
            return knightsWithChivalryCount;
        }
    }

    /// <summary>
    /// 律令骑士数量
    /// </summary>
    public LazyMutable<int> InstructorKnightsCount { get; }

    /// <summary>
    /// 拥有未使用美德词条槽位的常驻骑士
    /// </summary>
    public LazyMutableCollection<List<Pawn>, Pawn> KnightsHasUnusedTraitSlot { get; }
    /// <summary>
    /// 即将离职职的常驻骑士
    /// </summary>
    public LazyMutableCollection<List<Pawn>, Pawn> KnightsApproachingResignation { get; }

    public ResidentPawnsCacheManager(ResidentPawnsManager parent)
    {
        Parent = parent;
        InstructorKnightsCount = new(refreshFunc: () => ResidentKnights.Where(r => r?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_Instructor).Count());
        KnightsHasUnusedTraitSlot = new(refreshFunc: RefreshKnightsHasUnusedTraitSlot);
        KnightsApproachingResignation = new(refreshFunc: RefreshKnightsApproachingResignation);
    }

    public void TickHour()
    {
        KnightsApproachingResignation.MarkDirty();
    }

    public void OnKnightsChanged()
    {
        chivalriesCacheDirty = true;
        InstructorKnightsCount.MarkDirty();

        KnightsHasUnusedTraitSlot.MarkDirty();
        KnightsApproachingResignation.MarkDirty();
    }

    public void OnPawnsChanged() { }

    private void RefreshChivalriesCache()
    {
        allHasChivalriesDefs.Clear();
        knightsWithChivalryCount.Clear();

        if (!AnyResidentKnights)
        {
            chivalriesCacheDirty = false;
            return;
        }

        foreach (ResidentKnight knight in ResidentKnights)
        {
            KnightChivalryDef chivalry = knight.Chivalry;
            if (chivalry is null)
                continue;

            allHasChivalriesDefs.Add(chivalry);

            if (!knightsWithChivalryCount.TryGetValue(chivalry, out int count))
                count = 0;

            knightsWithChivalryCount[chivalry] = count + 1;

        }

        chivalriesCacheDirty = false;
    }

    private IEnumerable<Pawn> RefreshKnightsHasUnusedTraitSlot()
    {
        if (!AnyResidentKnights)
            yield break;

        foreach (ResidentKnight record in ResidentKnights)
        {
            if (record.VirtueHandler.HasUnusedTraitSlot)
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