using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻骑士职位Def
/// </summary>
public class ResidentKnightRoleDef : Def
{
    private static readonly Type DefaultRoleWorkerClass = typeof(ResidentKnightRoleDef);

    public Type roleWorkerClass = DefaultRoleWorkerClass;

    private ResidentKnightRoleWorker roleWorker;
    public ResidentKnightRoleWorker RoleWorker => roleWorker ??= (ResidentKnightRoleWorker)Activator.CreateInstance(roleWorkerClass, this);

    [MustTranslate]
    public List<string> customDescriptions;

    /// <summary>
    /// UI图标
    /// </summary>
    public PathedTexture2D iconTexture;

    /// <summary>
    /// 显示优先级
    /// </summary>
    public int displyPriority = 100;

    /// <summary>
    /// 职位变更冷却时间（Day）
    /// </summary>
    public int positionChangeCDDays = 10;

    /// <summary>
    /// 给予担任该职位的Pawn的Ability
    /// </summary>
    public AbilityDef roleAbility;
    /// <summary>
    /// 给予担任该职位的Pawn的Hediff
    /// </summary>
    public HediffDef roleHediff;

    /// <summary>
    /// Stat偏移修正
    /// 修正是针对全体殖民者的，而非担任该职位的<see cref="Pawn"/>
    /// </summary>
    public List<RimWorld.StatModifier> statOffsets;
    /// <summary>
    /// Stat系数修正
    /// 修正是针对全体殖民者的，而非担任该职位的<see cref="Pawn"/>
    /// </summary>
    public List<RimWorld.StatModifier> statFactors;

    public string GetRoleDetailDesc()
    {
        StringBuilder sb = new(64);
        sb.AppendLine(LabelCap);
        sb.AppendLine();
        sb.AppendLine(description);
        if (!customDescriptions.NullOrEmpty())
        {
            sb.AppendLine();
            foreach (string item in customDescriptions)
            {
                sb.AppendLine(item);
            }
        }

        return sb.ToString();
    }

}