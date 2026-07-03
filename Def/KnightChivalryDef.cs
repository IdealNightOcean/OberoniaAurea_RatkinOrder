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
    /// 对应任务类型名称
    /// </summary>
    [MustTranslate]
    public string taskTypeLabel = string.Empty;

    public Color color;

    public MedalProperties medal;
    public JointPatrolProperties jointPatrol;

    public List<KnightChivalryDef> resonateChivalries = [];
    public HashSet<KnightChivalryDef> ResonateChivalriesSet { get; private set; }

    /// <summary>
    /// 骑士激励提供的激励buff
    /// </summary>
    public HediffDef stimulateHediff;

    private Texture2D colorTex;
    public Texture2D ColorTex
    {
        get
        {
            colorTex ??= SolidColorMaterials.NewSolidColorTexture(color);
            return colorTex;
        }
    }

    /// <summary>
    /// 图标
    /// </summary>
    public PathedTexture2D icon;
    /// <summary>
    /// 主要印记图标
    /// </summary>
    public PathedTexture2D primaryIcon;

    private List<ThingDef> allPreferredBuildingsCached;
    private List<KnightAcademicDef> allAcademicsCached;
    private List<BranchTaskDef> allBranchTasksCached;
    private List<KnightVirtueDef> allKnightVirtuesCached;

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

    public List<BranchTaskDef> AllBranchTasks
    {
        get
        {
            if (allBranchTasksCached is null)
            {
                allBranchTasksCached = [];
                foreach (BranchTaskDef def in DefDatabase<BranchTaskDef>.AllDefs)
                {
                    if (def.chivalry == this)
                    {
                        allBranchTasksCached.Add(def);
                    }
                }
            }
            return allBranchTasksCached;
        }
    }

    public List<KnightVirtueDef> AllKnightVirtues
    {
        get
        {
            if (allKnightVirtuesCached is null)
            {
                allKnightVirtuesCached = [];
                foreach (KnightVirtueDef def in DefDatabase<KnightVirtueDef>.AllDefs)
                {
                    if (def.chivalry == this)
                    {
                        allKnightVirtuesCached.Add(def);
                    }
                }
            }
            return allKnightVirtuesCached;
        }
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        resonateChivalries ??= [];
        ResonateChivalriesSet = [.. resonateChivalries];
    }
}