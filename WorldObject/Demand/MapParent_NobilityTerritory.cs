using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 叛乱镇压 - 贵族领地 攻击时的地图
/// </summary>
public sealed class MapParent_NobilityTerritory : MapParent_Enterable
{
    public enum AssaultType
    {
        /// <summary>
        /// 玩家被突袭
        /// </summary>
        BePounced,
        /// <summary>
        /// 正常袭击
        /// </summary>
        Normal,
        /// <summary>
        /// 敌人被突袭
        /// </summary>
        Pounce,
        /// <summary>
        /// 敌人被致命突袭
        /// </summary>
        DeadlyPounce
    }

    public WorldObject_NobilityTerritory Parent;
    public AssaultType AssaultTypeValue;
    public bool BranchJoin;

    private bool succeeded;

    public void InitRaidInfo(WorldObject_NobilityTerritory parent, bool playerInitiated, bool branchJoin)
    {
        Parent = parent;
        BranchJoin = branchJoin;
        if (playerInitiated)
        {
            AssaultTypeValue = parent.Osmolity > 0.999f ? AssaultType.DeadlyPounce : Rand.Chance(parent.Osmolity) ? AssaultType.Pounce : AssaultType.Normal;
        }
        else
        {
            AssaultTypeValue = AssaultType.BePounced;
        }
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        bool result = base.ShouldRemoveMapNow(out _);
        alsoRemoveWorldObject = result;
        return result;
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();
        try
        {
            LetterDef letterDef = AssaultTypeValue switch
            {
                AssaultType.BePounced => LetterDefOf.ThreatBig,
                AssaultType.Normal => LetterDefOf.NeutralEvent,
                _ => LetterDefOf.PositiveEvent
            };

            Find.LetterStack.ReceiveLetter(
                label: $"OARO_NobilityTerritory_AssaultLabel_{AssaultTypeValue}".Translate(),
                text: $"OARO_NobilityTerritory_AssaultText_{AssaultTypeValue}".Translate(Parent.NobilityName),
                textLetterDef: letterDef,
                lookTargets: this,
                quest: Parent.AssociatedQuest);

            IncidentParms parms = new()
            {
                target = Map,
                faction = Faction,
                forced = true
            };
            OberoniaAurea_Frame.Utility.OAFrame_MiscUtility.AddNewQueuedIncident(OARO_ModDefOf.OARO_RaidNobilityTerritory, delayTicks: 60, parms);
        }
        catch (Exception ex)
        {
            ModUtility.LogExceptionError(ex,
                errorDesc: "generating Nobility Territory assault map.",
                typeName: nameof(MapParent_NobilityTerritory),
                methodName: nameof(PostMapGenerate),
                needStackTrace: true);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Parent, "Parent");
        Scribe_Values.Look(ref AssaultTypeValue, "AssaultTypeValue");
        Scribe_Values.Look(ref BranchJoin, "BranchJoin");

        Scribe_Values.Look(ref succeeded, "succeeded", defaultValue: false);
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!succeeded && HasMap && !GenHostility.AnyHostileActiveThreatToPlayer(Map, countDormantPawnsAsHostile: true))
        {
            succeeded = true;
            forceRemoveWorldObjectWhenMapRemoved = true;
            Parent?.Notify_AssaultEnd(true);
        }
    }

    public override void Destroy()
    {
        if (!succeeded)
        {
            Parent?.Notify_AssaultEnd(false);
        }
        base.Destroy();
    }
}
