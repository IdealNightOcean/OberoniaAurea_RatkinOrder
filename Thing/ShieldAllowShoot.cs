using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ShieldAllowShoot : NewRatkin.Shield
{
    public override bool AllowVerbCast(Verb verb)
    {
        return true;
    }
}