using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue : IExposable
{
    private KnightVirtueDef def;
    public KnightVirtueDef Def => def;
    public KnightPersonality Personality => def.relatedPersonality;

    private int level;
    public int Level
    {
        get => level;
        set => level = Mathf.Clamp(value, 0, 3);
    }

    private List<KnightVirtueTraitDef> selectedTraits = [];
    public IReadOnlyList<KnightVirtueTraitDef> SelectedTraits => selectedTraits;
    public int SelectedTraitMaxLevel => selectedTraits.Count;
    public bool HasEmptyTraitSlot => selectedTraits.Count < level;

    public KnightVirtue() { }
    public KnightVirtue(KnightVirtueDef def, int level)
    {
        this.def = def;
        this.Level = level;
    }

    public KnightVirtueTraitDef GetTraitOfLevel(int level)
    {
        if (level < 0 || level >= selectedTraits.Count)
        {
            return null;
        }
        return selectedTraits[level];
    }

    public bool HasTrait(KnightVirtueTraitDef traitDef) => selectedTraits.Contains(traitDef);

    public bool SelectTrait(KnightVirtueTraitDef traitDef, int level, bool replaceCur)
    {
        if (level < 0)
        {
            Log.Error($"尝试为骑士美德 '{def?.defName ?? "UNKOWN"}' 选择词条 '{traitDef?.defName ?? "UNKOWN"}' 时失败：词条等级 {level} 不能为负数");
            return false;
        }
        if (level == SelectedTraitMaxLevel + 1)
        {
            selectedTraits.Add(traitDef);
            return true;
        }
        else if (level > selectedTraits.Count + 1)
        {
            Log.Error($"尝试为骑士美德 '{def?.defName ?? "UNKOWN"}' 选择词条 '{traitDef?.defName ?? "UNKOWN"}' 时失败：词条等级 {level} 不能超过 {selectedTraits.Count + 1} (当前最大词条等级+1)");
            return false;
        }
        else
        {
            if (replaceCur)
            {
                selectedTraits[level - 1] = traitDef;
                return true;
            }
            else
            {
                Log.Error($"尝试为骑士美德 '{def?.defName ?? "UNKOWN"}' 选择词条 '{traitDef?.defName ?? "UNKOWN"}' 时失败：无法在不替换的情况下为等级 {level} 选择词条，该等级已存在词条");
                return false;
            }
        }
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_Values.Look(ref level, nameof(level), 1);
        Scribe_Collections.Look(ref selectedTraits, nameof(selectedTraits), LookMode.Def);
    }
}