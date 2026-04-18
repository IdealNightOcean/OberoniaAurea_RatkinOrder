using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RoomRoleWorker_RatkinOrderStation : RoomRoleWorker
{
    public override float GetScore(Room room)
    {
        return OrderStationHandler.Instance.OrderStationRoom == room ? 99999f : 0f;
    }
}