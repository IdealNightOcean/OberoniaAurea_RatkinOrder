using OberoniaAurea_Frame;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_GiveHediffInRange : KnightVirtueComp
{
    public abstract bool HasExtraPawnValiator { get; }
    protected virtual bool ExtraPawnValiator(Pawn target) => true;

    protected RangeHediffGiver hediffGiver;
    public RangeHediffGiver HediffGiver
    {
        get
        {
            if (hediffGiver is null)
            {
                ModExtension_RangeHediffGive rangeHediffGiveEx = Def.GetModExtension<ModExtension_RangeHediffGive>();
                if (rangeHediffGiveEx is null)
                    return null;
                RangeHediffGiver giver = (RangeHediffGiver)Activator.CreateInstance(rangeHediffGiveEx.giverClass);

                giver.LinkedThing = this.Pawn;
                giver.SetGiveParams(rangeHediffGiveEx.giveParams.ShallowCopy());
                if (HasExtraPawnValiator)
                    giver.ExtraTargetValiator = ExtraPawnValiator;

                hediffGiver = giver;
            }

            return hediffGiver;
        }
    }
}
