namespace OberoniaAurea.RatkinOrder.UI;

public interface IUIData
{
    UIDataState DataState { get; }
    bool CanDraw { get; }

    void MarkDirty();
    void Refresh();
}
