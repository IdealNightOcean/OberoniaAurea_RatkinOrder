using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatTransformerHandler
{
    public readonly Dictionary<BranchStatDef, BranchStatTransformer> branchStatTransformers = [];
    public Action<HashSet<BranchStatDef>> OnZeroFactorUnmerged;
    private HashSet<BranchStatDef> zeroFactorUnmergedStats;

    public bool TryGetStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        if (statDef is null)
        {
            transformer = new();
            return false;
        }

        return branchStatTransformers.TryGetValue(statDef, out transformer);
    }

    public void AddStatTransformer(BranchStatDef statDef, BranchStatTransformer transformer, bool replaceCur = false)
    {
        if (replaceCur || !branchStatTransformers.ContainsKey(statDef))
        {
            branchStatTransformers[statDef] = transformer;
        }
    }
    public bool RemoveStatTransformer(BranchStatDef statDef) => branchStatTransformers.Remove(statDef);

    public void MergeStatTransformer(BranchStatDef statDef, BranchStatTransformer toAdd, bool addIfMiss = true)
    {
        if (!toAdd.IsValid())
        {
            return;
        }

        if (branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer oldTransformer))
        {
            toAdd.MergeWith(oldTransformer);
            branchStatTransformers[statDef] = toAdd;
        }
        else if (addIfMiss)
        {
            branchStatTransformers.Add(statDef, toAdd);
        }
    }
    public void MergeStatTransformers(IEnumerable<KeyValuePair<BranchStatDef, BranchStatTransformer>> toAdds, bool addIfMiss = true)
    {
        if (toAdds is not null)
        {
            foreach (KeyValuePair<BranchStatDef, BranchStatTransformer> toAdd in toAdds)
            {
                MergeStatTransformer(toAdd.Key, toAdd.Value, addIfMiss);
            }
        }
    }

    public void MergeStatOffset(BranchStatModifier modifier, bool addIfMiss = true)
    {
        MergeStatOffset(modifier.statDef, modifier.value, addIfMiss);
    }

    public void MergeStatOffset(BranchStatDef statDef, float toAdd, bool addIfMiss = true)
    {
        if (branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer transformer))
        {
            transformer.MergeOffset(toAdd);
            if (transformer.IsValid())
            {
                branchStatTransformers[statDef] = transformer;
            }
            else
            {
                branchStatTransformers.Remove(statDef);
            }
        }
        else if (addIfMiss && toAdd != 0f)
        {
            branchStatTransformers.Add(statDef, new BranchStatTransformer() { offset = toAdd });
        }
    }

    public void MergeStatOffsets(IEnumerable<BranchStatModifier> modifiers, bool addIfMiss = true)
    {
        if (modifiers is not null)
        {
            foreach (BranchStatModifier modifier in modifiers)
            {
                MergeStatOffset(modifier.statDef, modifier.value, addIfMiss);
            }
        }
    }

    public void MergeStatFactor(BranchStatModifier modifier, bool addIfMiss = true)
    {
        UnmergeStatFactor(modifier.statDef, modifier.value, addIfMiss);
    }
    public void MergeStatFactor(BranchStatDef statDef, float toAdd, bool addIfMiss = true)
    {
        if (branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer transformer))
        {
            transformer.MergeFactor(toAdd);
            if (transformer.IsValid())
            {
                branchStatTransformers[statDef] = transformer;
            }
            else
            {
                branchStatTransformers.Remove(statDef);
            }
        }
        else if (addIfMiss && toAdd != 1f)
        {
            branchStatTransformers.Add(statDef, new BranchStatTransformer() { factor = toAdd });
        }
    }
    public void MergeStatFactors(IEnumerable<BranchStatModifier> modifiers, bool addIfMiss = true)
    {
        if (modifiers is not null)
        {
            foreach (BranchStatModifier modifier in modifiers)
            {
                MergeStatFactor(modifier.statDef, modifier.value, addIfMiss);
            }
        }
    }

    public void UnmergeStatTransformer(BranchStatDef statDef, BranchStatTransformer toRemove, bool doZeroUnmergedProcess = true)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer transformer))
        {
            return;
        }
        if (toRemove.factor == 0f)
        {
            zeroFactorUnmergedStats ??= [];
            zeroFactorUnmergedStats.Add(statDef);
            if (doZeroUnmergedProcess)
            {
                DoZeroFactorUnmergedProcess();
            }
            return;
        }

        transformer.Unmerge(toRemove);
        if (transformer.IsValid())
        {
            branchStatTransformers[statDef] = transformer;
        }
        else
        {
            branchStatTransformers.Remove(statDef);
        }
    }
    public void UnmergeStatTransformers(IEnumerable<KeyValuePair<BranchStatDef, BranchStatTransformer>> toRemoves, bool doZeroUnmergedProcess = true)
    {
        if (toRemoves is not null)
        {
            foreach (KeyValuePair<BranchStatDef, BranchStatTransformer> toRemove in toRemoves)
            {
                UnmergeStatTransformer(toRemove.Key, toRemove.Value, doZeroUnmergedProcess: false);
            }
            if (doZeroUnmergedProcess)
            {
                DoZeroFactorUnmergedProcess();
            }
        }

    }

    public void UnmergeStatOffset(BranchStatModifier modifier)
    {
        UnmergeStatFactor(modifier.statDef, modifier.value);
    }
    public void UnmergeStatOffset(BranchStatDef statDef, float toRemove)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer transformer))
        {
            return;
        }
        transformer.UnmergeOffset(toRemove);
        if (transformer.IsValid())
        {
            branchStatTransformers[statDef] = transformer;
        }
        else
        {
            branchStatTransformers.Remove(statDef);
        }
    }
    public void UnmergeStatsOffset(IEnumerable<BranchStatModifier> modifiers)
    {
        if (modifiers is not null)
        {
            foreach (BranchStatModifier modifier in modifiers)
            {
                UnmergeStatOffset(modifier.statDef, modifier.value);
            }
        }
    }

    public void UnmergeStatFactor(BranchStatModifier modifier, bool doZeroUnmergedProcess = true)
    {
        UnmergeStatFactor(modifier.statDef, modifier.value, doZeroUnmergedProcess);
    }
    public void UnmergeStatFactor(BranchStatDef statDef, float toRemove, bool doZeroUnmergedProcess = true)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer transformer))
        {
            return;
        }
        if (toRemove == 0f)
        {
            zeroFactorUnmergedStats ??= [];
            zeroFactorUnmergedStats.Add(statDef);
            if (doZeroUnmergedProcess)
            {
                DoZeroFactorUnmergedProcess();
            }
            return;
        }

        transformer.UnmergeFactor(toRemove);
        if (transformer.IsValid())
        {
            branchStatTransformers[statDef] = transformer;
        }
        else
        {
            branchStatTransformers.Remove(statDef);
        }
    }
    public void UnmergeStatsFactor(IEnumerable<BranchStatModifier> modifiers, bool doZeroUnmergedProcess = true)
    {
        if (modifiers is not null)
        {
            foreach (BranchStatModifier modifier in modifiers)
            {
                UnmergeStatFactor(modifier.statDef, modifier.value, doZeroUnmergedProcess: false);
            }
            if (doZeroUnmergedProcess)
            {
                DoZeroFactorUnmergedProcess();
            }
        }
    }

    public string GetDetailString()
    {
        if (branchStatTransformers.NullOrEmpty())
        {
            return "None";
        }
        StringBuilder sb = new();
        foreach (KeyValuePair<BranchStatDef, BranchStatTransformer> kv in branchStatTransformers)
        {
            sb.AppendInNewLine(kv.Key.label);
            sb.Append(":");
            sb.AppendInNewLine(kv.Value.ToString());
        }
        return sb.ToString();
    }

    public void DoZeroFactorUnmergedProcess()
    {
        if (zeroFactorUnmergedStats is null || zeroFactorUnmergedStats.Count <= 0)
        {
            return;
        }
        try
        {
            OnZeroFactorUnmerged?.Invoke(zeroFactorUnmergedStats);
        }
        finally
        {
            zeroFactorUnmergedStats = null;
        }
    }
}
