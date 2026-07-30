

namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataBase
{
    public bool IsReady { get; protected set; }
    public abstract bool IsValid { get; }

    public void MarkDirty() => IsReady = false;

    public void Refresh()
    {
        if (!IsReady)
        {
            if (IsValid)
            {
                RefreshInner();
            }
            IsReady = true;
        }
    }

    protected abstract void RefreshInner();
}