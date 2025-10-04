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
    public static string SignalCliqueAdded(Quest quest) => $"Quest{quest.id}.CliqueAdded";
    public static string SignalCliqueRemoved(Quest quest) => $"Quest{quest.id}.CliqueRemoved";
    public static string SignalCliqueActived(Quest quest) => $"Quest{quest.id}.CliqueActived";
    public static string SignalCliqueDeactived(Quest quest) => $"Quest{quest.id}.CliqueDeactived";

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
                if (clique.TicksToActive > 0 && (clique.TicksToActive -= 1000) <= 0)
                {
                    TryActiveClique(clique, directly: true);
                }
            }
        }
    }

    public bool HasClique(string cliqueKey) => allCliques?.ContainsKey(cliqueKey) ?? false;

    public bool TryGetClique(string cliqueKey, out QuestClique clique, bool showErrorIfMiss = true)
    {
        if (allCliques is not null && allCliques.TryGetValue(cliqueKey, out clique))
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

    public string GetCliqueName(string cliqueKey, bool showErrorIfMiss = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss))
        {
            return "UNKOWN";
        }
        return clique.Name;
    }

    public bool TryAddClique(QuestClique clique, bool replaceCur = false, bool defaultActive = false)
    {
        bool added = false;
        if (allCliques is null)
        {
            allCliques = new Dictionary<string, QuestClique> { { clique.Key, clique } };
            added = true;
        }
        else if (allCliques.TryGetValue(clique.Key, out QuestClique oldClique))
        {
            if (replaceCur)
            {
                if (oldClique.IsActive)
                {
                    DeactiveClique(oldClique);
                }
                allCliques[clique.Key] = clique;
                added = true;
            }
            else
            {
                Log.Warning($"Clique ({clique.Key}) already exists in quest.");
                added = false;
            }
        }
        else
        {
            allCliques[clique.Key] = clique;
            added = true;
        }

        if (added)
        {
            clique.IsActive = false;
            if (defaultActive)
            {
                TryActiveClique(clique, directly: true);
            }
            Find.SignalManager.SendSignal(new Signal(SignalCliqueAdded(quest), clique.Named("SUBJECT")));
            return true;
        }

        return false;
    }

    public void RemoveClique(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false) && allCliques.Remove(cliqueKey))
        {
            Find.SignalManager.SendSignal(new Signal(SignalCliqueRemoved(quest), clique.Named("SUBJECT")));
            if (clique.IsActive)
            {
                DeactiveClique(clique);
            }
        }
    }

    public bool IsCliqueActive(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            return clique.IsActive;
        }
        return false;
    }

    public bool CanActiveClique(string cliqueKey, bool directly = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            return false;
        }
        return CanActiveClique(clique, directly);
    }

    private bool CanActiveClique(QuestClique clique, bool directly = false)
    {
        if (!clique.IsActivatable || clique.IsActive)
        {
            return false;
        }
        if (directly)
        {
            return true;
        }
        if (clique.TicksToActive > 0)
        {
            return false;
        }
        if (clique.IsBranchClique && clique.RelatedBranch.Squad.SquadStat.Supply < 0.25f)
        {
            return false;
        }
        return clique.Willingness > 0.999f;
    }

    public bool TryActiveClique(string cliqueKey, bool directly = false, int activeDelayTicks = -1)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            return false;
        }
        return TryActiveClique(clique, directly, activeDelayTicks);
    }

    private bool TryActiveClique(QuestClique clique, bool directly = false, int activeDelayTicks = -1)
    {
        if (!CanActiveClique(clique, directly))
        {
            return false;
        }

        if (directly)
        {
            Active();
            return true;
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

        return true;

        void Active()
        {
            clique.IsActive = true;
            clique.TicksToActive = -1;
            totalPotency += clique.Potency;
            Find.SignalManager.SendSignal(new Signal(SignalCliqueActived(quest), clique.Named("SUBJECT")));
        }
    }

    public void DeactiveClique(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            if (clique.IsActive)
            {
                DeactiveClique(clique);
            }
        }
    }

    private void DeactiveClique(QuestClique clique)
    {
        clique.IsActive = false;
        totalPotency -= clique.Potency;
        Find.SignalManager.SendSignal(new Signal(SignalCliqueDeactived(quest), clique.Named("SUBJECT")));
    }

    public float GetCliquePotency(string cliqueKey, bool showErrorIfMiss = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss))
        {
            return clique.Potency;
        }
        return 0f;
    }

    public void AdjustCliquePotency(string cliqueKey, float change, bool showErrorIfMiss = true)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss))
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

    public float GetCliqueWillingness(string cliqueKey, bool showErrorIfMiss = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss))
        {
            return clique.IsActive ? 1f : clique.Willingness;
        }
        return 0f;
    }

    public void AdjustCliqueWillingness(string cliqueKey, float change, bool showMessage = true, bool showErrorIfMiss = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss))
        {
            clique.Willingness += change;
            if (showMessage)
            {
                if (change > 0f)
                {
                    Messages.Message("OARO_CliqueWillingness_Increase".Translate(clique.Name, change.ToString("F2")), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("OARO_CliqueWillingness_Decrease".Translate(clique.Name, (-change).ToString("F2")), MessageTypeDefOf.NegativeEvent);
                }
            }
        }
    }

    public bool CanBriberyClique(string cliqueKey, Map map)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            return false;
        }
        if (!clique.IsBribable || clique.IsActive)
        {
            return false;
        }

        if (map is null || !map.HasEnoughThingsOfDef(ThingDefOf.Silver, clique.BriberyCost))
        {
            return false;
        }

        return true;
    }

    public void BriberyClique(string cliqueKey, Map map)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            map.DestoryThingsOfDef(ThingDefOf.Silver, clique.BriberyCost);
            clique.Willingness += (1f - clique.Willingness);

            if (CanActiveClique(clique))
            {
                TryActiveClique(clique, directly: false);
            }
        }
    }

    public void TryCommunicateWithClique(string cliqueKey, Pawn negotiant)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            if (!clique.IsCommunicable)
            {
                Log.Error($"Trying to commiunicate with an incommunicable clique {clique.Name}.");
                return;
            }

            if (clique.PreferredBuilding is not null && branch.BuildingHandler.HasBuilding(clique.PreferredBuilding))
            {

            }

            if (CanActiveClique(clique))
            {
                TryActiveClique(clique, directly: false);
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