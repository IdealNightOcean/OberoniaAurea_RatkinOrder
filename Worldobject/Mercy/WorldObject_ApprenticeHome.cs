using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_ApprenticeHome : WorldObject_Interactive_Nameable
{
    public Pawn Apprentice;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Apprentice, nameof(Apprentice));
    }

    public override void Notify_CaravanArrived(Caravan caravan)
    {

        List<Thing> things = [];
        Thing t = ThingMaker.MakeThing(ThingDefOf.Silver);
        t.stackCount = Rand.Range(300, 700);
        things.Add(t);

        t = ThingMaker.MakeThing(ThingDefOf.RawPotatoes);
        t.stackCount = Rand.Range(700, 1400);
        things.Add(t);

        t = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
        t.stackCount = Rand.Range(4, 6);
        things.Add(t);

        ThingDef clothDef = DefDatabase<ThingDef>.GetNamedSilentFail("RK_ApronSkirt");
        t = ThingMaker.MakeThing(clothDef, ThingDefOf.Cloth);
        t.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Good, ArtGenerationContext.Outsider);
        things.Add(t);
        t = ThingMaker.MakeThing(clothDef, ThingDefOf.Cloth);
        t.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Good, ArtGenerationContext.Outsider);
        things.Add(t);

        foreach (Thing item in things)
        {
            CaravanInventoryUtility.GiveThing(caravan, item);
        }

        Find.LetterStack.ReceiveLetter(label: "OARO_Apprentice_NoOnePickUpReasonLabel".Translate(),
                                       text: "OARO_Apprentice_NoOnePickUpReasonText".Translate(Apprentice.Named(KeyLibrary_FormatArgName.PAWN)),
                                       LetterDefOf.NegativeEvent,
                                       lookTargets: this);

        this.SendWorkResolvedSignal();
        this.SafeDestroy();
    }
}