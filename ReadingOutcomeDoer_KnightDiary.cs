using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BookOutcomeProperties_KnightDiary : BookOutcomeProperties
{
    public override Type DoerClass => typeof(ReadingOutcomeDoer_KnightDiary);
}

public class ReadingOutcomeDoer_KnightDiary : BookOutcomeDoer
{
    private SkillDef skillDef;
    public SkillDef SkillDef => skillDef;

    private int skillLevelCap = -1;
    private int SkillLevelCap
    {
        get
        {
            if (skillLevelCap < 0)
            {
                skillLevelCap = GetSkillLevelCap(Quality);
            }
            return skillLevelCap;
        }
    }

    private float skillXpGainPerTick = -1f;
    public float SkillXPGainPerTick
    {
        get
        {
            if (skillXpGainPerTick < 0f)
            {
                skillXpGainPerTick = GetSkillXPGainPerHour(Quality) / 2500f;
            }
            return skillXpGainPerTick;
        }
    }

    private float meditationPointsPerTick = -1f;
    public float MeditationPointsPerTick
    {
        get
        {
            if (meditationPointsPerTick < 0f)
            {
                meditationPointsPerTick = GetMeditationPointsPerHour(Quality) / 2500f;
            }
            return meditationPointsPerTick;
        }
    }

    [Unsaved] private Pawn cachedPawn;
    [Unsaved] private ResidentKnight cachedResident;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref skillDef, nameof(skillDef));
    }

    public override bool DoesProvidesOutcome(Pawn reader) => true;

    public override void OnBookGenerated(Pawn author = null)
    {
        base.OnBookGenerated(author);
        Book.SetJoyFactor(GetJoyGainFactor(Quality));
    }

    public override void OnReadingTick(Pawn reader, float factor)
    {
        base.OnReadingTick(reader, factor);

        int curSkillLevel = reader.skills.GetSkill(skillDef).GetLevel();
        if (curSkillLevel < SkillLevelCap)
        {
            reader.skills.GetSkill(skillDef).Learn(SkillXPGainPerTick * factor);
        }

        if (cachedPawn is null || cachedPawn != reader)
        {
            cachedPawn = reader;
            ResidentPawnsManager.Instance.TryGetKnightRecord(reader, out cachedResident);
        }

        if (cachedResident is not null)
        {
            cachedResident.MeditationPoints += (MeditationPointsPerTick * factor);
        }
    }

    /// <summary>
    /// 获取骑士日记的娱乐倍率
    /// </summary>
    private static float GetJoyGainFactor(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 0.3f,
            QualityCategory.Poor => 0.35f,
            QualityCategory.Normal => 0.4f,
            QualityCategory.Good => 0.45f,
            QualityCategory.Excellent => 0.5f,
            QualityCategory.Masterwork => 0.55f,
            QualityCategory.Legendary => 0.6f,
            _ => 0.3f
        };
    }

    /// <summary>
    /// 获取骑士日记的修行点数效果
    /// </summary>
    public static float GetMeditationPointsPerHour(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 15f,
            QualityCategory.Poor => 18f,
            QualityCategory.Normal => 21f,
            QualityCategory.Good => 24f,
            QualityCategory.Excellent => 27f,
            QualityCategory.Masterwork => 30f,
            QualityCategory.Legendary => 35f,
            _ => 15f
        };
    }

    /// <summary>
    /// 获取骑士日记的技能增长效果
    /// </summary>
    public static float GetSkillXPGainPerHour(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Awful => 0f,
            QualityCategory.Poor => 0f,
            QualityCategory.Normal => 150f,
            QualityCategory.Good => 210f,
            QualityCategory.Excellent => 270f,
            QualityCategory.Masterwork => 330f,
            QualityCategory.Legendary => 390f,
            _ => 0f
        };
    }

    /// <summary>
    /// 获取骑士日记的技能等级上限
    /// </summary>
    public static int GetSkillLevelCap(QualityCategory quality)
    {
        return quality switch
        {
            QualityCategory.Normal => 8,
            QualityCategory.Good => 10,
            QualityCategory.Excellent => 12,
            QualityCategory.Masterwork => 14,
            QualityCategory.Legendary => 16,
            _ => 0
        };
    }
}
