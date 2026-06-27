using OberoniaAurea_Frame;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public abstract class KnightVirtueComp_GiveHediffInRange : KnightVirtueComp
{
    public KnightVirtueCompProperties_GiveHediffInRange Props => (KnightVirtueCompProperties_GiveHediffInRange)props;

    public abstract bool HasExtraPawnValiator { get; }
    protected virtual bool ExtraPawnValiator(Pawn target) => true;

    protected RangeHediffGiver hediffGiver;
    public RangeHediffGiver HediffGiver
    {
        get
        {
            if (hediffGiver is null)
            {
                RangeHediffGiver giver = (RangeHediffGiver)Activator.CreateInstance(Props.giverClass);

                giver.LinkedThing = this.Pawn;
                giver.SetGiveParams(Props.giveParams.ShallowCopy());
                if (HasExtraPawnValiator)
                    giver.ExtraTargetValiator = ExtraPawnValiator;

                hediffGiver = giver;
            }

            return hediffGiver;
        }
    }
}
