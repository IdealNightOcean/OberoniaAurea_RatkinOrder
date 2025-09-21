using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestPart_CliquesManager : QuestPartActivable, IOnBranchDestoryed
{
    private Dictionary<string, QuestClique> allCliques;

    public string InSignalOutPotency;
    public string OutSignalOutPotency;

    private float totalPotency;
    public float TotalPotency => totalPotency;

    private int ticksToNextCheck = 1000;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref allCliques, "allCliques", LookMode.Value, LookMode.Deep);

        Scribe_Values.Look(ref InSignalOutPotency, "InSignalOutPotency");
        Scribe_Values.Look(ref OutSignalOutPotency, "OutSignalOutPotency");

        Scribe_Values.Look(ref totalPotency, "totalPotency", 0f);
        Scribe_Values.Look(ref ticksToNextCheck, "ticksToNextCheck", 0);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            allCliques?.RemoveAll(kv => kv.Key is null || kv.Value is null);
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        InSignalOutPotency = null;
        OutSignalOutPotency = null;
        allCliques = null;
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag == InSignalOutPotency)
        {
            Find.SignalManager.SendSignal(new Signal(OutSignalOutPotency, totalPotency.Named("SUBJECT")));
        }
    }

    public override void QuestPartTick()
    {
        if (--ticksToNextCheck < 0)
        {
            ticksToNextCheck = 1000;
            if (allCliques is null)
            {
                return;
            }
            foreach (QuestClique clique in allCliques.Values)
            {
                if (!clique.IsActive && clique.TicksToActive > 0 && (clique.TicksToActive -= 1000) <= 0)
                {
                    ActiveClique(clique);
                }
            }
        }
    }

    public bool AddClique(string cliqueKey, QuestClique clique, bool replaceCur)
    {
        if (allCliques is null)
        {
            allCliques = new Dictionary<string, QuestClique> { { cliqueKey, clique } };
            return true;
        }
        if (replaceCur || !allCliques.ContainsKey(cliqueKey))
        {
            allCliques[cliqueKey] = clique;
            return true;
        }
        else
        {
            Log.Warning("Clique already exists in quest.");
            return false;
        }
    }

    public void RemoveClique(string cliqueKey)
    {
        if (allCliques is null)
        {
            return;
        }
        if (allCliques.TryGetValue(cliqueKey, out QuestClique clique))
        {
            allCliques.Remove(cliqueKey);
            if (clique.IsActive)
            {
                totalPotency -= clique.Potency;
            }
        }
    }

    public bool HasClique(string cliqueKey)
    {
        return allCliques?.ContainsKey(cliqueKey) ?? false;
    }

    public bool IsCliqueActive(string cliqueKey)
    {
        if (allCliques?.TryGetValue(cliqueKey, out QuestClique clique) ?? false)
        {
            return clique.IsActive;
        }
        return false;
    }

    public bool CanActiveClique(string cliqueKey)
    {
        if (allCliques is null || !allCliques.TryGetValue(cliqueKey, out QuestClique clique))
        {
            return false;
        }
        if (clique.IsActive || clique.TicksToActive > 0)
        {
            return false;
        }
        return clique.Willingness > 0.999f;
    }

    public bool ActiveClique(string cliqueKey, int activeDelayTicks = -1)
    {
        if (allCliques is null || !allCliques.TryGetValue(cliqueKey, out QuestClique clique))
        {
            Log.Error("No clique found in quest.");
            return false;
        }
        if (!clique.IsActive)
        {
            if (activeDelayTicks > 0)
            {
                clique.TicksToActive = activeDelayTicks;
            }
            else
            {
                ActiveClique(clique);
            }
        }
        return true;
    }

    private void ActiveClique(QuestClique clique)
    {
        clique.IsActive = true;
        clique.TicksToActive = -1;
        totalPotency += clique.Potency;
    }

    public bool DeactiveClique(string cliqueKey)
    {
        if (allCliques is null || !allCliques.TryGetValue(cliqueKey, out QuestClique clique))
        {
            Log.Error("No clique found in quest.");
            return false;
        }
        if (clique.IsActive)
        {
            clique.IsActive = false;
            totalPotency -= clique.Potency;
        }
        return true;
    }

    public void AdjustCliqueWillingness(string cliqueKey, float change)
    {
        if (allCliques?.TryGetValue(cliqueKey, out QuestClique clique) ?? false)
        {
            clique.Willingness += change;
        }
    }

    public void Notify_BranchDestoryed(Branch branch)
    {
        List<string> keysToRemove = allCliques?.Where(kv => kv.Value.RelatedBranch == branch).Select(kv => kv.Key).ToList();
        if (keysToRemove is not null)
        {
            foreach (string key in keysToRemove)
            {
                RemoveClique(key);
            }
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        List<string> keysToRemove = allCliques?.Where(kv => kv.Value.RelatedRatkinOrder == ratkinOrder).Select(kv => kv.Key).ToList();
        if (keysToRemove is not null)
        {
            foreach (string key in keysToRemove)
            {
                RemoveClique(key);
            }
        }
    }

    public static bool TryGetCliquesManager(Quest quest, bool addPartIfMiss, out QuestPart_CliquesManager questPart_CliquesManager)
    {
        questPart_CliquesManager = quest.PartsListForReading.OfType<QuestPart_CliquesManager>()?.FirstOrFallback(null);
        if (addPartIfMiss && questPart_CliquesManager is null)
        {
            questPart_CliquesManager = new QuestPart_CliquesManager();
            quest.AddPart(questPart_CliquesManager);
        }
        return questPart_CliquesManager is not null;
    }
}