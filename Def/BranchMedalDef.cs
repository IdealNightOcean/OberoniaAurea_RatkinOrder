using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchMedalDef : Def
{
    private static readonly Type defaultBuffWorkerClass = typeof(BranchMedalBuffWorker);

    public BranchTaskType focusedTaskType;

    public Color color;

    protected Type buffWorkerClass = defaultBuffWorkerClass;
    private BranchMedalBuffWorker buffWorker;
    public BranchMedalBuffWorker BuffWorker => buffWorker ??= (BranchMedalBuffWorker)Activator.CreateInstance(buffWorkerClass, args: [this]);

    public Color backgroundColor;
    protected Texture2D backgroundTexture;
    public Texture2D BackgroundTexture => backgroundTexture ??= SolidColorMaterials.NewSolidColorTexture(backgroundColor);

    [NoTranslate]
    protected string iconPath;
    protected Texture2D iconTexture;
    public Texture2D IconTexture
    {
        get
        {
            if (iconTexture is null)
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    return null;
                }
                iconTexture = ContentFinder<Texture2D>.Get(iconPath);
            }
            return iconTexture;
        }
    }

    protected Texture2D expandingIconTexture;
    public Texture2D ExpandingIconTexture
    {
        get
        {
            if (expandingIconTexture is null)
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    return null;
                }
                expandingIconTexture = ContentFinder<Texture2D>.Get(iconPath + "_Expand");
            }
            return expandingIconTexture;
        }
    }
}


public class BranchMedalBuffWorker(BranchMedalDef def)
{
    protected readonly BranchMedalDef def = def;

    public virtual void AdjuestHediffBuffStage(HediffStage stage, bool isPrimary, int medalCount) { }

}

public class BranchMedalBuffWorker_Tenacity(BranchMedalDef def) : BranchMedalBuffWorker(def)
{
    public override void AdjuestHediffBuffStage(HediffStage stage, bool isPrimary, int medalCount)
    {
        stage.painFactor *= (isPrimary ? 0.85f : 0.95f);
    }
}

public class BranchMedalBuffWorker_Courage(BranchMedalDef def) : BranchMedalBuffWorker(def)
{
    public override void AdjuestHediffBuffStage(HediffStage stage, bool isPrimary, int medalCount)
    {
        stage.statOffsets.Add(new StatModifier()
        {
            stat = StatDefOf.MeleeHitChance,
            value = isPrimary ? 4f : 2f
        });
    }
}

public class BranchMedalBuffWorker_Rescue(BranchMedalDef def) : BranchMedalBuffWorker(def)
{
    public override void AdjuestHediffBuffStage(HediffStage stage, bool isPrimary, int medalCount)
    {
        stage.statOffsets.Add(new StatModifier()
        {
            stat = StatDefOf.MedicalTendSpeed,
            value = isPrimary ? 0.12f : 0.06f
        });
    }
}

public class BranchMedalBuffWorker_Justice(BranchMedalDef def) : BranchMedalBuffWorker(def)
{
    public override void AdjuestHediffBuffStage(HediffStage stage, bool isPrimary, int medalCount)
    {
        stage.statOffsets.Add(new StatModifier()
        {
            stat = StatDefOf.MoveSpeed,
            value = isPrimary ? 0.15f : 0.10f
        });
        stage.statOffsets.Add(new StatModifier()
        {
            stat = StatDefOf.WorkSpeedGlobal,
            value = isPrimary ? 0.05f : 0.03f
        });
    }
}