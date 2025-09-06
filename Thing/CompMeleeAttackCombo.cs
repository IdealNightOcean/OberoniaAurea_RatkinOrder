using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;


public class CompProperties_MeleeAttackCombo : CompProperties
{
    public float comboChance;
    public int comboCooldown;

    [MustTranslate]
    public string comboText;

    public CompProperties_MeleeAttackCombo()
    {
        compClass = typeof(CompMeleeAttackCombo);
    }
}


public class CompMeleeAttackCombo : ThingComp
{
    public CompProperties_MeleeAttackCombo Props => (CompProperties_MeleeAttackCombo)props;

    [Unsaved] private int lastComboTick = -1;

    public virtual bool CanCombo(Thing victim, Pawn caster)
    {
        if (caster is null || !Rand.Chance(Props.comboChance) || Find.TickManager.TicksGame < lastComboTick + Props.comboCooldown)
        {
            return false;
        }
        return true;
    }

    public void TryCombo(Verb_MeleeAttackDamage_Combo verb, LocalTargetInfo target)
    {
        Pawn caster = verb.CasterPawn;
        if (CanCombo(target.Thing, caster))
        {
            lastComboTick = Find.TickManager.TicksGame;

            caster.stances?.SetStance(new Stance_Mobile());

            verb.Reset();
            verb.TryStartCastOn(target, surpriseAttack: true);
            if (caster.Spawned)
            {
                MoteMaker.ThrowText(caster.DrawPos, caster.Map, Props.comboText, 1.9f);
            }
        }
    }
}


