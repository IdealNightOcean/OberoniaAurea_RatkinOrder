using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class QuestNode_CliquesManager : QuestNode
{
    public SlateRef<Branch> branch;
    protected override bool TestRunInt(Slate slate)
    {
        return true;
    }

    protected override void RunInt()
    {
        QuestPart_CliquesManager questPart_CliquesManager = new()
        {
            inSignalEnable = QuestGen.quest.InitiateSignal
        };
        questPart_CliquesManager.InitOrderBranch(branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.Branch));
        QuestGen.quest.AddPart(questPart_CliquesManager);
    }
}

public class QuestPart_CliquesManager : QuestPartActivable, ISingleBranchRelated
{
    private Branch branch;
    public Branch Branch => branch;

    private Dictionary<string, QuestClique> allCliques;

    public string InSignalOutPotency;
    public string OutSignalOutPotency;

    private float totalPotency;
    public float TotalPotency
    {
        get => totalPotency;
        private set => totalPotency = Mathf.Max(0f, value);
    }

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
                    ActiveClique(clique, directly: true);
                }
            }
        }
    }

    public bool HasClique(string cliqueKey)
    {
        return allCliques?.ContainsKey(cliqueKey) ?? false;
    }

    public bool TryGetClique(string cliqueKey, out QuestClique clique, bool showErrorIfMiss = true)
    {
        if (allCliques?.TryGetValue(cliqueKey, out clique) ?? false)
        {
            return true;
        }

        if (showErrorIfMiss)
        {
            Log.Error("No clique found in quest.");
        }
        clique = null;
        return false;
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
        if (allCliques?.TryGetValue(cliqueKey, out QuestClique clique) ?? false)
        {
            allCliques.Remove(cliqueKey);
            if (clique.IsActive)
            {
                totalPotency -= clique.Potency;
            }
        }
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
        return CanActiveClique(clique);
    }

    private bool CanActiveClique(QuestClique clique)
    {
        if (clique.IsActive || clique.TicksToActive > 0)
        {
            return false;
        }
        if (clique.IsBranchClique && clique.RelatedBranch.Squad.SquadStat.Supply < 0.25f)
        {
            return false;
        }
        return clique.Willingness > 0.999f;
    }

    public bool ActiveClique(string cliqueKey, bool directly, int activeDelayTicks = -1)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            ActiveClique(clique, directly, activeDelayTicks);
            return true;
        }
        return false;
    }

    private void ActiveClique(QuestClique clique, bool directly, int activeDelayTicks = -1)
    {
        if (clique.IsActive)
        {
            return;
        }

        if (directly)
        {
            Active();
            return;
        }

        int delayTicks = activeDelayTicks > 0 ? activeDelayTicks
                                              : clique.IsBranchClique ? Rand.RangeInclusive(120000, 240000)
                                                                      : -1;
        if (delayTicks > 0)
        {
            clique.TicksToActive = Mathf.Min(clique.TicksToActive, delayTicks);
        }
        else
        {
            if (clique.IsBranchClique)
            {
                clique.RelatedBranch.Squad.SquadStat.Supply -= 0.25f;
            }
            Active();
        }

        void Active()
        {
            clique.IsActive = true;
            clique.TicksToActive = -1;
            totalPotency += clique.Potency;
        }
    }

    public bool DeactiveClique(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique) && clique.IsActive)
        {
            clique.IsActive = false;
            totalPotency -= clique.Potency;
            return true;
        }

        return false;
    }

    public void AdjustCliquePotency(string cliqueKey, float change)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            if (clique.IsActive)
            {
                TotalPotency -= clique.Potency;
                clique.Potency += change;
                TotalPotency += clique.Potency;
            }
            else
            {
                clique.Potency += change;
            }
        }
    }

    public void AdjustCliqueWillingness(string cliqueKey, float change, bool record = true)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            clique.AdjustCliqueWillingness(change, record);
        }
    }

    public void BriberyClique(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique) && clique.IsBribable)
        {
            Map map = OARO_MapUtility.GetRationalPlayerHomeMap(forQuest: false, canBeSpace: true);
            if (map is null || !map.HasEnoughThingsOfDef(ThingDefOf.Silver, clique.BriberyCost))
            {

                return;
            }
            map.DestoryThingsOfDef(ThingDefOf.Silver, clique.BriberyCost);
            clique.AdjustCliqueWillingness(1f - clique.Willingness, record: true);

            if (CanActiveClique(clique))
            {
                ActiveClique(clique, directly: false);
            }
        }
    }

    public void CommunicateClique(string cliqueKey, Pawn negotiant)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique) && clique.IsCommunicable)
        {

            if (CanActiveClique(clique))
            {
                ActiveClique(clique, directly: false);
            }
        }
    }

    public void InitOrderBranch(Branch branch)
    {
        this.branch = branch;
    }

    public void Notify_BranchDestroyed(Branch branch)
    {
        if (this.branch == branch)
        {
            this.branch = null;
        }

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
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
        }

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
            questPart_CliquesManager = new QuestPart_CliquesManager
            {
                inSignalEnable = quest.InitiateSignal
            };
            quest.AddPart(questPart_CliquesManager);
        }
        return questPart_CliquesManager is not null;
    }

    public override void DoDebugWindowContents(Rect innerRect, ref float curY)
    {
        if (State == QuestPartState.Enabled)
        {
            Rect rect = new(innerRect.x, curY, 500f, 25f);
            if (Widgets.ButtonText(rect, "Show All Cliques"))
            {
                ShowAllCliques();
            }

            curY += rect.height + 4f;
        }
    }

    private void ShowAllCliques()
    {
        if (allCliques.NullOrEmpty())
        {
            Messages.Message("No cliques in this quest.", MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        StringBuilder sb = new();
        int i = 0;
        foreach (KeyValuePair<string, QuestClique> kv in allCliques)
        {
            QuestClique clique = kv.Value;
            sb.AppendInNewLine((i++).ToString());
            sb.AppendInNewLine($"Key: {kv.Key},  Name:{clique.Name},  IsActive:{clique.IsActive} ({clique.TicksToActive})");
            sb.AppendInNewLine($"Potency: {clique.Potency:F2},  Willingness:{clique.Willingness:F2}");
            sb.AppendInNewLine($"IsBribable: {clique.IsBribable},  IsCommunicable: {clique.IsCommunicable}");
            sb.AppendInNewLine($"IsBranchClique: {clique.IsBranchClique}, RelatedBranch: {clique.RelatedBranch?.Name ?? "NULL"}");
            sb.AppendInNewLine("------------");
        }
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(sb.ToTaggedString()));
    }
}