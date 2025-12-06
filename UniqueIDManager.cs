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
            { nameof(ResidentKnightRecord), 0 }
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
                Log.Warning("Getting next unique ID during LoadingVars before UniqueIDsManager was loaded. Assigning a random value.");
                return Rand.Int;
            }
        }
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            Log.Warning("Getting next unique ID during saving This may cause bugs.");
        }
        int result = Instance.uniqueIDs.TryGetValue(key, fallback: -1);
        if (result < 0)
        {
            Log.Warning("Current ID is Negative. May try get ID for non-referencable object type.");
        }
        else
        {
            result++;
            if (result == int.MaxValue)
            {
                Log.Warning("Next ID is at max value. Resetting to 0. This may cause bugs.");
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