using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompPropertiesShieldFightBack : CompProperties
{
    public int priority = 100;

    public bool ignoreCurAttack = true;
    public float baseFightBackChance;
    public int fightBackCooldown = 600;

    public DamageDef fightBackDamageDef = DamageDefOf.Blunt;
    public float fightBackDamageAmount = 10f;
    public float fightBackArmorPenetration = 0.1f;

    public bool causeStun;
    public FloatRange stunDurationRange = FloatRange.Zero;

    [MustTranslate]
    public string fightBackText = "Fight Back";

    public HediffDef fightBackHediff;

    public CompPropertiesShieldFightBack()
    {
        compClass = typeof(CompShieldFightBack);
    }

}

public class CompShieldFightBack : ThingComp, IPawnPreApplyDamage
{
    protected CompPropertiesShieldFightBack Props => (CompPropertiesShieldFightBack)props;

    [Unsaved] private int lastFightBackTick = -1;

    [Unsaved] private CompEquippable equippableComp;
    protected CompEquippable EquippableComp => equippableComp ??= parent.TryGetComp<CompEquippable>();

    protected Pawn parentPawn;
    private bool needRegisteredAfterLoad = false;

    public int Priority => Props.priority;

    public void PawnPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;

        if (parentPawn is null
            || dinfo.Def.isRanged
            || dinfo.Def.isExplosive
            || (Find.TickManager.TicksGame < lastFightBackTick + Props.fightBackCooldown)
            || dinfo.Instigator is not Pawn instigator
           )
        {
            return;
        }

        if (CanFightBack(instigator))
        {
            if (Props.ignoreCurAttack)
            {
                absorbed = true;
                dinfo.SetAmount(0f);
            }
            FightBack(instigator);
        }
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        Register(pawn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        Deregister(pawn);
    }

    private void Register(Pawn pawn)
    {
        if (parentPawn is not null && parentPawn != pawn)
        {
            parentPawn?.GetComp<CompPawnPreApplyDamageHandler>()?.DeregisterDamageProcessor(this);
        }

        if (pawn.GetComp<CompPawnPreApplyDamageHandler>()?.RegisterDamageProcessor(this) ?? false)
        {
            needRegisteredAfterLoad = true;
            parentPawn = pawn;
        }
    }

    private void Deregister(Pawn pawn)
    {
        if (parentPawn != pawn)
        {
            parentPawn?.GetComp<CompPawnPreApplyDamageHandler>()?.DeregisterDamageProcessor(this);
        }
        pawn.GetComp<CompPawnPreApplyDamageHandler>()?.DeregisterDamageProcessor(this);
        needRegisteredAfterLoad = false;
        parentPawn = null;
    }

    protected virtual bool CanFightBack(Pawn Instigator)
    {
        return Rand.Chance(Props.baseFightBackChance);
    }

    protected virtual void FightBack(Pawn instigator)
    {
        lastFightBackTick = Find.TickManager.TicksGame;

        if (Props.fightBackHediff is not null)
        {
            instigator.health.AddHediff(Props.fightBackHediff);
        }

        parentPawn.stances?.SetStance(new Stance_Mobile());
        instigator.TakeDamage(new DamageInfo(Props.fightBackDamageDef, Props.fightBackDamageAmount, Props.fightBackArmorPenetration, instigator: parentPawn, weapon: parent.def));
        if (Props.causeStun)
        {
            instigator.TakeDamage(new DamageInfo(DamageDefOf.Stun, Props.stunDurationRange.RandomInRange / 30f, instigator: parentPawn, weapon: parent.def));
        }

        if (parentPawn.Spawned)
        {
            MoteMaker.ThrowText(parentPawn.DrawPos, parentPawn.Map, Props.fightBackText, 1.9f);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref needRegisteredAfterLoad, "needRegisteredAfterLoad", defaultValue: false);
        Scribe_References.Look(ref parentPawn, "parentPawn");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (needRegisteredAfterLoad)
            {
                Register(parentPawn);
            }
        }
    }
}
