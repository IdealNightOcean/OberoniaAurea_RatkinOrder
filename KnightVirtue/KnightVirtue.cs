using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class KnightVirtue : IExposable
{
    private KnightVirtueDef def;
    public KnightVirtueDef Def => def;
    public KnightPersonality Personality => def.relatedPersonality;

    private Dictionary<int, KnightVirtueTraitDef> selectedTraits = [];
    public bool HasEmptyTraitSlot => selectedTraits.Count < level;

    private int level;
    public int Level
    {
        get => level;
        set => level = Mathf.Clamp(value, 0, 3);
    }

    public KnightVirtue() { }
    public KnightVirtue(KnightVirtueDef def, int level)
    {
        this.def = def;
        this.Level = level;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, nameof(def));
        Scribe_Values.Look(ref level, nameof(level), 1);
        Scribe_Collections.Look(ref selectedTraits, nameof(selectedTraits), LookMode.Def);
    }
}