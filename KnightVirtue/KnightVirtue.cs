using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士美德
/// </summary>
public class KnightVirtue : IExposable
{
    public struct KnightVirtueTrait : IExposable
    {
        public KnightVirtueTraitDef def;
        public int level;

        public KnightVirtueTrait(KnightVirtueTraitDef def, int level)
        {
            this.def = def ?? throw new System.ArgumentNullException(nameof(def));
            this.level = level > 0 ? level : 1;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, nameof(def));
            Scribe_Values.Look(ref level, nameof(level), defaultValue: 1);
        }
    }

    private KnightVirtueDef def;
    public KnightVirtueDef Def => def;
    public KnightChivalryDef Chivalry => def.chivalry;

    public ResidentKnight knight;
    public Pawn Pawn => knight.Pawn;

    private int level;
    public int Level
    {
        get => level;
        set
        {
            level = Mathf.Clamp(value, 1, def.maxLevel);
        }
    }

    private List<KnightVirtueTrait> selectedTraits = [];
    public IReadOnlyList<KnightVirtueTrait> SelectedTraits => selectedTraits;
    public int SelectedTraitMaxLevel => selectedTraits.Count;
    public bool HasUnusedTraitSlot => selectedTraits.Count < level;

    public KnightVirtue() { }

    public static KnightVirtue GenerateKnightVirtue(ResidentKnight knight, KnightVirtueDef def, int level)
    {
        KnightVirtue virtue = Activator.CreateInstance(def.virtueClass) as KnightVirtue;
        virtue.knight = knight;
        virtue.def = def;
        virtue.level = Mathf.Clamp(level, 1, def.maxLevel);
        return virtue;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_References.Look(ref knight, nameof(knight));

        Scribe_Values.Look(ref level, nameof(level), defaultValue: 1);
        Scribe_Collections.Look(ref selectedTraits, nameof(selectedTraits), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (selectedTraits.RemoveAll(trait => trait.def is null || trait.level <= 0) > 0)
            {
                Log.Error("");
            }
            selectedTraits.Sort((a, b) => a.level - b.level);
        }
    }

    public KnightVirtueTraitDef GetTraitOfLevel(int traitLevel)
    {
        int targetIndex = GetTraitOfLevelIndex(traitLevel);
        if (targetIndex < 0)
            return null;
        else
            return selectedTraits[targetIndex].def;
    }

    public bool HasTrait(KnightVirtueTraitDef traitDef)
    {
        for (int i = 0; i < selectedTraits.Count; i++)
            if (selectedTraits[i].def == traitDef)
                return true;

        return false;
    }

    public bool TrySelectTraitForLevel(KnightVirtueTraitDef traitDef, int traitLevel, bool replaceCur = false)
    {
        if (!SelectTraitForLevel(traitDef, traitLevel, replaceCur))
            return false;

        ResidentPawnsManager.CacheManager.KnightsHasUnusedTraitSlot?.MarkDirty();
        return true;
    }

    public virtual void SpecialStatModifies(HediffStageTemplate buffStageTemplate) { }

    public virtual void Notify_KilledPawn(Pawn victim, DamageInfo? dinfo) { }

    public virtual void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt) { }

    public virtual void Notify_Stimulate(Pawn recipient) { }

    private bool SelectTraitForLevel(KnightVirtueTraitDef traitDef, int traitLevel, bool replaceCur = false)
    {
        if (traitLevel < 1 || traitLevel > def.maxLevel)
        {
            Log.Error($"尝试为骑士美德 '{def?.defName ?? "UNKNOWN"}' 选择词条失败：等级 {traitLevel} 无效");
            return false;
        }

        int targetIndex = GetTraitOfLevelIndex(traitLevel);
        if (targetIndex >= 0)
        {
            if (!replaceCur)
            {
                Log.Error($"尝试为骑士美德 '{def.defName}' 选择词条失败：等级 {traitLevel} 已存在词条且未允许替换");
                return false;
            }
            else
            {
                selectedTraits[targetIndex] = new KnightVirtueTrait(traitDef, traitLevel);
                return true;
            }
        }
        else
        {
            targetIndex = ~targetIndex;
            selectedTraits.Insert(targetIndex, new KnightVirtueTrait(traitDef, traitLevel));
            return true;
        }
    }

    private int GetTraitOfLevelIndex(int traitLevel)
    {
        if (traitLevel < 1 || traitLevel > def.maxLevel)
            return -1;

        int left = 0;
        int right = selectedTraits.Count;
        int mid;

        while (left < right)
        {
            mid = left + ((right - left) >> 1);
            int compare = selectedTraits[mid].level - traitLevel;
            if (compare == 0)
                return mid;
            else if (compare < 0)
                left = mid + 1;
            else
                right = mid;
        }

        return ~left;
    }
}