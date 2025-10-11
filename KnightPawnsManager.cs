using OberoniaAurea_Frame;

namespace OberoniaAurea.RatkinOrder;

public class KnightPawnsManager
{
    public static KnightPawnsManager Instance { get; private set; }

    public KnightPawnsManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(Instance));
        Instance = this;
    }
    public static void ClearStaticCache() => Instance = null;
}
