using NightOcean;
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
            inSignalEnable = QuestGen.slate.Get<string>(KeyLibrary_SlateStoreAs.inSignal),
        };
        questPart_CliquesManager.SetOrderBranch(branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(KeyLibrary_SlateStoreAs.branch));
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
    public IReadOnlyDictionary<string, QuestClique> AllCliques => allCliques;

    public string InSignalOutPotency;
    public string OutSignalOutPotency;

    public LazyMutable<float> TotalPotency { get; }

    private int ticksToNextCheck = 1000;

    public QuestPart_CliquesManager()
    {
        TotalPotency = new(refreshFunc: () => allCliques?.Values.Sum(c => c.Potency) ?? 0f);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref allCliques, "allCliques", LookMode.Value, LookMode.Deep);

        Scribe_Values.Look(ref InSignalOutPotency, "InSignalOutPotency");
        Scribe_Values.Look(ref OutSignalOutPotency, "OutSignalOutPotency");

        Scribe_Values.Look(ref ticksToNextCheck, "ticksToNextCheck", 0);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (allCliques is not null)
            {
                allCliques.RemoveAll(kv => kv.Value is null);
                foreach (QuestClique clique in allCliques.Values)
                {
                    clique.CliquesManager = this;
                }
            }
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
            Find.SignalManager.SendSignal(new Signal(OutSignalOutPotency, TotalPotency.Value.Named(KeyLibrary_FormatArgName.SUBJECT)));
        }
    }

    protected override void Enable(SignalArgs receivedArgs)
    {
        base.Enable(receivedArgs);
        if (allCliques.NullOrEmpty())
        {
            return;
        }

        StringBuilder branchCliqueInfoSB = new("OARO_BranchCliquesInfoText_Header".Translate(branch.RatkinOrder.Name.Named(KeyLibrary_FormatArgName.OrderName)));
        branchCliqueInfoSB.AppendLine();
        foreach (Branch cliqueBranch in allCliques.Values.Where(c => c.IsBranchClique).Select(c => c.RelatedBranch).OrderBy(b => b?.RatkinOrder.LoadID ?? int.MinValue))
        {
            branchCliqueInfoSB.AppendLine($"{cliqueBranch.RatkinOrder.Name} - {cliqueBranch.Name}".Colorize(cliqueBranch.IsBranchOfType(Branch.BranchType.Friendly) ? Color.green : Color.white));
        }

        OrderLetterUtility.ReceiveLetter(
            label: "OARO_BranchCliquesInfoLabel".Translate(quest.name.Named("QuestName")),
            text: branchCliqueInfoSB.ToTaggedString(),
            def: OrderLetterDefOf.OARO_OfficialLetter,
            relatedOrder: branch.RatkinOrder,
            sender: branch.Name,
            relatedLetterType: OrderLetter.RelatedLetterType.Positive);
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
                    clique.TryActive(directly: true);
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
            Log.Error($"[OARO] 在quest_{quest.id}中未找到派别：{cliqueKey}。");
        }
        clique = null;
        return false;
    }

    public string GetCliqueName(string cliqueKey, bool showErrorIfMiss = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss))
        {
            return clique.Name;
        }
        return "UNKOWN";
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
                    oldClique.Deactive();
                }
                allCliques[clique.Key] = clique;
                added = true;
            }
            else
            {
                Log.Warning($"派别 ({clique.Key}) 已存在于任务中。");
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
            clique.CliquesManager = this;

            clique.IsActive = false;
            if (defaultActive)
            {
                clique.TryActive(directly: true);
            }
            Find.SignalManager.SendSignal(new Signal(SignalCliqueAdded(quest), clique.Named(KeyLibrary_FormatArgName.SUBJECT)));
            return true;
        }

        return false;
    }

    public void RemoveClique(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false) && allCliques.Remove(cliqueKey))
        {
            Find.SignalManager.SendSignal(new Signal(SignalCliqueRemoved(quest), clique.Named(KeyLibrary_FormatArgName.SUBJECT)));
            if (clique.IsActive)
            {
                clique.Deactive();
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
            return clique.CanActiveNow(directly: directly, resultOnly: true);
        }
        return false;
    }

    public bool TryActiveClique(string cliqueKey, bool directly = false, Map map = null, int activeDelayTicks = -1)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            return clique.TryActive(directly: directly, map: map, activeDelayTicks: activeDelayTicks);
        }
        return false;
    }

    public void DeactiveClique(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            clique.Deactive();
        }
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
            clique.Potency += change;
            TotalPotency.MarkDirty();
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
            clique.AdjustCliqueWillingness(change, showMessage);
        }
    }

    public bool CanBriberyClique(string cliqueKey, Map map, bool resultOnly)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            return clique.CanBribable(map, resultOnly: resultOnly);
        }

        return false;
    }

    public void BriberyClique(string cliqueKey, Map map)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: false))
        {
            clique.Bribery(map);
        }
    }

    public void TryCommunicateWithClique(string cliqueKey, Map map = null)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            clique.Communicate(branch, map);
        }
    }

    public void SetOrderBranch(Branch branch) => this.branch = branch;

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