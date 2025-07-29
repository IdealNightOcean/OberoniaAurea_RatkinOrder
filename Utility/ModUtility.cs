using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;


[StaticConstructorOnStartup]
public static class ModUtility
{
    public static bool AnyThingOfDef(Room room, ThingDef thingDef)
    {
        List<Region> regions = room.Regions;
        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].ListerThings.AnyThingWithDef(thingDef))
            {
                return true;
            }
        }
        return false;
    }

    public static Faction GenerateSubRatkinFaction(FactionDef subFactionDef, FactionDef parentFactionDef = null, Faction parentFaction = null, bool addToManager = true)
    {
        if (parentFactionDef is null && parentFaction is not null)
        {
            parentFactionDef = parentFaction.def;
        }
        if (parentFactionDef is not null && parentFaction is null)
        {
            parentFaction = Find.FactionManager.FirstFactionOfDef(parentFactionDef);
        }

        FactionGeneratorParms parms = new(subFactionDef, default, hidden: true);
        if (ModsConfig.IdeologyActive)
        {
            parms.ideoGenerationParms = parentFactionDef is null ? new IdeoGenerationParms(subFactionDef) : new IdeoGenerationParms(parentFactionDef);
        }
        Faction faction = FactionGenerator.NewGeneratedFaction(parms);
        faction.temporary = true;
        if (ModsConfig.IdeologyActive && parentFaction?.ideos?.PrimaryIdeo is not null)
        {
            faction.ideos?.SetPrimary(parentFaction.ideos.PrimaryIdeo);
        }
        if (addToManager)
        {
            Find.FactionManager.Add(faction);
        }
        return faction;
    }

    public static void OnRatkinOrderRemoved(this QuestManager questManager, RatkinOrder order)
    {
        ConcurrentBag<IRatkinOrderRelated> ratkinOrderRelateds = [];
        questManager.ActiveQuestsListForReading
            .AsParallel()
            .ForAll(quest =>
            {
                IEnumerable<IRatkinOrderRelated> relatedParts = quest.PartsListForReading.OfType<IRatkinOrderRelated>();
                foreach (IRatkinOrderRelated relatedPartInner in relatedParts)
                {
                    ratkinOrderRelateds.Add(relatedPartInner);
                }
            });

        foreach (IRatkinOrderRelated relatedPart in ratkinOrderRelateds)
        {
            relatedPart.Notify_RatkinOrderRemoved(order);
        }
    }

    public static bool TryMakePawnArrival(List<Pawn> pawns, IncidentParms arrivalParms, PawnsArrivalModeDef arrivalMode, bool sendStandardLetter = true)
    {
        Map map = (Map)arrivalParms.target;
        PawnsArrivalModeDef pawnsArrivalModeDef = arrivalMode ?? PawnsArrivalModeDefOf.EdgeWalkIn;
        if (!pawnsArrivalModeDef.Worker.CanUseOnMap(map))
        {
            foreach (PawnsArrivalModeDef backupMode in DefDatabase<PawnsArrivalModeDef>.AllDefsListForReading.InRandomOrder())
            {
                if (backupMode.canBeBackup && backupMode.Worker.CanUseOnMap(map))
                {
                    pawnsArrivalModeDef = backupMode;
                    break;
                }
            }
        }

        if (!pawnsArrivalModeDef.Worker.CanUseOnMap(map))
        {
            Log.Error($"Tried to do pawns arrive on map {map} but could not find a legal arrival mode, current method: {pawnsArrivalModeDef.defName}");
            return false;
        }

        pawnsArrivalModeDef.Worker.TryResolveRaidSpawnCenter(arrivalParms);
        pawnsArrivalModeDef.Worker.Arrive(pawns, arrivalParms);
        if (sendStandardLetter)
        {
            TaggedString letterLabel = arrivalParms.customLetterLabel ?? "LetterLabelPawnsArrive".Translate();
            TaggedString letterText = arrivalParms.customLetterText ?? "LetterPawnsArrive".Translate(GenLabel.ThingsLabel(pawns.Cast<Thing>()));
            Find.LetterStack.ReceiveLetter(label: letterLabel,
                text: letterText,
                textLetterDef: arrivalParms.customLetterDef ?? LetterDefOf.PositiveEvent,
                pawns,
                arrivalParms.faction,
                arrivalParms.quest);
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref letterLabel, ref letterText, "LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), informEvenIfSeenBefore: true);
        }
        return true;
    }
}