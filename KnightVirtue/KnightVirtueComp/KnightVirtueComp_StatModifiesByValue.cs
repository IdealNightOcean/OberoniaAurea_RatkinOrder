using Verse;

namespace OberoniaAurea.RatkinOrder;



public abstract class KnightVirtueComp_StatModifiesByValue : KnightVirtueComp
{
    public KnightVirtueCompProperties_StatModifiesByValue Props => (KnightVirtueCompProperties_StatModifiesByValue)props;

    protected abstract float GetValueForStat();

    public override void OnRefreshBuffStage(HediffStageModifierBuilder buffStageBuilder)
    {
        float value = GetValueForStat();
        if (!Props.statOffsetsByValue.NullOrEmpty())
        {
            foreach (StatModifierBySeverity offset in Props.statOffsetsByValue)
            {
                buffStageBuilder.AddOffset(offset.stat, offset.valueBySeverity.Evaluate(value));
            }

        }
        if (!Props.statFactorsByValue.NullOrEmpty())
        {
            foreach (StatModifierBySeverity factor in Props.statFactorsByValue)
            {
                buffStageBuilder.AddOffset(factor.stat, factor.valueBySeverity.Evaluate(value));
            }
        }
    }
}