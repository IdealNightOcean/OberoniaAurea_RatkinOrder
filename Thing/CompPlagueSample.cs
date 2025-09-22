using OberoniaAurea_Frame;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompPlagueSample : CompInteractWithThing
{
    private Quest quest;
    public Quest AssociatedQuest => quest;

    private Worldobject_PlagueVillage plagueVillage;

    private bool isStrangePlague;
    public float MaxSamplePoints => isStrangePlague ? 800f : 400f;
    private float samplePoints = 0f;
    public float SamplePoints => samplePoints;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref quest, "quest");
        Scribe_References.Look(ref plagueVillage, "plagueVillage");

        Scribe_Values.Look(ref isStrangePlague, "isStrangePlague", defaultValue: false);
        Scribe_Values.Look(ref samplePoints, "samplePoints", 0f);
    }

    public void InitSample(Quest quest, Worldobject_PlagueVillage plagueVillage, bool isStrangePlague)
    {
        this.quest = quest;
        this.plagueVillage = plagueVillage;
        this.isStrangePlague = isStrangePlague;
    }

    public void AddSamplePoints(float points)
    {
        samplePoints += points;
        if (samplePoints >= MaxSamplePoints)
        {
            samplePoints = MaxSamplePoints;
        }
    }

    public override void InteractionResult(Pawn pawn)
    {
        int medicineSkillLevel = pawn.GetSkillLevel(SkillDefOf.Medicine);
        if (quest is not null && plagueVillage is not null)
        {
            float controlGain = 20f + 2f * medicineSkillLevel;
            if (isStrangePlague)
            {
                controlGain *= 2f;
            }
            plagueVillage.PlagueControl += Mathf.RoundToInt(controlGain);
            Messages.Message("OARO_PlagueSample_Result".Translate(Mathf.RoundToInt(controlGain)), MessageTypeDefOf.PositiveEvent);
        }

        if (medicineSkillLevel < 10 && Rand.Chance(0.75f))
        {
            Messages.Message("OARO_PlagueSample_InfectioPlague".Translate(pawn), MessageTypeDefOf.NegativeEvent);
        }

        if (!parent.Destroyed)
        {
            parent.Destroy();
        }
    }
}

public class JobDriver_PlagueSample : JobDriver_InteractWithThing
{
    [Unsaved] private CompPlagueSample sampleComp;
    private CompPlagueSample SampleComp => sampleComp ??= job.targetA.Thing?.TryGetComp<CompPlagueSample>();

    protected override float GetTotalWorkAmount(float baseWorkAmount)
    {
        return SampleComp?.SamplePoints ?? 1f;
    }

    protected override void JobTickIntervalAction(int delta)
    {
        SampleComp?.AddSamplePoints(tickWorkAmount * delta);
        base.JobTickIntervalAction(delta);
    }

    protected override void InteractionResult(Pawn pawn)
    {
        SampleComp?.InteractionResult(pawn);
    }
}