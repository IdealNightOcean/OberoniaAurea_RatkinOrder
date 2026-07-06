using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompProperties_Disappears : CompProperties
{
    public IntRange disappearsAfterTicks;
    public bool showRemainingTime = true;
    public bool canUseDecimalsShortForm;

    public bool allowStack;

    [MustTranslate]
    public string messageOnDisappear;

    public MessageTypeDef disappearMessageType;

    public CompProperties_Disappears()
    {
        compClass = typeof(CompDisappears);
    }
}

public class CompDisappears : ThingComp
{
    private int disappearsTick;

    private CompProperties_Disappears Props => (CompProperties_Disappears)props;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref disappearsTick, "disappearsTick", 0);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        disappearsTick = Find.TickManager.TicksGame + Props.disappearsAfterTicks.RandomInRange;
    }

    public override string CompInspectStringExtra()
    {
        if (!Props.showRemainingTime)
        {
            return null;
        }
        int ticksToDisappears = disappearsTick - Find.TickManager.TicksGame;
        if (ticksToDisappears < 2500)
        {
            return "OARO_ThingDisappearAfter".Translate() + ": " + ticksToDisappears.ToStringSecondsFromTicks("F0");
        }
        return "OARO_ThingDisappearAfter".Translate() + ": " + ticksToDisappears.ToStringTicksToPeriod(allowSeconds: true, shortForm: true, canUseDecimals: true, allowYears: true, Props.canUseDecimalsShortForm);
    }

    public override void CompTickInterval(int delta)
    {
        if (Find.TickManager.TicksGame > disappearsTick)
        {
            parent.Destroy();
        }
    }

    public override void CompTickRare()
    {
        if (Find.TickManager.TicksGame > disappearsTick)
        {
            parent.Destroy();
        }
    }

    public override void CompTickLong()
    {
        if (Find.TickManager.TicksGame > disappearsTick)
        {
            parent.Destroy();
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (!String.IsNullOrEmpty(Props.messageOnDisappear))
        {
            Messages.Message(Props.messageOnDisappear.Formatted(parent.Named(KeyLibrary_FormatArgName.THING)), Props.disappearMessageType ?? MessageTypeDefOf.NeutralEvent);
        }
    }

    public override bool AllowStackWith(Thing other) => Props.allowStack && base.AllowStackWith(other);

    public override void PreAbsorbStack(Thing otherStack, int count)
    {
        int otherDisappearsTick = otherStack.TryGetComp<CompDisappears>()?.disappearsTick ?? -1;
        if (otherDisappearsTick > 0)
        {
            float newDisappearTick = (disappearsTick * parent.stackCount + otherDisappearsTick * count) / (float)(parent.stackCount + count);
            disappearsTick = Mathf.Max(1, Mathf.RoundToInt(newDisappearTick));
        }
    }
}