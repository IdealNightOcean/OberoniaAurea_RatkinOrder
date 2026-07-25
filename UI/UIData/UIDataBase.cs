namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UIDataBase
{
    public bool IsReady { get; protected set; }

    public void MarkDirty() => IsReady = false;

    public void Refresh()
    {
        if (!IsReady)
        {
            RefreshInner();
            IsReady = true;
        }
    }

    protected abstract void RefreshInner();
}