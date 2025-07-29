using RimWorld;
using System;
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
                    RatkinOrder newOrder = GenerateRatkinOrderForFaction(faction);
                    if (newOrder is null)
                    {
                        continue; // Skip if order generation failed
                    }
                    RatkinOrderManager.Instance.AddRatkinOrder(newOrder);

                    BranchUtility.InitBranchForNewOrder(newOrder);
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

        return faction.def.HasModExtension<RatkinOrderFactionExtension>();
    }

    public static bool CanHaveNewRatkinOrder(Faction faction)
    {
        if (CanHaveNewRatkinOrder(faction) && RatkinOrderManager.Instance is not null)
        {
            return !RatkinOrderManager.Instance.IsFactionHasRatkinOrder(faction);
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

    public static RatkinOrder GenerateRatkinOrderForFaction(Faction faction, RatkinOrderDef forceOrderDef = null)
    {
        try
        {
            RatkinOrderDef def = forceOrderDef ?? faction.def.GetModExtension<RatkinOrderFactionExtension>().ratkinOrderDef;
            if (def is null)
            {
                Log.Error("Tried to create RatkinOrder for faction_" + faction.loadID + " but the faction has no RatkinOrderDef.");
                return null;
            }
            return new RatkinOrder(def, faction);
        }
        catch (Exception ex)
        {
            Log.Error("Could not create RatkinOrder for faction_" + faction.loadID + ": " + ex);
            return null;
        }
    }

}
