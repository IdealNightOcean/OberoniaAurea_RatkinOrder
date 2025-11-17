using NightOcean;
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
        Military = 1,
        Information = 2,
        Diplomacy = 4
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

    public BranchTaskType FocusedTaskType => Branch.TaskHandler.FocusedTaskType;

    public PatrolInteractionType CurInteractions;

    public int NextIncidentCheckTick = -1;

    public JointBranchRecord()
    {
        TaskPotency = new(initValue: 0f, refreshFunc: PotencyUpdate);
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref Branch, "Branch");
        Scribe_Values.Look(ref CurInteractions, "CurInteractions", PatrolInteractionType.None);
        Scribe_Values.Look(ref NextIncidentCheckTick, "NextIncidentCheckTick", -1);

        Scribe_Values.Look(ref potencyFactor, "potencyFactor", 1f);
        Scribe_Values.Look(ref potencyOffset, "potencyOffset", 0f);
    }

    public void ActiveInteraction(PatrolInteractionType interaction)
    {
        if (HasInteraction(interaction))
        {
            return;
        }

        CurInteractions |= interaction;

        if (CurInteractions == allInteraction)
        {
            Branch.SetFriendly(friendly: true);
        }
    }

    public bool HasInteraction(PatrolInteractionType interaction) => (CurInteractions & interaction) != 0;

    private float PotencyUpdate()
    {
        float potency = (Branch.Squad.MemberCount * 10f)
              * (1f + Branch.MedalHandler.MedalTypeCount * 0.1f)
              * (1f + Branch.FacilityHandler.TotalFacilityLevel.Value * 0.02f)
              * (Branch.IsBranchOfType(Branch.BranchType.Honor) ? 1.2f : 1f)
              * PotencyFactor;
        return Mathf.Max(0f, potency + PotencyOffset);
    }
}