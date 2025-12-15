using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Building_OrderCodePedestal : Building
{
    public bool IsMainPedestal => this == OrderHallHandler.Instance.MainOrderCodePedestal;

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (IsMainPedestal)
        {
            if (BeingTransportedOnGravship)
            {
                OrderHallHandler.Instance.RefreshCache();
            }
            else
            {
                OrderHallHandler.Instance.TryUnsetMainPedestal(this);
            }
        }
        base.DeSpawn(mode);
    }

    public override void PostSwapMap()
    {
        base.PostSwapMap();
        if (Spawned && IsMainPedestal)
        {
            OrderHallHandler.Instance.RefreshCache();
        }
    }

    public override void TickLong()
    {
        base.TickLong();
        if (!IsMainPedestal || !Spawned)
        {
            return;
        }

        _ = ResidentKnightsManager.Instance.BuffHediffStage;
        foreach (Pawn p in Map.mapPawns.FreeColonistsSpawned)
        {
            Hediff hediff = p.health.GetOrAddHediff(OARO_HediffDefOf.OARO_Hediff_ByResidentKnightBuff);
            HediffComp_Disappears disappearsComp = hediff?.TryGetComp<HediffComp_Disappears>();
            if (disappearsComp is not null)
            {
                disappearsComp.disappearsAfterTicks = 2000;
                disappearsComp.ticksToDisappear = 2000;
            }
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        if (!Spawned)
        {
            yield break;
        }

        if (IsMainPedestal)
        {
            Command_Action command_OpenOrderHallWin = new()
            {
                defaultLabel = "OARO_CodePedestal_OpenOrderHallWin".Translate(),
                defaultDesc = "OARO_CodePedestal_OpenOrderHallWinDesc".Translate(),
                action = delegate
                {
                    Find.Selector.Deselect(this);
                    Window_OrderHall orderHallWin = new(Map);
                    Find.WindowStack.Add(orderHallWin);
                }
            };
            yield return command_OpenOrderHallWin;

            Command_Action command_UnsetAsMain = new()
            {
                defaultLabel = "OARO_CodePedestal_Unset".Translate(),
                defaultDesc = "OARO_CodePedestal_UnsetDesc".Translate(),
                action = delegate { OrderHallHandler.Instance.TryUnsetMainPedestal(this); }
            };
            yield return command_UnsetAsMain;

            Command_Action command_RecheckHallLevel = new()
            {
                defaultLabel = "OARO_CodePedestal_RecheckHallLevel".Translate(),
                defaultDesc = "OARO_CodePedestal_RecheckHallLevelDesc".Translate(),
                action = OrderHallHandler.Instance.RefreshCache
            };
            yield return command_RecheckHallLevel;
        }
        else
        {
            Command_Action command_SetAsMain = new()
            {
                defaultLabel = "OARO_CodePedestal_SetAsMain".Translate(),
                defaultDesc = "OARO_CodePedestal_SetAsMainDesc".Translate(),
                action = delegate { OrderHallHandler.Instance.TrySetMainPedestal(this, replaceCur: false); }
            };

            Command_Action command_SetAsOrReplaceMain = new()
            {
                defaultLabel = "OARO_CodePedestal_ForceSetAsMain".Translate(),
                defaultDesc = "OARO_CodePedestal_ForceSetAsMainDesc".Translate(),
                action = delegate { OrderHallHandler.Instance.TrySetMainPedestal(this, replaceCur: true); }
            };

            yield return command_SetAsMain;
            yield return command_SetAsOrReplaceMain;
        }
    }
}