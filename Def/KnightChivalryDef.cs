using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士精神Def
/// </summary>
public class KnightChivalryDef : Def
{
    /// <summary>
    /// 交流行为 - 骑士激励概率Offset
    /// </summary>
    public float knightlyTalkOffset;

    public Color color;

    public List<KnightChivalryDef> resonateChivalries = [];

    public HashSet<KnightChivalryDef> ResonateChivalriesSet { get; private set; }

    private Texture2D colorTex;
    public Texture2D ColorTex
    {
        get
        {
            colorTex ??= SolidColorMaterials.NewSolidColorTexture(color);
            return colorTex;
        }
    }

    private List<ThingDef> allPreferredBuildingsCached;
    private List<KnightAcademicDef> allAcademicsCached;

    public List<ThingDef> AllPreferredBuildings
    {
        get
        {
            if (allPreferredBuildingsCached is null)
            {
                allPreferredBuildingsCached = [];
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    if (def.building is null)
                        continue;
                    if (def.GetModExtension<ResidentKnightPreferredBuildingExtension>()?.chivalry == this)
                    {
                        allPreferredBuildingsCached.Add(def);
                    }
                }
            }
            return allPreferredBuildingsCached;
        }
    }

    public List<KnightAcademicDef> AllAcademics
    {
        get
        {
            if (allAcademicsCached is null)
            {
                allAcademicsCached = [];
                foreach (KnightAcademicDef def in DefDatabase<KnightAcademicDef>.AllDefs)
                {
                    if (def.chivalry == this)
                    {
                        allAcademicsCached.Add(def);
                    }
                }
            }
            return allAcademicsCached;
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        resonateChivalries ??= [];
        ResonateChivalriesSet = [.. resonateChivalries];
    }
}