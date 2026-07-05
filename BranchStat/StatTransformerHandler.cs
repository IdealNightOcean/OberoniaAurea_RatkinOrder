using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatTransformerHandler<T> where T : OAROStatDefBase
{
    public readonly Dictionary<T, StatTransformer> branchStatTransformers = [];
    public Action<IEnumerable<T>> OnZeroFactorUnmerged;
    private HashSet<T> zeroFactorUnmergedStats;

    public bool TryGetStatTransformer(T statDef, out StatTransformer transformer)
    {
        if (statDef is null)
        {
            transformer = new();
            return false;
        }

        return branchStatTransformers.TryGetValue(statDef, out transformer);
    }

    public void AddStatTransformer(T statDef, StatTransformer transformer, bool replaceCur = false)
    {
        if (replaceCur || !branchStatTransformers.ContainsKey(statDef))
        {
            branchStatTransformers[statDef] = transformer;
        }
    }
    public bool RemoveStatTransformer(T statDef) => branchStatTransformers.Remove(statDef);

    public void MergeStatTransformer(T statDef, StatTransformer toAdd, bool addIfMiss = true)
    {
        if (!toAdd.IsValid())
            return;

        if (branchStatTransformers.TryGetValue(statDef, out StatTransformer oldTransformer))
        {
            toAdd.MergeWith(oldTransformer);
            branchStatTransformers[statDef] = toAdd;
        }
        else if (addIfMiss)
        {
            branchStatTransformers.Add(statDef, toAdd);
        }
    }
    public void MergeStatTransformers(IEnumerable<KeyValuePair<T, StatTransformer>> toAdds, bool addIfMiss = true)
    {
        if (toAdds is not null)
        {
            foreach (KeyValuePair<T, StatTransformer> toAdd in toAdds)
            {
                MergeStatTransformer(toAdd.Key, toAdd.Value, addIfMiss);
            }
        }
    }

    public void MergeStatOffset(StatModifier<T> modifier, bool addIfMiss = true)
    {
        MergeStatOffset(modifier.statDef, modifier.value, addIfMiss);
    }

    public void MergeStatOffset(T statDef, float toAdd, bool addIfMiss = true)
    {
        if (branchStatTransformers.TryGetValue(statDef, out StatTransformer transformer))
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
            branchStatTransformers.Add(statDef, new StatTransformer() { offset = toAdd });
        }
    }

    public void MergeStatOffsets(IEnumerable<StatModifier<T>> modifiers, bool addIfMiss = true)
    {
        if (modifiers is not null)
        {
            foreach (StatModifier<T> modifier in modifiers)
            {
                MergeStatOffset(modifier.statDef, modifier.value, addIfMiss);
            }
        }
    }

    public void MergeStatFactor(StatModifier<T> modifier, bool addIfMiss = true)
    {
        MergeStatFactor(modifier.statDef, modifier.value, addIfMiss);
    }
    public void MergeStatFactor(T statDef, float toAdd, bool addIfMiss = true)
    {
        if (branchStatTransformers.TryGetValue(statDef, out StatTransformer transformer))
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
            branchStatTransformers.Add(statDef, new StatTransformer() { factor = toAdd });
        }
    }
    public void MergeStatFactors(IEnumerable<StatModifier<T>> modifiers, bool addIfMiss = true)
    {
        if (modifiers is not null)
        {
            foreach (StatModifier<T> modifier in modifiers)
            {
                MergeStatFactor(modifier.statDef, modifier.value, addIfMiss);
            }
        }
    }

    public void UnmergeStatTransformer(T statDef, StatTransformer toRemove, bool doZeroUnmergedProcess = true)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out StatTransformer transformer))
            return;

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
    public void UnmergeStatTransformers(IEnumerable<KeyValuePair<T, StatTransformer>> toRemoves, bool doZeroUnmergedProcess = true)
    {
        if (toRemoves is not null)
        {
            foreach (KeyValuePair<T, StatTransformer> toRemove in toRemoves)
            {
                UnmergeStatTransformer(toRemove.Key, toRemove.Value, doZeroUnmergedProcess: false);
            }
            if (doZeroUnmergedProcess)
            {
                DoZeroFactorUnmergedProcess();
            }
        }
    }

    public void UnmergeStatOffset(StatModifier<T> modifier)
    {
        UnmergeStatFactor(modifier.statDef, modifier.value);
    }
    public void UnmergeStatOffset(T statDef, float toRemove)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out StatTransformer transformer))
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
    public void UnmergeStatsOffset(IEnumerable<StatModifier<T>> modifiers)
    {
        if (modifiers is not null)
        {
            foreach (StatModifier<T> modifier in modifiers)
            {
                UnmergeStatOffset(modifier.statDef, modifier.value);
            }
        }
    }

    public void UnmergeStatFactor(StatModifier<T> modifier, bool doZeroUnmergedProcess = true)
    {
        UnmergeStatFactor(modifier.statDef, modifier.value, doZeroUnmergedProcess);
    }
    public void UnmergeStatFactor(T statDef, float toRemove, bool doZeroUnmergedProcess = true)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out StatTransformer transformer))
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
    public void UnmergeStatsFactor(IEnumerable<StatModifier<T>> modifiers, bool doZeroUnmergedProcess = true)
    {
        if (modifiers is not null)
        {
            foreach (StatModifier<T> modifier in modifiers)
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
        foreach (KeyValuePair<T, StatTransformer> kv in branchStatTransformers)
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
