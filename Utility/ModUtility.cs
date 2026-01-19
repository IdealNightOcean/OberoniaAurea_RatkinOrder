using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;
using Verse.Grammar;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class ModUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SafeDestroy(this Thing thing)
    {
        if (thing is not null && !thing.Destroyed)
        {
            thing.Destroy();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SafeDestroy(this WorldObject worldObject)
    {
        if (worldObject is not null && !worldObject.Destroyed)
        {
            if (worldObject is MapParent mapParent && mapParent.HasMap)
            {
                mapParent.forceRemoveWorldObjectWhenMapRemoved = true;
            }
            else
            {
                worldObject.Destroy();
            }
        }
    }

    public static Faction GenerateSubRatkinFaction(FactionDef subFactionDef, FactionDef parentFactionDef = null, Faction parentFaction = null, bool addToManager = true)
    {
        if (parentFactionDef is null && parentFaction is not null)
            parentFactionDef = parentFaction.def;

        if (parentFactionDef is not null && parentFaction is null)
            parentFaction = Find.FactionManager.FirstFactionOfDef(parentFactionDef);

        FactionGeneratorParms parms = new(subFactionDef, default, hidden: true);
        if (ModsConfig.IdeologyActive)
            parms.ideoGenerationParms = parentFactionDef is null ? new IdeoGenerationParms(subFactionDef) : new IdeoGenerationParms(parentFactionDef);

        Faction faction;
        if (subFactionDef.permanentEnemy)
        {
            faction = FactionGenerator.NewGeneratedFaction(parms);
        }
        else
        {
            List<FactionRelation> factionRelations = new(Find.FactionManager.AllFactionsListForReading.Count);
            foreach (Faction otherFaction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!otherFaction.def.PermanentlyHostileTo(subFactionDef))
                {
                    factionRelations.Add(new FactionRelation
                    {
                        other = otherFaction,
                        kind = FactionRelationKind.Neutral
                    });
                }
            }
            faction = FactionGenerator.NewGeneratedFactionWithRelations(parms, factionRelations);
        }

        faction.temporary = true;

        if (ModsConfig.IdeologyActive && parentFaction?.ideos?.PrimaryIdeo is not null)
            faction.ideos?.SetPrimary(parentFaction.ideos.PrimaryIdeo);

        if (addToManager)
            Find.FactionManager.Add(faction);

        return faction;
    }

    public static bool TryMakePawnArrival(List<Pawn> pawns, IncidentParms arrivalParms, PawnsArrivalModeDef arrivalMode, bool joinPlayer = false, bool sendStandardLetter = true)
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
            Log.Error($"[OARO] 尝试让单位到达地图 {map} 但找不到合法的到达方式，当前方法：{pawnsArrivalModeDef.defName}");
            return false;
        }

        pawnsArrivalModeDef.Worker.TryResolveRaidSpawnCenter(arrivalParms);
        if (joinPlayer)
        {
            foreach (Pawn p in pawns)
            {
                OAFrame_PawnUtility.MakePawnJoinPlayer(p);
            }
        }
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

    public static IEnumerable<Rule> RulesForRatkinOrder(string prefix, RatkinOrder ratkinOrder)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix += "_";
        }
        if (!ratkinOrder.IsValid())
        {
            yield return new Rule_String(prefix + "name", "OARO_RatkinOrderUnaffiliated".Translate());
            yield break;
        }
        yield return new Rule_String(prefix + "name", ratkinOrder.NameColored);
    }

    public static IEnumerable<Rule> RulesForBranch(string prefix, Branch branch, bool alsoAddOrderRule)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix += "_";
        }
        if (branch is null)
        {
            yield return new Rule_String(prefix + "name", "OARO_BranchUnaffiliated".Translate());
            yield break;
        }
        RatkinOrder ratkinOrder = branch.RatkinOrder;
        if (alsoAddOrderRule)
        {
            foreach (Rule orderObjRule in RulesForRatkinOrder(prefix + KeyLibrary_FormatArgName.ORDER, ratkinOrder))
            {
                yield return orderObjRule;
            }
        }
        yield return new Rule_String(prefix + "name", branch.Name.Colorize(ratkinOrder.Color));
        foreach (Rule worldObjRule in GrammarUtility.RulesForWorldObject(prefix + "SITE", branch.BaseSite))
        {
            yield return worldObjRule;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogExceptionError(Exception ex, string errorDesc, string typeName, string methodName, bool needStackTrace = false)
    {
        if (needStackTrace)
        {
            Log.Error($"[OARO] 在 {typeName}.{methodName} 中执行 {errorDesc} 时发生异常。\n异常：\n{ex}");
        }
        else
        {
            Log.Error($"[OARO] 在 {typeName}.{methodName} 中执行 {errorDesc} 时发生异常。\n异常：\n{ex.Message}");
        }
    }
}