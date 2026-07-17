using UnityEngine;

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UICacheDrawerBase<T> where T : UICacheBase
{
    public void Draw(Rect inRect, T darwData)
    {
        darwData.Refresh();
        DrawInner(inRect, darwData);
    }

    public abstract void DrawInner(Rect inRect, T darwData);

}