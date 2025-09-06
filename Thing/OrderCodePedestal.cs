using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class OrderCodePedestal : ThingWithComps
{
    private bool isMainPedestal;

    [Unsaved] private Room cachedRoom;
    [Unsaved] private int cachedOrderHallLevel;

    [Unsaved] private int ticksToNextRoomCheck;
    [Unsaved] private int nextLevelCheckTick;

    public Room CachedRoom => cachedRoom;
    public int CachedOrderHallLevel
    {
        get
        {
            if (!isMainPedestal)
            {
                return 0;
            }
            if (cachedOrderHallLevel <= 0 || Find.TickManager.TicksGame > nextLevelCheckTick)
            {
                cachedOrderHallLevel = OrderHallUtility.GetOrderHallLevel(cachedRoom);
                nextLevelCheckTick = Find.TickManager.TicksGame + 2500;
            }
            return cachedOrderHallLevel;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref isMainPedestal, "isMainPedestal", defaultValue: false);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        RecheckMainPedestalState();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (isMainPedestal)
        {
            TryUnsetAsMainPedestal();

            if (BeingTransportedOnGravship)
            {
                isMainPedestal = true;
            }
        }
        base.DeSpawn(mode);
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (isMainPedestal && (ticksToNextRoomCheck -= delta) <= 0)
        {
            ticksToNextRoomCheck = 250;
            Room curRoom = this.GetRoom();
            if (curRoom != cachedRoom)
            {
                cachedRoom = curRoom;
                cachedOrderHallLevel = OrderHallUtility.GetOrderHallLevel(cachedRoom);
                nextLevelCheckTick = Find.TickManager.TicksGame + 2500;
            }
        }
    }

    public override void PostSwapMap()
    {
        base.PostSwapMap();
        if (Spawned)
        {
            RecheckMainPedestalState();
        }
    }

    public int GetNewestHallLevel()
    {
        if (!isMainPedestal)
        {
            return 0;
        }
        cachedRoom = this.GetRoom();
        ticksToNextRoomCheck = 250;
        cachedOrderHallLevel = OrderHallUtility.GetOrderHallLevel(cachedRoom);
        nextLevelCheckTick = Find.TickManager.TicksGame + 2500;
        return cachedOrderHallLevel;
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

        if (isMainPedestal)
        {
            Command_Action command_UnsetAsMain = new()
            {
                action = delegate { TryUnsetAsMainPedestal(); }
            };
            yield return command_UnsetAsMain;

            Command_Action command_RecheckHallLevel = new()
            {
                action = delegate
                {
                    GetNewestHallLevel();
                }
            };
            yield return command_RecheckHallLevel;
        }
        else
        {
            Command_Action command_SetAsMain = new()
            {
                action = delegate { TrySetAsMainPedestal(replaceCur: false); }
            };

            Command_Action command_SetAsOrReplaceMain = new()
            {
                action = delegate { TrySetAsMainPedestal(replaceCur: true); }
            };

            yield return command_SetAsMain;
            yield return command_SetAsOrReplaceMain;
        }
    }

    private void RecheckMainPedestalState()
    {
        if (isMainPedestal)
        {
            TrySetAsMainPedestal(replaceCur: false);
        }
        else
        {
            UnsetAsMainPedestal();
        }
    }

    public bool TrySetAsMainPedestal(bool replaceCur)
    {
        if (OrderInteractionHandler.Instance.SetMainOrderCodePedestal(this, replaceCur))
        {
            isMainPedestal = true;
            cachedRoom = this.GetRoom();
            ticksToNextRoomCheck = 250;
            nextLevelCheckTick = -1;
            return true;
        }
        else
        {
            UnsetAsMainPedestal();
            return false;
        }
    }

    public void Notify_MainReplacedByOther()
    {
        UnsetAsMainPedestal();
    }

    private void TryUnsetAsMainPedestal()
    {
        UnsetAsMainPedestal();
        OrderInteractionHandler.Instance.Notify_MainOrderCodePedestalUnset(this);
    }

    private void UnsetAsMainPedestal()
    {
        isMainPedestal = false;
        cachedRoom = null;
        cachedOrderHallLevel = 0;
        ticksToNextRoomCheck = 250;
        nextLevelCheckTick = int.MaxValue;
    }

}