using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue_Tenacity_GeneralII : KnightVirtue, IPawnPreApplyDamage
{
    public int Priority => 50;

    public void PawnPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (Pawn.VerbTracker?.PrimaryVerb?.IsMeleeAttack ?? false)
        {
            if (!dinfo.Def.isRanged && !dinfo.Def.isExplosive)
                dinfo.SetAmount(dinfo.Amount * 0.5f);
        }
    }
}