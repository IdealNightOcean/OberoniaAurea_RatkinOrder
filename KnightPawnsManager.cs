using OberoniaAurea_Frame;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightPawnsManager : IExposable
{
    public static KnightPawnsManager Instance { get; private set; }

    private Dictionary<Pawn, KnightRecord> knights = new(32);
    public IReadOnlyDictionary<Pawn, KnightRecord> AllKnights => knights;

    private readonly Dictionary<RatkinOrder, List<KnightRecord>> orderToKnights = [];
    private readonly Dictionary<Branch, List<KnightRecord>> branchToKnights = [];

    public KnightPawnsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(KnightPawnsManager));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref knights, nameof(knights), LookMode.Reference, LookMode.Deep, ref knightKeys, ref knightValues);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (knights.RemoveAll(kv => kv.Value is null || kv.Value.Pawn.DestroyedOrNull() || !kv.Value.RatkinOrder.IsValid()) > 0)
            {
                Log.Error($"[OARO] {nameof(KnightPawnsManager)} 的部分骑士记录在加载后无效，已被移除。");
            }

            foreach (KnightRecord knightRecord in knights.Values)
            {
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

        knightRecord.BindPawn(pawn);
        knights[knightRecord.Pawn] = knightRecord;
        AddKnightToLookupDicts(knightRecord);

        return true;
    }

    public bool DeregisterKnight(Pawn pawn)
    {
        if (!knights.TryGetValue(pawn, out KnightRecord record))
            return false;

        knights.Remove(pawn);
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
    public bool IsKnight(Pawn pawn) => pawn.CanBeKnight() && knights.ContainsKey(pawn);

    public KnightRecord GetKnightRecord(Pawn pawn)
    {
        if (knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightRecord(Pawn pawn, out KnightRecord record)
    {
        record = null;
        return pawn.CanBeKnight() && knights.TryGetValue(pawn, out record);
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

    private List<Pawn> knightKeys;
    private List<KnightRecord> knightValues;
}