using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻骑士职位的功能类，必须实现一个只接受 <see cref="ResidentKnightRoleDef"/> 参数的构造函数
/// </summary>
public class ResidentKnightRoleWorker(ResidentKnightRoleDef def)
{
    public readonly ResidentKnightRoleDef Def = def;

    /// <summary>
    /// 根据该职位的【Pawn】提供不同的Stat修正
    /// 修正是针对殖民者的，而非担任该职位的<see cref="Pawn"/>
    /// </summary>
    /// <param name="rolePawn">担任该职位的Pawn</param>
    public virtual IEnumerable<RimWorld.StatModifier> RoleStatOffsets(Pawn rolePawn) { return null; }
    public virtual IEnumerable<RimWorld.StatModifier> RoleStatFactors(Pawn rolePawn) { return null; }

    public virtual void PostActiveRole(Pawn rolePawn)
    {
        if (Def.roleAbility is not null)
        {
            rolePawn.abilities.GainAbility(Def.roleAbility);
        }
        if (Def.roleHediff is not null)
        {
            rolePawn.health.AddHediff(Def.roleHediff);
        }

        QuestUtility.SendQuestTargetSignals(rolePawn.questTags, "AssignedRole", rolePawn.Named(KeyLibrary_FormatArgName.SUBJECT), Def.Named("ROLE"));
    }

    public virtual void PostDeactiveRole(Pawn rolePawn)
    {
        if (Def.roleAbility is not null)
        {
            rolePawn.abilities.RemoveAbility(Def.roleAbility);
        }
        if (Def.roleHediff is not null)
        {
            rolePawn.RemoveFirstHediffOfDef(Def.roleHediff);
        }

        QuestUtility.SendQuestTargetSignals(rolePawn.questTags, "UnassignedRole", rolePawn.Named(KeyLibrary_FormatArgName.SUBJECT), Def.Named("ROLE"));
    }
}
