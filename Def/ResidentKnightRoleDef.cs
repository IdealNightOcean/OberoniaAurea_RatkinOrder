using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ResidentKnightRoleDef : Def
{
    private static readonly Type DefaultRoleWorkerClass = typeof(ResidentKnightRoleDef);
    private static readonly ResidentKnightRoleWorker DefaultRoleWorker = new();

    public Type roleWorkerClass = DefaultRoleWorkerClass;

    private ResidentKnightRoleWorker roleWorker;
    public ResidentKnightRoleWorker RoleWorker => roleWorker ??= (roleWorkerClass == DefaultRoleWorkerClass) ? DefaultRoleWorker : (ResidentKnightRoleWorker)Activator.CreateInstance(roleWorkerClass);


    public int displyPriority = 100;

    public int positionChangeCDDays = 10;

    public List<StatModifier> statOffsets;

    public List<StatModifier> statFactors;
}