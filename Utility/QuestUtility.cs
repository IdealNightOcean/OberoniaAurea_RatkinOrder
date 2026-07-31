using OberoniaAurea.RatkinOrder.DataLibrary;
using OberoniaAurea_Frame.DataLibrary;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder.Utility;

public static class OARO_QuestUtility
{
    public static void SendSignalSafeSilent(this SignalManager signalManager, Signal signal)
    {
        if (String.IsNullOrEmpty(signal.tag))
        {
            return;
        }
        signalManager.SendSignal(signal);
    }

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
        slate.Set(OARO_KeyLibrary_SlateStoreAs.ratkinOrder, ratkinOrder);
        slate.Set(OARO_KeyLibrary_SlateStoreAs.orderName, ratkinOrder.Name);
        slate.Set(OARO_KeyLibrary_SlateStoreAs.orderFaction, ratkinOrder.Faction);
    }

    public static void SetBasicBranchSlateVar(this Slate slate, Branch branch, bool alsoSetOrder = true)
    {
        if (alsoSetOrder)
        {
            slate.SetBasicOrderSlateVar(branch.RatkinOrder);
        }
        slate.Set(OARO_KeyLibrary_SlateStoreAs.branch, branch);
        slate.Set(OARO_KeyLibrary_SlateStoreAs.branchName, branch.Name);
        slate.Set(OARO_KeyLibrary_SlateStoreAs.branchSite, branch.BaseSite);
    }

    public static void SendWorkResolvedSignal(this WorldObject worldObject, NamedArgument[] args = null)
    {
        if (args is null)
        {
            QuestUtility.SendQuestTargetSignals(worldObject.questTags, "WorkResolved", worldObject.Named(KeyLibrary_FormatArgName.SUBJECT));
        }
        else
        {
            NamedArgument[] extendedArgs = new NamedArgument[args.Length + 1];
            extendedArgs[0] = worldObject.Named(KeyLibrary_FormatArgName.SUBJECT);
            Array.Copy(args, 0, extendedArgs, 1, args.Length);
            QuestUtility.SendQuestTargetSignals(worldObject.questTags, "WorkResolved", extendedArgs);
        }
    }

    public static bool TryGetMercyQuestWatcher(this Quest quest, out QuestPart_MercyQuestWatcher watcher)
    {
        watcher = quest?.PartsListForReading.OfType<QuestPart_MercyQuestWatcher>()?.FirstOrFallback(null);
        return watcher is not null;
    }

    public static bool TryGetCliquesManager(this Quest quest, bool addPartIfMiss, out QuestPart_CliquesManager questPart_CliquesManager)
    {
        questPart_CliquesManager = quest?.PartsListForReading.OfType<QuestPart_CliquesManager>()?.FirstOrFallback(null);
        if (addPartIfMiss && questPart_CliquesManager is null)
        {
            questPart_CliquesManager = new QuestPart_CliquesManager
            {
                inSignalEnable = quest.InitiateSignal
            };
            quest.AddPart(questPart_CliquesManager);
        }
        return questPart_CliquesManager is not null;
    }

    public static bool TryGetEffectTagsPart(this Quest quest, bool addPartIfMiss, out QuestPart_EffectTags questPart_EffectTags)
    {
        questPart_EffectTags = quest.PartsListForReading.OfType<QuestPart_EffectTags>()?.FirstOrFallback(null);
        if (addPartIfMiss && questPart_EffectTags is null)
        {
            questPart_EffectTags = new QuestPart_EffectTags();
            quest.AddPart(questPart_EffectTags);
        }
        return questPart_EffectTags is not null;
    }

    public static bool TryGetBranchDemandWatcher(this Quest quest, out QuestPart_BranchDemandWatcher watcher)
    {
        watcher = quest?.PartsListForReading.OfType<QuestPart_BranchDemandWatcher>()?.FirstOrFallback(null);
        return watcher is not null;
    }

}