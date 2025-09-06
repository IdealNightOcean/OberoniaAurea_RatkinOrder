using OberoniaAurea_Frame;
using RimWorld;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class WorkGiver_FillFermentingBarrel : WorkGiver_ThingDefScanner
{
    private static string TemperatureTrans;
    private static string NoRawMaterialTrans;

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public static void ResetStaticData()
    {
        TemperatureTrans = "BadTemperature".Translate();
        NoRawMaterialTrans = "OARO_BarrelNoRawMaterial".Translate();
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_OrderFermentingBarrel { Fermented: false, SpaceLeftForRaw: > 0, AmbientTemperature: float ambientTemperature } building_FermentingBarrel)
        {
            return false;
        }
        CompProperties_TemperatureRuinable compProperties = building_FermentingBarrel.TemperatureRuinableComp.Props;
        if (ambientTemperature < compProperties.minSafeTemperature + 2f || ambientTemperature > compProperties.maxSafeTemperature - 2f)
        {
            JobFailReason.Is(TemperatureTrans);
            return false;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return false;
        }
        if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) is not null)
        {
            return false;
        }
        if (FindRawMaterial(pawn, building_FermentingBarrel) is null)
        {
            JobFailReason.Is(NoRawMaterialTrans);
            return false;
        }
        if (t.IsBurning())
        {
            return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Building_OrderFermentingBarrel barrel = (Building_OrderFermentingBarrel)t;
        Thing thing = FindRawMaterial(pawn, barrel);
        return JobMaker.MakeJob(WorkThingDefRequest.jobDef, t, thing);
    }

    private Thing FindRawMaterial(Pawn pawn, Building_OrderFermentingBarrel barrel)
    {
        return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(barrel.ModEx_FermentingBarrel.rawMaterial), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));
    }
}
