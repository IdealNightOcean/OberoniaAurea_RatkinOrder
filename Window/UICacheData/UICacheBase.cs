namespace OberoniaAurea.RatkinOrder.UI;

public abstract class UICacheBase
{
    public bool IsReady { get; protected set; }

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