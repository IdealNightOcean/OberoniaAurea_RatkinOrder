using UnityEngine;

namespace OberoniaAurea.RatkinOrder;

public abstract class DevWindowBase : Verse.Window
{
    protected Vector2 scrollPosition;
    protected float viewRectHeight;

    public override Vector2 InitialSize => new(550f, 750f);

    public DevWindowBase()
    {
        doCloseX = true;
        draggable = true;
    }
}
