using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.QuestGen;
using System;
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
            inSignalEnable = QuestGen.slate.Get<string>(OARO_KeyLibrary_SlateStoreAs.inSignal),
        };
        questPart_CliquesManager.SetOrderBranch(branch.GetValue(QuestGen.slate) ?? QuestGen.slate.Get<Branch>(OARO_KeyLibrary_SlateStoreAs.branch));
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

    private List<QuestClique> allCliques;
    private Dictionary<string, QuestClique> allCliquesDict;
    public IReadOnlyList<QuestClique> AllCliques => allCliques;

    public string InSignalOutPotency;
    public string OutSignalOutPotency;

    public LazyMutable<float> TotalPotency { get; }

    private int ticksToNextCheck = 1000;

    public QuestPart_CliquesManager()
    {
        TotalPotency = new(refreshFunc: () => allCliques?.Where(c => c.IsActive).Sum(c => c.Potency) ?? 0f);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref allCliques, nameof(allCliques), LookMode.Deep);

        Scribe_Values.Look(ref InSignalOutPotency, nameof(InSignalOutPotency));
        Scribe_Values.Look(ref OutSignalOutPotency, nameof(OutSignalOutPotency));

        Scribe_Values.Look(ref ticksToNextCheck, nameof(ticksToNextCheck), 0);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (allCliques is not null)
            {
                allCliques.RemoveAll(c => c is null || String.IsNullOrEmpty(c.Key));
                allCliquesDict = new(allCliques.Count);
                foreach (QuestClique clique in allCliques)
                {
                    allCliquesDict.Add(clique.Key, clique);
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
        allCliquesDict = null;
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
        if (allCliquesDict.NullOrEmpty())
        {
            return;
        }

        StringBuilder branchCliqueInfoSB = new("OARO_BranchCliquesInfoText_Header".Translate(branch.RatkinOrder.Name.Named(OARO_KeyLibrary_FormatArgName.OrderName)));
        branchCliqueInfoSB.AppendLine();
        foreach (Branch cliqueBranch in allCliques.Where(c => c.IsBranchClique).Select(c => c.RelatedBranch).OrderBy(b => b?.RatkinOrder.LoadID ?? int.MinValue))
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
            foreach (QuestClique clique in allCliques)
            {
                if (clique.ticksToInactive > 0 && (clique.ticksToInactive -= 1000) <= 0)
                {
                    clique.TryActive(directly: true);
                }
            }
        }
    }

    public bool HasClique(string cliqueKey) => allCliquesDict?.ContainsKey(cliqueKey) ?? false;

    public bool TryGetClique(string cliqueKey, out QuestClique clique, bool showErrorIfMiss = false)
    {
        if (allCliquesDict is not null && allCliquesDict.TryGetValue(cliqueKey, out clique))
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
        return KeyLibrary_Misc.ErrorTip;
    }

    public bool TryAddClique(QuestClique clique, bool replaceCur = false, bool defaultActive = false)
    {
        bool added = false;
        allCliques ??= [];
        allCliquesDict ??= [];

        if (allCliquesDict.TryGetValue(clique.Key, out QuestClique oldClique))
        {
            if (replaceCur)
            {
                if (oldClique.IsActive)
                {
                    oldClique.Deactive();
                }

                allCliquesDict[clique.Key] = clique;
                allCliques.Remove(oldClique);
                allCliques.Add(clique);
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
            allCliquesDict.Add(clique.Key, clique);
            allCliques.Add(clique);
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
        if (TryGetClique(cliqueKey, out QuestClique clique) && allCliquesDict.Remove(cliqueKey))
        {
            allCliques.Remove(clique);
            Find.SignalManager.SendSignal(new Signal(SignalCliqueRemoved(quest), clique.Named(KeyLibrary_FormatArgName.SUBJECT)));
            if (clique.IsActive)
            {
                clique.Deactive();
            }
        }
    }

    public bool IsCliqueActive(string cliqueKey)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            return clique.IsActive;
        }
        return false;
    }

    public bool CanActiveClique(string cliqueKey, bool directly = false)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            return clique.CanActiveNow(directly: directly, resultOnly: true);
        }
        return false;
    }

    public bool TryActiveClique(string cliqueKey, bool directly = false, Map map = null, int activeDelayTicks = -1)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique))
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
        if (TryGetClique(cliqueKey, out QuestClique clique))
        {
            return clique.CanBribable(map, resultOnly: resultOnly);
        }

        return false;
    }

    public void BriberyClique(string cliqueKey, Map map)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: true))
        {
            clique.Bribery(map);
        }
    }

    public void TryCommunicateWithClique(string cliqueKey, Map map = null)
    {
        if (TryGetClique(cliqueKey, out QuestClique clique, showErrorIfMiss: true))
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
        List<QuestClique> toRemove = allCliques?.Where(c => c.RelatedBranch == branch).ToList();
        if (toRemove is not null)
        {
            foreach (QuestClique clique in toRemove)
            {
                RemoveClique(clique.Key);
            }
        }
    }

    public void Notify_RatkinOrderRemoved(RatkinOrder ratkinOrder)
    {
        if (branch?.RatkinOrder == ratkinOrder)
        {
            branch = null;
        }

        List<QuestClique> toRemove = allCliques?.Where(c => c.RelatedRatkinOrder == ratkinOrder).ToList();
        if (toRemove is not null)
        {
            foreach (QuestClique clique in toRemove)
            {
                RemoveClique(clique.Key);
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
        foreach (QuestClique clique in allCliques)
        {
            sb.AppendInNewLine((i++).ToString());
            sb.AppendInNewLine($"Key: {clique.Key},  Name:{clique.Name},  IsActive:{clique.IsActive} ({clique.ticksToInactive})");
            sb.AppendInNewLine($"Potency: {clique.Potency:F2},  Willingness:{clique.Willingness:F2}");
            sb.AppendInNewLine($"IsBribable: {clique.IsBribable},  IsCommunicable: {clique.IsCommunicable}");
            sb.AppendInNewLine($"IsBranchClique: {clique.IsBranchClique}, RelatedBranch: {clique.RelatedBranch?.Name ?? "NULL"}");
            sb.AppendInNewLine("------------");
        }
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(sb.ToTaggedString()));
    }
}