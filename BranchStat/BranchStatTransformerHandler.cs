using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchStatTransformerHandler
{
    public Dictionary<BranchStatDef, BranchStatTransformer> branchStatTransformers;

    public bool TryGetStatTransformer(BranchStatDef statDef, out BranchStatTransformer transformer)
    {
        if (statDef is null || branchStatTransformers is null)
        {
            transformer = default;
            return false;
        }

        return branchStatTransformers.TryGetValue(statDef, out transformer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddStatModifier(BranchStatModifier modifier)
    {
        AddStatTransformer(modifier.statDef, modifier.statTransformer);
    }
    public void AddStatModifiers(IEnumerable<BranchStatModifier> modifiers)
    {
        if (modifiers is null)
        {
            return;
        }

        branchStatTransformers ??= [];
        foreach (BranchStatModifier modifier in modifiers)
        {
            try
            {
                if (branchStatTransformers.TryGetValue(modifier.statDef, out BranchStatTransformer oldTransformer))
                {
                    branchStatTransformers[modifier.statDef] = BranchStatTransformer.Merge(oldTransformer, modifier.statTransformer);
                }
                else
                {
                    branchStatTransformers.Add(modifier.statDef, modifier.statTransformer);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to add stat modifier for {modifier.statDef.defName}: {ex.Message}");
                continue;
            }
        }

        if (branchStatTransformers.Count == 0)
        {
            branchStatTransformers = null;
        }
    }

    public void AddStatTransformer(BranchStatDef statDef, BranchStatTransformer transformer)
    {
        if (!BranchStatTransformer.IsValid(transformer))
        {
            return;
        }

        if (branchStatTransformers is null)
        {
            branchStatTransformers = new Dictionary<BranchStatDef, BranchStatTransformer> { { statDef, transformer } };
        }
        else
        {
            if (branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer oldTransformer))
            {
                branchStatTransformers[statDef] = BranchStatTransformer.Merge(oldTransformer, transformer);
            }
            else
            {
                branchStatTransformers.Add(statDef, transformer);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveStatModifier(BranchStatModifier modifier)
    {
        RemoveStatTransformer(modifier.statDef, modifier.statTransformer);
    }
    public void RemoveStatModifies(IEnumerable<BranchStatModifier> modifies)
    {
        if (modifies is null || branchStatTransformers is null)
        {
            return;
        }

        foreach (BranchStatModifier modify in modifies)
        {
            try
            {
                if (branchStatTransformers.TryGetValue(modify.statDef, out BranchStatTransformer oldTransformer))
                {
                    BranchStatTransformer newTransformer = BranchStatTransformer.Unmerge(oldTransformer, modify.statTransformer);
                    if (BranchStatTransformer.IsValid(newTransformer))
                    {
                        branchStatTransformers[modify.statDef] = newTransformer;
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

        if (branchStatTransformers.Count == 0)
        {
            branchStatTransformers = null;
        }
    }

    public void RemoveStatTransformer(BranchStatDef statDef, BranchStatTransformer transformer)
    {
        if (branchStatTransformers is null || !branchStatTransformers.TryGetValue(statDef, out BranchStatTransformer oldTransformer))
        {
            return;
        }

        BranchStatTransformer newTransformer = BranchStatTransformer.Unmerge(oldTransformer, transformer);
        if (BranchStatTransformer.IsValid(newTransformer))
        {
            branchStatTransformers[statDef] = newTransformer;
        }
        else
        {
            branchStatTransformers.Remove(statDef);
            if (branchStatTransformers.Count == 0)
            {
                branchStatTransformers = null;
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
}
