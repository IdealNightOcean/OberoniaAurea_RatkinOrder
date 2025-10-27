using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderCodePedestal : ThingWithComps
{
    public bool IsMainPedestal => this == OrderHallHandler.MainOrderCodePedestal;

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (IsMainPedestal)
        {
            if (BeingTransportedOnGravship)
            {
                OrderHallHandler.OnPedestalChange();
            }
            else
            {
                OrderHallHandler.TryUnsetMainPedestal(this);
            }
        }
        base.DeSpawn(mode);
    }

    public override void PostSwapMap()
    {
        base.PostSwapMap();
        if (Spawned && IsMainPedestal)
        {
            OrderHallHandler.OnPedestalChange();
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
            Command_Action command_UnsetAsMain = new()
            {
                defaultLabel = "OARO_CodePedestal_Unset".Translate(),
                defaultDesc = "OARO_CodePedestal_UnsetDesc".Translate(),
                action = delegate { OrderHallHandler.TryUnsetMainPedestal(this); }
            };
            yield return command_UnsetAsMain;

            Command_Action command_RecheckHallLevel = new()
            {
                defaultLabel = "OARO_CodePedestal_RecheckHallLevel".Translate(),
                defaultDesc = "OARO_CodePedestal_RecheckHallLevelDesc".Translate(),
                action = OrderHallHandler.OnPedestalChange
            };
            yield return command_RecheckHallLevel;
        }
        else
        {
            Command_Action command_SetAsMain = new()
            {
                defaultLabel = "OARO_CodePedestal_SetAsMain".Translate(),
                defaultDesc = "OARO_CodePedestal_SetAsMainDesc".Translate(),
                action = delegate { OrderHallHandler.TrySetMainPedestal(this, replaceCur: false); }
            };

            Command_Action command_SetAsOrReplaceMain = new()
            {
                defaultLabel = "OARO_CodePedestal_ForceSetAsMain".Translate(),
                defaultDesc = "OARO_CodePedestal_ForceSetAsMainDesc".Translate(),
                action = delegate { OrderHallHandler.TrySetMainPedestal(this, replaceCur: true); }
            };

            yield return command_SetAsMain;
            yield return command_SetAsOrReplaceMain;
        }
    }
}