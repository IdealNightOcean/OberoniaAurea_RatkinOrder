using OberoniaAurea_Frame;
using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class CompPlagueSample : CompInteractWithThing
{
    private Quest quest;
    public Quest AssociatedQuest => quest;

    private WorldObject_PlagueVillage plagueVillage;

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

    public override string CompInspectStringExtra()
    {
        return "OARO_PlagueSample_Points".Translate(samplePoints.ToString("0.##"), MaxSamplePoints.ToString("0.##"));
    }

    public void InitSample(Quest quest, WorldObject_PlagueVillage plagueVillage, bool isStrangePlague)
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
        QuestUtility.SendQuestTargetSignals(parent.questTags, "");

        int medicineSkillLevel = pawn.GetSkillLevel(SkillDefOf.Medicine);
        if (quest is not null && plagueVillage is not null)
        {
            plagueVillage.CliquesManager?.AdjustCliqueWillingness(KeyLibrary_QuestCliqueKey.Doctor, 0.05f);
            float controlGain = 20f + 2f * medicineSkillLevel;
            if (isStrangePlague)
            {
                controlGain *= 2f;
            }
            Messages.Message("OARO_PlagueSample_Result".Translate(controlGain.ToString("0.##")), MessageTypeDefOf.PositiveEvent);
            plagueVillage?.AdjustPlagueControl(controlGain);
        }

        if (medicineSkillLevel < 10 && Rand.Chance(0.75f))
        {
            pawn.health.AddHediff(HediffDefOf.Plague);
            Messages.Message("OARO_PlagueSample_InfectioPlague".Translate(pawn), MessageTypeDefOf.NegativeEvent);
        }

        parent.SafeDestroy();
    }
}