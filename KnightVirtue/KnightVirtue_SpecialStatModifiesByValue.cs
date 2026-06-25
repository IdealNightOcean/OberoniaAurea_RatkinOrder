using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtue_SpecialStatModifiesByValue : KnightVirtue
{
    protected ModExtension_StatModifiersByValue statModifiersByValueEx;

    protected abstract float ValueForStat { get; }

    public override void SpecialStatModifies(HediffStageModifierBuilder buffStageTemplate)
    {
        statModifiersByValueEx ??= Def.GetModExtension<ModExtension_StatModifiersByValue>();
        if (statModifiersByValueEx is null)
            return;
        float value = ValueForStat;
        if (!statModifiersByValueEx.statOffsetsByValue.NullOrEmpty())
        {
            foreach (StatModifierBySeverity offset in statModifiersByValueEx.statOffsetsByValue)
            {
                buffStageTemplate.AddOffset(offset.stat, offset.valueBySeverity.Evaluate(value));
            }

        }
        if (!statModifiersByValueEx.statFactorsByValue.NullOrEmpty())
        {
            foreach (StatModifierBySeverity factor in statModifiersByValueEx.statFactorsByValue)
            {
                buffStageTemplate.AddOffset(factor.stat, factor.valueBySeverity.Evaluate(value));
            }
        }
    }
}