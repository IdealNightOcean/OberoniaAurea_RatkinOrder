using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompProperties_MeleeFightBack : CompProperties
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

    public CompProperties_MeleeFightBack()
    {
        compClass = typeof(CompMeleeFightBack);
    }

}

public class CompMeleeFightBack : ThingComp, IPawnPreApplyDamage
{
    protected CompProperties_MeleeFightBack Props => (CompProperties_MeleeFightBack)props;

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
            || (Props.fightBackCooldown > 0 && (Find.TickManager.TicksGame < lastFightBackTick + Props.fightBackCooldown))
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
            parentPawn.DeregisterPawnPreApplyDamageHandler(this);
        }

        if (pawn.RegisterPawnPreApplyDamageHandler(this))
        {
            needRegisteredAfterLoad = true;
            parentPawn = pawn;
        }
    }

    private void Deregister(Pawn pawn)
    {
        if (parentPawn != pawn)
        {
            parentPawn.DeregisterPawnPreApplyDamageHandler(this);
        }
        pawn.DeregisterPawnPreApplyDamageHandler(this);
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

        Verb verbToUse = EquippableComp?.PrimaryVerb;
        if (verbToUse is null)
        {
            return;
        }

        parentPawn.stances?.SetStance(new Stance_Mobile());
        verbToUse.Reset();
        verbToUse.TryStartCastOn(instigator, surpriseAttack: true);
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
        Scribe_Values.Look(ref needRegisteredAfterLoad, nameof(needRegisteredAfterLoad), defaultValue: false);
        Scribe_References.Look(ref parentPawn, nameof(parentPawn));

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (needRegisteredAfterLoad)
            {
                Register(parentPawn);
            }
        }
    }
}
