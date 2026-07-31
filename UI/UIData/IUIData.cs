namespace OberoniaAurea.RatkinOrder.UI;

public interface IUIData
{
    bool IsReady { get; }
    bool IsValid { get; }

    void MarkDirty();
    void Refresh();
}
