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

    private List<Pawn> knightKeys;
    private List<KnightRecord> knightValues;

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
            if (knights.RemoveAll(kv => kv.Value is null || !kv.Value.RatkinOrder.IsValid()) > 0)
            {
                Log.Error($"[OARO] {nameof(KnightPawnsManager)} 的部分骑士记录在加载后无效，已被移除。");
            }
        }
    }

    public void RegisterKnight(Pawn pawn, KnightRecord knightRecord)
    {
        if (!pawn.CanBeKnight())
        {
            Log.Error($"[OARO] 将单位 ({pawn}) 注册到 KnightPawnsManager 失败：该单位不能是骑士。");
            return;
        }
        knights[pawn] = knightRecord;
    }

    public bool DeregisterKnight(Pawn pawn) => knights.Remove(pawn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKnight(Pawn pawn) => pawn.CanBeKnight() && knights.ContainsKey(pawn);

    public bool IsKnightCommander(Pawn pawn)
    {
        if (pawn.CanBeKnight() && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.IsCommander;
        }
        return false;
    }

    public KnightRecord GetKnightRecord(Pawn pawn)
    {
        if (knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record;
        }
        return null;
    }

    public bool TryGetKnightRecord(Pawn pawn, out KnightRecord record)
    {
        record = null;
        return pawn.CanBeKnight() && knights.TryGetValue(pawn, out record);
    }

    public RatkinOrder GetKnightOrder(Pawn pawn)
    {
        if (pawn.CanBeKnight() && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.RatkinOrder;
        }
        return null;
    }

    public Branch GetKnightBranch(Pawn pawn)
    {
        if (pawn.CanBeKnight() && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.Branch;
        }
        return null;
    }

    public bool IsKnightOfOrder(Pawn pawn, RatkinOrder ratkinOrder)
    {
        if (pawn.CanBeKnight() && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.RatkinOrder == ratkinOrder;
        }
        return false;
    }

    public bool IsKnightOfBranch(Pawn pawn, Branch branch)
    {
        if (pawn.CanBeKnight() && knights.TryGetValue(pawn, out KnightRecord record))
        {
            return record.Branch == branch;
        }
        return false;
    }
}