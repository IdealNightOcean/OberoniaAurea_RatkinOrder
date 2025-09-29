using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public static class LetterBoxTexture
{
    public static readonly Texture2D MainBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_MainBackground");

    public static readonly Texture2D LeftRibbon = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftRibbon");
    public static readonly Texture2D LeftBigLetter = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftBigLetter");
    public static readonly Texture2D LeftMainBackBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftMainBackBackground");
    public static readonly Texture2D LeftCuttingLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftCuttingLine");
    public static readonly Texture2D LeftLetterIcon_Close = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterIcon_Close");
    public static readonly Texture2D LeftLetterIcon_Open = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterIcon_Open");
    public static readonly Texture2D LeftLetterEntry_Odd = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_Odd");
    public static readonly Texture2D LeftLetterEntry_Even = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_Even");
    public static readonly Texture2D LeftLetterEntry_HighLight = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_HighLight");
    public static readonly Texture2D LeftLetterEntry_Sel = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_Sel");
    public static readonly Texture2D LeftButtonBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftButtonBackground");
    public static readonly Texture2D LeftButtonBackground_Down = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftButtonBackground_Down");
    public static readonly Texture2D LeftPigeonReliefSculpture = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftPigeonReliefSculpture");

    public static readonly Texture2D RightUpClose = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpClose");
    public static readonly Texture2D RightUpSetting = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpSetting");
    public static readonly Texture2D RightMainBackBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightMainBackBackground");
    public static readonly Texture2D RightUpLetter = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpLetter");
    public static readonly Texture2D RightUpCuttingLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpCuttingLine");
    public static readonly Texture2D RightCoatOfArms = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightCoatOfArms");
    public static readonly Texture2D RightDownCuttingLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightDownCuttingLine");
    public static readonly Texture2D RightRibbon = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightRibbon");
    public static readonly Texture2D RightMedal = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightMedal");
}


public class Window_OrderLetterBox : Window
{
    protected override float Margin => 0.0f;
    public override Vector2 InitialSize => new(1316, 872);

    private Vector2 scrollPosition_letterList;

    private int selectedLetterIndex = -1;
    private OrderLetter curLetter = null; // 当前选中的信件
    private string curLetterDesc;

    private readonly List<OrderLetter> archivedLetters = [];

    public Window_OrderLetterBox() : base()
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

        archivedLetters = OrderLetterBox.Instance.ArchivedLetters;
        UnselectLetter();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect reuseRect = default;

        //主背景
        Rect mainRect = inRect;
        mainRect.xMin += 66f;
        mainRect.xMax -= 24f;
        GUI.DrawTexture(mainRect, LetterBoxTexture.MainBackground, ScaleMode.StretchToFill);

        Rect mainInnerRect = mainRect.ContractedBy(3f);

        //左上绶带
        reuseRect = new(mainInnerRect.xMin + 58f, mainInnerRect.yMin + 29f, 391f, 75f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.LeftRibbon);

        //左侧上部大信封
        reuseRect = new(mainInnerRect.xMin + 196f, mainInnerRect.yMin + 18f, 115f, 76f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.LeftBigLetter);

        //左侧主背景
        Rect leftMainRect = new(mainInnerRect.xMin + 46f, mainInnerRect.yMin + 112f, 414f, 593f);
        GUI.DrawTexture(leftMainRect, LetterBoxTexture.LeftMainBackBackground);

        //左侧上部标题
        Rect leftInnerRect = leftMainRect.ContractedBy(3f);
        reuseRect = new(CenterMinCoords(leftInnerRect.x, leftInnerRect.width, 74f), leftInnerRect.yMin + 3f, 74f, 32f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reuseRect, "OARO_Inbox".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        //左侧上部（标题 | 列表）分割线
        reuseRect = new(CenterMinCoords(leftInnerRect.x, leftInnerRect.width, 358f), leftInnerRect.yMin + 42f, 358f, 3f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.LeftCuttingLine);

        //左侧列表
        Rect leftListRect = new(leftInnerRect.xMin + 4f, reuseRect.yMax + 4f, 379f, 527f);
        DrawLetterList(leftListRect);

        //左侧下部按钮
        reuseRect = new(leftMainRect.xMin, leftMainRect.yMax + 2f, 414f, 92f);
        Text.Font = GameFont.Medium;
        if (ButtonImageWithLabel(reuseRect, "OARO_Epistolize".Translate(), LetterBoxTexture.LeftButtonBackground, LetterBoxTexture.LeftButtonBackground_Down))
        {

        }
        Text.Font = GameFont.Small;

        //左下鸽子
        reuseRect = new(inRect.xMin, inRect.yMax - 446f, 126f, 449f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.LeftPigeonReliefSculpture);

        //右侧右上关闭按钮
        reuseRect = new(mainRect.xMax - (15f + 38f), mainRect.yMin + 14f, 38f, 38f);
        if (Widgets.ButtonImage(reuseRect, LetterBoxTexture.RightUpClose))
        {
            Close();
        }
        //右侧右上设置按钮
        reuseRect = new(reuseRect.xMin - (10f + 38f), reuseRect.yMin, 38f, 38f);
        if (Widgets.ButtonImage(reuseRect, LetterBoxTexture.RightUpSetting))
        {
            Find.WindowStack.Add(new Window_LetterBoxSetting());
        }

        //右侧主背景
        reuseRect = new(leftMainRect.xMax + 16f, mainInnerRect.yMin + 63f, 705f, 733f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightMainBackBackground);

        Rect rightInnerRect = reuseRect.ContractedBy(3f);

        //右侧右上信封
        reuseRect = new(rightInnerRect.xMin + 35f, rightInnerRect.yMin + 31f, 78f, 69f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightUpLetter);

        //右侧上部信件标题
        reuseRect = new(reuseRect.xMax + 16f, rightInnerRect.yMin + 48f, 383f, 32f);
        Text.Font = GameFont.Medium;
        Widgets.LabelEllipses(reuseRect, curLetter?.Label ?? "OARO_Letter_NoSelected".Translate());
        Text.Font = GameFont.Small;

        //右侧上部分割线
        reuseRect = new(rightInnerRect.xMax - (58f + 518f), rightInnerRect.yMin + 93f, 518f, 17f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightUpCuttingLine);

        //右侧信件主要显示区
        Rect letterTextRect = new(CenterMinCoords(rightInnerRect.x, rightInnerRect.width, 564f), rightInnerRect.yMin + 130f, 564f, 408f);
        if (curLetter is not null)
        {
            Text.Font = GameFont.Medium;
            Widgets.TextArea(letterTextRect, curLetter.Text, readOnly: true);
            Text.Font = GameFont.Small;
        }

        reuseRect = new(CenterMinCoords(letterTextRect.x, letterTextRect.width, 342f),
                        CenterMinCoords(letterTextRect.y, letterTextRect.height, 367f),
                        342f,
                        367f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightCoatOfArms);

        //右侧下部分割线
        reuseRect = new(CenterMinCoords(rightInnerRect.x, rightInnerRect.width, 642f), letterTextRect.yMax + 24f, 642f, 9f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightDownCuttingLine);

        //右侧下部信件绶带
        reuseRect = new(CenterMinCoords(rightInnerRect.x, rightInnerRect.width, 652f), reuseRect.yMax + 10f, 652f, 130f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightRibbon);

        //右侧下部信件信息显示
        reuseRect = new(CenterMinCoords(rightInnerRect.x, rightInnerRect.width, 340f), reuseRect.yMin + 10f, 340f, 113f);
        Text.Font = GameFont.Medium;
        Widgets.TextArea(reuseRect, curLetterDesc, readOnly: true);
        Text.Font = GameFont.Small;

        /*
        //右上勋章
        reuseRect = new(inRect.xMax - 50f, inRect.yMin + 62f, 50f, 97f);
        GUI.DrawTexture(reuseRect, LetterBoxTexture.RightMedal);
        */
    }

    private void DrawLetterList(Rect inRect)
    {
        Rect viewRect = new(inRect.xMin, inRect.yMin, 374f, 68f);
        int listCount = Mathf.Min(200, archivedLetters.Count);
        viewRect.height = listCount * (68f + 2f) - 2f;

        Widgets.BeginScrollView(inRect, ref scrollPosition_letterList, viewRect);

        float entryXMin = viewRect.xMin;
        float curEntryYMin = viewRect.yMin;
        Rect entryRect;

        Text.Anchor = TextAnchor.MiddleLeft;
        for (int i = 0; i < listCount; i++)
        {
            entryRect = new(entryXMin, curEntryYMin, 374f, 68f);
            if (DrawLetterEntry(entryRect, index: i))
            {
                if (selectedLetterIndex == i)
                {
                    UnselectLetter();
                }
                else
                {
                    SelectLetter(i);
                }
            }
            curEntryYMin += (2f + 68f);
        }
        Text.Anchor = TextAnchor.UpperLeft;

        Widgets.EndScrollView();
    }

    private bool DrawLetterEntry(Rect inRect, int index)
    {
        Rect reuseRect = new(inRect.xMin + 2f, CenterMinCoords(inRect.yMin, inRect.height, 49f), 49f, 49f);
        if (index == selectedLetterIndex)
        {
            GUI.DrawTexture(inRect, LetterBoxTexture.LeftLetterEntry_Sel);
            Widgets.DrawBox(inRect);
            GUI.DrawTexture(reuseRect, LetterBoxTexture.LeftLetterIcon_Open);
        }
        else
        {
            if (Mouse.IsOver(inRect))
            {
                GUI.DrawTexture(inRect, LetterBoxTexture.LeftLetterEntry_HighLight);
            }
            else
            {
                GUI.DrawTexture(inRect, (index & 1) == 0 ? LetterBoxTexture.LeftLetterEntry_Even : LetterBoxTexture.LeftLetterEntry_Odd);
            }
            GUI.DrawTexture(reuseRect, LetterBoxTexture.LeftLetterIcon_Close, ScaleMode.ScaleToFit);
        }

        reuseRect = Rect.MinMaxRect(reuseRect.xMax + 6f, inRect.yMin + 2f, inRect.xMax - 2f, inRect.yMax - 2f);
        Widgets.LabelEllipses(reuseRect, archivedLetters[index].Label);

        return Widgets.ButtonInvisible(inRect);
    }

    private void SelectLetter(int selIndex)
    {
        if (selIndex < 0 || selIndex > archivedLetters.Count)
        {
            UnselectLetter();
            return;
        }
        selectedLetterIndex = selIndex;
        curLetter = archivedLetters[selectedLetterIndex];
        curLetterDesc = curLetter.GetLetterDesc();
    }

    private void UnselectLetter()
    {
        selectedLetterIndex = -1;
        curLetter = null;
        curLetterDesc = GetEmptyLetterDesc();

        static string GetEmptyLetterDesc()
        {
            StringBuilder sb = new("OARO_Letter_LetterSender".Translate());
            sb.Append("OARO_Letter_UnkownSender".Translate());
            sb.AppendInNewLine("OARO_Letter_RelatedOrder".Translate());
            sb.Append("敬请期待");
            sb.AppendInNewLine("OARO_Letter_RelatedThings".Translate());
            sb.Append("None".Translate());
            sb.AppendInNewLine("OARO_Letter_SendTime".Translate());
            sb.Append("None".Translate());
            return sb.ToString();
        }
    }

    /// <summary>
    /// Rect居中对应最小坐标
    /// </summary>
    /// <param name="outerMinCoords">做标准Rect对应最小坐标</param>
    /// <param name="outerSize">做标准Rect尺寸</param>
    /// <param name="innerSize">被居中Rect尺寸</param>
    /// <returns>Rect居中对应最小坐标</returns>
    private static float CenterMinCoords(float outerMinCoords, float outerSize, float innerSize) => outerMinCoords + (outerSize - innerSize) * 0.5f;

    private static bool ButtonImage(Rect butRect, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        if (Mouse.IsOver(butRect))
        {
            GUI.DrawTexture(butRect, downTex);
        }
        else
        {
            GUI.DrawTexture(butRect, baseTex);
        }

        if (!tooltip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(butRect, tooltip);
        }

        return Widgets.ButtonInvisible(butRect, doMouseoverSound);
    }

    private static bool ButtonImageWithLabel(Rect butRect, string label, Texture2D baseTex, Texture2D downTex, bool doMouseoverSound = true, string tooltip = null)
    {
        bool result = ButtonImage(butRect, baseTex, downTex, doMouseoverSound, tooltip);

        TextAnchor anchor = Text.Anchor;
        Color color = GUI.color;
        bool wordWrap = Text.WordWrap;

        Text.Anchor = TextAnchor.MiddleCenter;
        if (butRect.height < Text.LineHeight * 2f)
        {
            Text.WordWrap = false;
        }

        Widgets.Label(butRect, label);

        Text.Anchor = anchor;
        GUI.color = color;
        Text.WordWrap = wordWrap;

        return result;
    }
}