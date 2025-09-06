using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Verb_MeleeAttackDamage_Combo : Verb_MeleeAttackDamage
{
    [Unsaved] private CompMeleeAttackCombo comboComb;
    public CompMeleeAttackCombo ComboComb => comboComb ??= EquipmentSource.GetComp<CompMeleeAttackCombo>();

    protected override bool TryCastShot()
    {
        if (base.TryCastShot())
        {
            ComboComb?.TryCombo(this, currentTarget);
            return true;
        }
        else
        {
            return false;
        }
    }
}
