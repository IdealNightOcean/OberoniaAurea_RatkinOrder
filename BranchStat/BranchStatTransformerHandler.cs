using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatTransformerHandler
{
    public readonly Dictionary<BranchStatDef, BranchStatTransformer> branchStatTransformers = [];

    public bool TryGetStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        if (statDef is null)
        {
            transformer = BranchStatTransformer.DefaultTransformer;
            return false;
        }

        return branchStatTransformers.TryGetValue(statDef, out transformer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddStatModifier(BranchStatModifier modifier)
    {
        AddStatTransformer(modifier.statDef, modifier.Transformer);
    }
    public void AddStatModifiers(IEnumerable<BranchStatModifier> modifiers)
    {
        if (modifiers is null)
        {
            return;
        }

        foreach (BranchStatModifier modifier in modifiers)
        {
            try
            {
                if (branchStatTransformers.TryGetValue(modifier.statDef, out BranchStatTransformer transformer))
                {
                    transformer.MergeWith(modifier.Transformer);
                    branchStatTransformers[modifier.statDef] = transformer;
                }
                else
                {
                    branchStatTransformers.Add(modifier.statDef, modifier.Transformer);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to add stat modifier for {modifier.statDef.defName}: {ex.Message}");
                continue;
            }
        }
    }

    public bool RemoveStatRecord(BranchStatDef statDef)
    {
        return branchStatTransformers.Remove(statDef);
    }

    public void AddStatTransformer(BranchStatDef statDef, BranchStatTransformer transformer)
    {
        if (!transformer.IsValid())
        {
            return;
        }

        if (branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer oldTransformer))
        {
            transformer.MergeWith(oldTransformer);
            branchStatTransformers[statDef] = transformer;
        }
        else
        {
            branchStatTransformers.Add(statDef, transformer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveStatModifier(BranchStatModifier modifier)
    {
        RemoveStatTransformer(modifier.statDef, modifier.Transformer);
    }
    public void RemoveStatModifies(IEnumerable<BranchStatModifier> modifies)
    {
        if (modifies is null)
        {
            return;
        }

        foreach (BranchStatModifier modify in modifies)
        {
            try
            {
                if (branchStatTransformers.TryGetValue(modify.statDef, out BranchStatTransformer transformer))
                {
                    transformer.Unmerge(modify.Transformer);
                    if (transformer.IsValid())
                    {
                        branchStatTransformers[modify.statDef] = transformer;
                    }
                    else
                    {
                        branchStatTransformers.Remove(modify.statDef);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to Remove stat modifier for {modify.statDef.defName}: {ex.Message}");
                continue;
            }
        }
    }

    public void RemoveStatTransformer(BranchStatDef statDef, BranchStatTransformer toRemove)
    {
        if (!branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer transformer))
        {
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
}
