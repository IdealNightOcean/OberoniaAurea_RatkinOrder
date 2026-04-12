using OberoniaAurea_Frame;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class UniqueIDManager : IExposable
{
    private Dictionary<string, int> uniqueIDs = new(2)
        {
            { nameof(RatkinOrder), 0 },
            { nameof(Branch), 0 },
            { nameof(KnightRecord), 0 },
            { nameof(ResidentPawn), 0 },
            { nameof(ResidentKnight), 0 }
        };

    private bool wasLoaded;
    public static UniqueIDManager Instance { get; private set; }

    public UniqueIDManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;

    public static int GetUniqueID(string key)
    {
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (!Instance.wasLoaded)
            {
                Log.Warning("在 UniqueIDsManager 加载前的 LoadingVars 期间获取下一个唯一 ID。分配一个随机值。");
                return Rand.Int;
            }
        }
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            Log.Warning("在保存期间获取下一个唯一 ID。这可能导致错误。");
        }
        int result = Instance.uniqueIDs.TryGetValue(key, fallback: -1);
        if (result < 0)
        {
            Log.Warning("当前 ID 为负数。可能尝试获取不可引用对象类型的 ID。");
        }
        else
        {
            result++;
            if (result == int.MaxValue)
            {
                Log.Warning("下一个 ID 达到最大值。重置为 0。这可能导致错误。");
                result = 0;
            }
            Instance.uniqueIDs[key] = result;
        }

        return result;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref uniqueIDs, nameof(uniqueIDs), LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            wasLoaded = true;
        }
    }
}