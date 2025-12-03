using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Window_LetterBoxSetting : Window
{
    public override Vector2 InitialSize => new(450f, 300f); // inRect大小
    private OrderLetterBox LetterBox { get; }
    public Window_LetterBoxSetting()
    {
        doCloseX = true; // 是否显示关闭按钮
        forcePause = true; // 是否强制暂停游戏
        preventCameraMotion = true; // 是否防止相机移动
        closeOnClickedOutside = true; // 点击窗口外部是否关闭窗口

        LetterBox = OrderLetterBox.Instance;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Listing_Standard listing_Rect = new()
        {
            ColumnWidth = inRect.width
        };
        listing_Rect.Begin(inRect);
        Text.Font = GameFont.Medium;
        listing_Rect.Label("OARO_LetterSetting_Title".Translate());
        Text.Font = GameFont.Small;
        listing_Rect.GapLine(12f);
        listing_Rect.CheckboxLabeled("OARO_LetterSetting_AutoTransNormal".Translate(), ref LetterBox.autoTransNormal);
        listing_Rect.CheckboxLabeled("OARO_LetterSetting_AutoTransOfficial".Translate(), ref LetterBox.autoTransOfficial);
        listing_Rect.CheckboxLabeled("OARO_LetterSetting_AutoTransUrgent".Translate(), ref LetterBox.autoTransUrgent);
        listing_Rect.End();
    }
}
