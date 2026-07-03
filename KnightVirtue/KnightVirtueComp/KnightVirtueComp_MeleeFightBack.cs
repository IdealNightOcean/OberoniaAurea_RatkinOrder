using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtueCompProperties_MeleeFightBack : KnightVirtueCompProperties
{
    public int priority = 100;

    public bool ignoreCurAttack = true;
    public float baseFightBackChance;
    public int fightBackCooldown = 600;

    [MustTranslate]
    public string fightBackText = "Fight Back";

    public HediffDef fightBackHediff;

    public bool causeStun;
    public FloatRange stunDurationRange = FloatRange.Zero;

    public KnightVirtueCompProperties_MeleeFightBack()
    {
        compClass = typeof(KnightVirtueComp_MeleeFightBack);
    }

}


public class KnightVirtueComp_MeleeFightBack : KnightVirtueComp, IPawnPreApplyDamage
{
    protected KnightVirtueCompProperties_MeleeFightBack Props => (KnightVirtueCompProperties_MeleeFightBack)props;
    public int Priority => Props.priority;

    protected int lastFightBackTick = -1;

    public void PawnPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (!Rand.Chance(Props.baseFightBackChance))
            return;

        if (this.Pawn is null
            || dinfo.Def.isRanged
            || dinfo.Def.isExplosive
            || (Props.fightBackCooldown > 0 && (Find.TickManager.TicksGame < lastFightBackTick + Props.fightBackCooldown))
            || dinfo.Instigator is not Pawn instigator
           )
        {
            return;
        }

        if (this.Pawn.verbTracker?.PrimaryVerb?.IsMeleeAttack ?? false)
        {
            if (Props.ignoreCurAttack)
            {
                absorbed = true;
                dinfo.SetAmount(0f);
            }
            FightBack(instigator);
        }
    }

    protected void FightBack(Pawn instigator)
    {
        Verb verbToUse = this.Pawn.verbTracker?.PrimaryVerb;
        if (verbToUse is null)
            return;

        this.Pawn.stances?.SetStance(new Stance_Mobile());
        verbToUse.Reset();
        verbToUse.TryStartCastOn(instigator, surpriseAttack: true);
        if (Props.causeStun)
        {
            instigator.TakeDamage(new DamageInfo(def: DamageDefOf.Stun,
                                                 amount: Props.stunDurationRange.RandomInRange / 30f,
                                                 instigator: this.Pawn,
                                                 weapon: verbToUse.EquipmentSource?.def));
        }

        if (this.Pawn.Spawned)
        {
            MoteMaker.ThrowText(this.Pawn.DrawPos, this.Pawn.Map, Props.fightBackText, 1.9f);
        }
    }

    public override void PostActive()
    {
        base.PostActive();
        this.Pawn.RegisterPawnPreApplyDamageHandler(this);
    }

    public override void PostRemove()
    {
        base.PostRemove();
        this.Pawn.DeregisterPawnPreApplyDamageHandler(this);
    }
}
