using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRoleDef : Def
{
    private static readonly Type DefaultRoleWorkerClass = typeof(ResidentKnightRoleDef);

    public Type roleWorkerClass = DefaultRoleWorkerClass;

    private ResidentKnightRoleWorker roleWorker;
    public ResidentKnightRoleWorker RoleWorker => roleWorker ??= (ResidentKnightRoleWorker)Activator.CreateInstance(roleWorkerClass, this);

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
    /// 根据职位【Def】提供不同的Stat修正
    /// 修正是针对全体殖民者的，而非担任该职位的<see cref="Pawn">
    /// </summary>
    public List<StatModifier> statOffsets;
    public List<StatModifier> statFactors;

}