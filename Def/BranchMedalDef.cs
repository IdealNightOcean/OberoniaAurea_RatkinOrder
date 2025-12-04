using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class BranchMedalDef : Def
{
    private static readonly Type defaultBuffWorkerClass = typeof(BranchMedalBuffWorker);

    /// <summary>印记专注任务类型</summary>
    /// <remarks>- 会在 <see cref="Branch"/> 初始化时设置 <see cref="BranchTaskHandler.FocusedTaskType"/></remarks>
    public BranchTaskType focusedTaskType;

    /// <summary>
    /// 印记颜色
    /// </summary>
    public Color color;

    /// <summary>
    /// 印记Buff功能类
    /// </summary>
    protected Type buffWorkerClass = defaultBuffWorkerClass;
    private BranchMedalBuffWorker buffWorker;
    public BranchMedalBuffWorker BuffWorker => buffWorker ??= (BranchMedalBuffWorker)Activator.CreateInstance(buffWorkerClass, args: [this]);

    /// <summary>
    /// 印记背景颜色
    /// </summary>
    public Color backgroundColor;
    private Texture2D backgroundTexture;
    /// <summary>
    /// 印记背景图标，颜色使用 <see cref="backgroundColor"/>
    /// </summary>
    public Texture2D BackgroundTexture => backgroundTexture ??= SolidColorMaterials.NewSolidColorTexture(backgroundColor);


    /// <summary>
    /// 印记图标
    /// </summary>
    public PathedTexture2DWithExpanded iconTexture;

    public PathedTexture2D jointPatrolEntryBackgroundTexture;

    public PathedTexture2D jointPatrolEntryShadeTexture;

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