using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Bullet_Banner : Bullet
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (hitThing is Pawn pawn)
        {
            pawn.health.AddHediff(OARO_HediffDefOf.OARO_Hediff_BannerBullet);
            if (pawn.Spawned)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "OARO_Mote_BannerBulletMark".Translate(), 1.9f);
            }
        }

        base.Impact(hitThing, blockedByShield);
    }
}
