using RimWorld;
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
                Log.Error($"Failed to generate RatkinOrder for faction {faction.loadID}: {ex}");
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
        if (RatkinOrderManager.Instance is not null)
        {
            return !RatkinOrderManager.Instance.FactionHasRatkinOrder(faction);
        }
        return false;
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
                Log.Error("Tried to create RatkinOrder for faction_" + faction.loadID + " but the faction has no RatkinOrderDef.");
                return null;
            }
            ratkinOrder = new RatkinOrder(ratkinOrderDef, faction)
            {
                Name = GenerateRatkinOrderName(ratkinOrderDef)
            };
        }
        catch (Exception ex)
        {
            Log.Error("Could not create RatkinOrder for faction_" + faction.loadID + ": " + ex);
            return null;
        }

        try
        {
            BranchUtility.InitBranchForNewOrder(ratkinOrder);
        }
        catch (Exception ex)
        {
            Log.Error("Could not initialize RatkinOrder for faction_" + faction.loadID + ": " + ex);
            return null;
        }

        RatkinOrderManager.Instance.AddRatkinOrder(ratkinOrder);
        return ratkinOrder;
    }

    public static string GenerateRatkinOrderName(RatkinOrderDef def)
    {
        if (!def.fixedName.NullOrEmpty())
        {
            return def.fixedName;
        }

        return NameGenerator.GenerateName(def.nameMaker, RatkinOrderManager.Instance.AllRatkinOrders.Select(o => o.Name));
    }

}
