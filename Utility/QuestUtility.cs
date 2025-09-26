using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OberoniaAurea.RatkinOrder;

public static class OARO_QuestUtility
{
    public static void OnRatkinOrderRemoved(this QuestManager questManager, RatkinOrder order)
    {
        ConcurrentBag<IOnRatkinOrderRemoved> ratkinOrderRelateds = [];
        questManager.ActiveQuestsListForReading
            .AsParallel()
            .ForAll(quest =>
            {
                IEnumerable<IOnRatkinOrderRemoved> relatedParts = quest.PartsListForReading.OfType<IOnRatkinOrderRemoved>();
                foreach (IOnRatkinOrderRemoved relatedPartInner in relatedParts)
                {
                    ratkinOrderRelateds.Add(relatedPartInner);
                }
            });

        foreach (IOnRatkinOrderRemoved relatedPart in ratkinOrderRelateds)
        {
            relatedPart.Notify_RatkinOrderRemoved(order);
        }
    }

    public static void SetBasicOrderSlateVar(this Slate slate, RatkinOrder ratkinOrder)
    {
        slate.Set(KeyLibrary_SlateStoreAs.RatkinOrder, ratkinOrder);
        slate.Set(KeyLibrary_SlateStoreAs.OrderName, ratkinOrder.Name);
        slate.Set(KeyLibrary_SlateStoreAs.OrderFaction, ratkinOrder.Faction);

        slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFaction, ratkinOrder.Faction);
        slate.Set(KeyLibrary_SlateStoreAs.ParentRatkinFactionDef, ratkinOrder.Faction.def);
    }

    public static void SetBasicOrderSlateVar(this Slate slate, Branch branch)
    {
        slate.SetBasicOrderSlateVar(branch.RatkinOrder);

        slate.Set(KeyLibrary_SlateStoreAs.Branch, branch);
        slate.Set(KeyLibrary_SlateStoreAs.BranchName, branch.Name);
        slate.Set(KeyLibrary_SlateStoreAs.BranchSite, branch.WorldObject);
    }
}
