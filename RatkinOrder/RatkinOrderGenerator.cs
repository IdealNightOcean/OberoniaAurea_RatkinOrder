using RimWorld;
using RimWorld.Planet;
using System;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class RatkinOrderGenerator
{
    public static void StartNewGame()
    {
        foreach (Faction faction in Find.FactionManager.AllFactionsVisible)
        {
            try
            {
                if (CanHaveRatkinOrder(faction))
                {
                    RatkinOrderDef ratkinOrderDef = faction.def.GetModExtension<RatkinOrderFactionExtension>()?.ratkinOrderDef;
                    if (ratkinOrderDef is null)
                    {
                        continue;
                    }
                    GenerateRatkinOrderForFaction(faction, ratkinOrderDef);
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"generating RatkinOrder for faction {faction.loadID}",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(StartNewGame),
                    needStackTrace: true);
                continue;
            }
        }
    }

    public static bool CanHaveRatkinOrder(Faction faction)
    {
        if (faction is null || faction.temporary || faction.defeated)
        {
            return false;
        }

        return true;
    }

    public static bool CanHaveNewRatkinOrder(Faction faction)
    {
        if (faction is null || faction.temporary || faction.defeated)
        {
            return false;
        }
        return !RatkinOrderManager.FactionHasRatkinOrder(faction);
    }

    public static bool TryGenerateNewRatkinOrderForFaction(Faction faction, out RatkinOrder newOrder)
    {
        if (CanHaveNewRatkinOrder(faction))
        {
            newOrder = GenerateRatkinOrderForFaction(faction);
            return true;
        }
        else
        {
            newOrder = null;
            return false;
        }
    }

    public static RatkinOrder GenerateRatkinOrderForFaction(Faction faction, RatkinOrderDef ratkinOrderDef = null)
    {
        RatkinOrder ratkinOrder = null;
        try
        {
            ratkinOrderDef ??= faction.def.GetModExtension<RatkinOrderFactionExtension>().ratkinOrderDef;
            if (ratkinOrderDef is null)
            {
                Log.Error("[OARO] Tried to create RatkinOrder for faction_" + faction.loadID + " but the faction has no RatkinOrderDef.");
                return null;
            }
            ratkinOrder = new RatkinOrder(ratkinOrderDef, faction)
            {
                Name = GenerateRatkinOrderName(ratkinOrderDef)
            };
            ratkinOrder.PostGenerated();
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"generating RatkinOrder for faction_{faction.loadID}",
                typeName: nameof(RatkinOrderGenerator),
                methodName: nameof(GenerateRatkinOrderForFaction),
                needStackTrace: true);
            return null;
        }

        try
        {
            InitBranchForNewOrder(ratkinOrder);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: $"initializing RatkinOrder for faction_{faction.loadID}",
                typeName: nameof(RatkinOrderGenerator),
                methodName: nameof(GenerateRatkinOrderForFaction),
                needStackTrace: true);
            return null;
        }

        RatkinOrderManager.AddRatkinOrder(ratkinOrder);
        return ratkinOrder;
    }

    private static bool InitBranchForNewOrder(RatkinOrder ratkinOrder)
    {
        if (ratkinOrder is null || ratkinOrder.Faction is null)
        {
            return false;
        }

        bool atLeastOneSite = false;
        foreach (Settlement settlement in Find.WorldObjects.Settlements.Where(s => s.Faction == ratkinOrder.Faction))
        {
            if (Rand.Chance(0.4f))
            {
                continue;
            }
            try
            {
                if (Branch.GenerateBranchFor(ratkinOrder, settlement, addToManager: true) is not null)
                {
                    atLeastOneSite = true;
                }
            }
            catch (Exception ex)
            {
                ModUtility.LogExceptionError(ex,
                    errorDesc: $"Failed to generating a new branch for {ratkinOrder} at {settlement}",
                    typeName: nameof(RatkinOrderGenerator),
                    methodName: nameof(InitBranchForNewOrder),
                    needStackTrace: true);
                continue;
            }
        }
        return atLeastOneSite;
    }


    public static string GenerateRatkinOrderName(RatkinOrderDef def)
    {
        if (!string.IsNullOrEmpty(def.fixedName))
        {
            return def.fixedName;
        }

        return NameGenerator.GenerateName(def.nameMaker, RatkinOrderManager.AllRatkinOrders.Select(o => o.Name));
    }

}
