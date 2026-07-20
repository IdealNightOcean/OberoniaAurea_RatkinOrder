using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataDrawerBase<T> where T : UIDataBase
{
    public void Draw(Rect inRect, T drawData)
    {
        drawData.Refresh();
        DrawInner(inRect, drawData);
    }

    public abstract void DrawInner(Rect inRect, T drawData);

}