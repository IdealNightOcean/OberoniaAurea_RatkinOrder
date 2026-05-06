using OberoniaAurea_Frame;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士单位管理器 - 负责管理所有被注册为骑士的单位及其对应的骑士记录
/// </summary>
public class KnightPawnsManager : IExposable
{
    public static KnightPawnsManager Instance { get; private set; }

    private List<KnightRecord> knights = new(32);
    private Dictionary<Pawn, KnightRecord> knightsDict = [];
    public IReadOnlyList<KnightRecord> AllKnights => knights;

    /// <summary>
    /// 骑士团 - 骑士记录的反向索引，方便根据骑士团快速获取对应的骑士列表
    /// </summary>
    private readonly Dictionary<RatkinOrder, List<KnightRecord>> orderToKnights = [];
    /// <summary>
    /// 骑士分部 - 骑士记录的反向索引，方便根据分部快速获取对应的骑士列表
    /// </summary>
    private readonly Dictionary<Branch, List<KnightRecord>> branchToKnights = [];

    public KnightPawnsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(KnightPawnsManager));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref knights, nameof(knights), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (knights.RemoveAll(k => k is null || k.Pawn.DestroyedOrNull() || !k.RatkinOrder.IsValid()) > 0)
            {
                Log.Error($"[OARO] {nameof(KnightPawnsManager)} 的部分骑士记录在加载后无效，已被移除。");
            }

            knightsDict = new(knights.Count);

            foreach (KnightRecord knightRecord in knights)
            {
                knightsDict.Add(knightRecord.Pawn, knightRecord);
                AddKnightToLookupDicts(knightRecord);
            }
        }
    }

    public bool RegisterKnight(Pawn pawn, KnightRecord knightRecord)
    {
        if (pawn is null || knightRecord == null)
            return false;

        if (!pawn.CanBeKnight())
        {
            Log.Error($"[OARO] 将单位 ({pawn}) 注册到 KnightPawnsManager 失败：该单位不能成为骑士。");
            return false;
        }
        if (knightsDict.ContainsKey(pawn))
        {
            Log.Error($"[OARO] 将单位 ({pawn}) 注册到 KnightPawnsManager 失败：该单位已经是骑士了。");
            return false;
        }
        knightRecord.BindPawn(pawn);
        knightsDict.Add(pawn, knightRecord);
        knights.Add(knightRecord);
        AddKnightToLookupDicts(knightRecord);

        return true;
    }

    public bool DeregisterKnight(Pawn pawn)
    {
        if (!knightsDict.TryGetValue(pawn, out KnightRecord record))
            return false;

        knightsDict.Remove(pawn);
        knights.Remove(record);
        if (record.RatkinOrder is not null && orderToKnights.TryGetValue(record.RatkinOrder, out List<KnightRecord> orderKnights))
        {
            orderKnights?.Remove(record);
        }
        if (record.Branch is not null && branchToKnights.TryGetValue(record.Branch, out List<KnightRecord> branchKnights))
        {
            branchKnights?.Remove(record);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKnight(Pawn pawn) => pawn.CanBeKnight() && knightsDict.ContainsKey(pawn);

    public KnightRecord GetKnightRecord(Pawn pawn)
    {
        if (knightsDict.TryGetValue(pawn, out KnightRecord record))
        {
            return record;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out KnightRecord record)
    {
        record = null;
        return pawn.CanBeKnight() && knightsDict.TryGetValue(pawn, out record);
    }

    private void AddKnightToLookupDicts(KnightRecord knightRecord)
    {
        if (knightRecord.RatkinOrder is not null)
        {
            if (orderToKnights.TryGetValue(knightRecord.RatkinOrder, out List<KnightRecord> orderKnights))
            {
                orderKnights.Add(knightRecord);
            }
            else
            {
                orderToKnights[knightRecord.RatkinOrder] = [knightRecord];
            }
        }
        if (knightRecord.Branch is not null)
        {
            if (branchToKnights.TryGetValue(knightRecord.Branch, out List<KnightRecord> branchKnights))
            {
                branchKnights.Add(knightRecord);
            }
            else
            {
                branchToKnights[knightRecord.Branch] = [knightRecord];
            }
        }
    }
}