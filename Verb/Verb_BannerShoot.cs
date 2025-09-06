using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Verb_BannerShoot : Verb_Shoot
{
    [Unsaved] private int lastbannerShootTick = -1;
    [Unsaved] private bool bannerShoot = false;
    public override ThingDef Projectile => bannerShoot ? OARO_ThingDefOf.OARO_Bullet_BannerRifle : verbProps.defaultProjectile;

    protected override bool TryCastShot()
    {
        bannerShoot = Find.TickManager.TicksGame > lastbannerShootTick + 900;
        if (base.TryCastShot())
        {
            if (bannerShoot)
            {
                lastbannerShootTick = Find.TickManager.TicksGame;
                if (caster.Spawned)
                {
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "OARO_Mote_BannerShoot".Translate(), 1.9f);
                }
            }
            bannerShoot = false;
            return true;
        }
        bannerShoot = false;
        return false;
    }
}