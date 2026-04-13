using Verse;

namespace OberoniaAurea.RatkinOrder;

public class RoomRoleWorker_RatkinOrderHall : RoomRoleWorker
{
    public override float GetScore(Room room)
    {
        return OrderStationHandler.Instance.OrderHallRoom == room ? 99999f : 0f;
    }
}