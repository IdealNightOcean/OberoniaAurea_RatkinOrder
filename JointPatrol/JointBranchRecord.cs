using NightOcean;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JointBranchRecord : IExposable
{
    [Flags]
    public enum PatrolInteractionType : byte
    {
        None = 0,
        Military = 1, //军备维护
        Information = 2, //地区信息
        Diplomacy = 4 //改善当地关系
    }

    private static readonly PatrolInteractionType allInteraction = PatrolInteractionType.Military | PatrolInteractionType.Information | PatrolInteractionType.Diplomacy;

    public Branch Branch;
    public readonly LazyMutable<float> TaskPotency;

    private float potencyFactor = 1f;
    private float potencyOffset;

    public float PotencyFactor
    {
        get => potencyFactor;
        set => potencyFactor = Mathf.Max(0f, value);
    }
    public float PotencyOffset
    {
        get => potencyOffset;
        set => potencyOffset = value;
    }

    public KnightChivalryDef FocusedTaskChivalry => Branch.TaskHandler.FocusedTaskChivalry;

    public PatrolInteractionType CurInteractions;

    public int NextIncidentCheckTick = -1;

    public JointBranchRecord()
    {
        TaskPotency = new(refreshFunc: PotencyUpdate);
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Branch, nameof(Branch));
        Scribe_Values.Look(ref CurInteractions, nameof(CurInteractions), PatrolInteractionType.None);
        Scribe_Values.Look(ref NextIncidentCheckTick, nameof(NextIncidentCheckTick), -1);

        Scribe_Values.Look(ref potencyFactor, nameof(potencyFactor), 1f);
        Scribe_Values.Look(ref potencyOffset, nameof(potencyOffset), 0f);
    }

    public AcceptanceReport CanActiveInteraction(PatrolInteractionType interaction, Map map, bool resultOnly)
    {
        if (HasInteraction(interaction))
        {
            return resultOnly ? false : "OARO_AlreadyHas_PatrolInteraction".Translate();
        }
        if (map is null)
        {
            return false;
        }
        int neededSilver = interaction switch
        {
            PatrolInteractionType.Military => (int)(3000 + TaskPotency.Value * 10),
            PatrolInteractionType.Information => (int)(4000 + TaskPotency.Value * 20),
            PatrolInteractionType.Diplomacy => (int)(8000 + TaskPotency.Value * 20),
            _ => -1
        };

        if (neededSilver > 0 && !OberoniaAurea_Frame.Utility.OAFrame_MapUtility.HasEnoughThingsOfDef(map, ThingDefOf.Silver, neededSilver))
        {
            return resultOnly ? false : "OAFrame_NeedCountOfThing".Translate(ThingDefOf.Silver.label, neededSilver);
        }

        return true;
    }

    public bool ActiveInteraction(PatrolInteractionType interaction, bool applyCost, Map map = null)
    {
        if (HasInteraction(interaction))
        {
            return false;
        }

        if (applyCost && map is not null)
        {
            int neededSilver = interaction switch
            {
                PatrolInteractionType.Military => (int)(3000 + TaskPotency.Value * 10),
                PatrolInteractionType.Information => (int)(4000 + TaskPotency.Value * 20),
                PatrolInteractionType.Diplomacy => (int)(8000 + TaskPotency.Value * 20),
                _ => -1
            };
            if (neededSilver > 0)
            {
                map.DestroyThingsOfDef(ThingDefOf.Silver, neededSilver);
            }
        }

        CurInteractions |= interaction;
        if (interaction == PatrolInteractionType.Military)
        {
            potencyFactor += 0.25f;
        }

        if (CurInteractions == allInteraction)
        {
            Branch.SetFriendly(active: true);
        }
        return true;
    }

    public bool HasInteraction(PatrolInteractionType interaction) => (CurInteractions & interaction) != 0;

    private float PotencyUpdate()
    {
        float potency = (Branch.Squad.MemberCount * 10f)
              * (1f + Branch.MedalHandler.MedalTypeCount * 0.1f)
              * (1f + Branch.FacilityHandler.TotalFacilityLevel * 0.02f)
              * (Branch.IsBranchOfType(Branch.BranchType.Honor) ? 1.2f : 1f)
              * PotencyFactor;
        return Mathf.Max(0f, potency + PotencyOffset);
    }
}