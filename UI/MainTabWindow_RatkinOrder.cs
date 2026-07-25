using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class MainTabWindow_RatkinOrder : MainTabWindow
{
    protected override float Margin => 0.0f;
    public override Vector2 RequestedTabSize => new(1463f, 919f);
    public override Vector2 InitialSize => new(1463f, 919f);

    public MainTabWindow_RatkinOrder() : base()
    {
        forcePause = true;
        draggable = false;
        resizeable = false;
        doCloseButton = false;
        doCloseX = false;

        layer = WindowLayer.Dialog;  //窗体层级
        doWindowBackground = false; //绘制泰南的界面背景
        drawShadow = false; //绘制主体界面阴影

        //声音
        //注：用的通讯台声音
        soundAppear = SoundDefOf.CommsWindow_Open;
        soundClose = SoundDefOf.CommsWindow_Close;
    }

    protected override void SetInitialSizeAndPosition()
    {
        Vector2 initialSize = InitialSize;
        windowRect = new Rect((UI.screenWidth - initialSize.x) / 2f, (UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
        windowRect = windowRect.Rounded();
    }
    public override void DoWindowContents(Rect inRect)
    {
        Rect reuseRect = default;

        Widgets.DrawWindowBackground(inRect);
        Rect mainRect = new(37f, 49f, 1388f, 862f);
    }

}