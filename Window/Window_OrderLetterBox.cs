using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

[StaticConstructorOnStartup]
public class Window_OrderLetterBox : OrderWindowBase
{
    public override Vector2 InitialSize => new(1316, 872);

    private Vector2 scrollPosition_letterList;
    private Vector2 scrollPosition_letterText;

    private int SelectedLetterIndex { get; set; }
    /// <summary>
    /// 当前选中的信件
    /// </summary>
    private OrderLetter CurLetter { get; set; }
    private string CurLetterDesc { get; set; }

    private List<OrderLetter> ArchivedLetters { get; }
    public Window_OrderLetterBox() : base()
    {
        ArchivedLetters = OrderLetterBox.Instance.ArchivedLetters;
        UnselectLetter();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect reusedRect = default;

        //主背景
        Rect mainRect = inRect;
        mainRect.xMin += 66f;
        mainRect.xMax -= 24f;
        GUI.DrawTexture(mainRect, mainBackground, ScaleMode.StretchToFill);

        Rect mainInnerRect = mainRect.ContractedBy(3f);

        //左上绶带
        reusedRect = new(mainInnerRect.xMin + 58f, mainInnerRect.yMin + 29f, 391f, 75f);
        GUI.DrawTexture(reusedRect, leftRibbon);

        //左侧上部大信封
        reusedRect = new(mainInnerRect.xMin + 196f, mainInnerRect.yMin + 18f, 115f, 76f);
        GUI.DrawTexture(reusedRect, leftBigLetter);

        //左侧主背景
        Rect leftMainRect = new(mainInnerRect.xMin + 46f, mainInnerRect.yMin + 112f, 414f, 593f);
        GUI.DrawTexture(leftMainRect, leftMainBackBackground);

        //左侧上部标题
        Rect leftInnerRect = leftMainRect.ContractedBy(3f);
        reusedRect = OARO_WindowUtility.CenterRectOnX(leftInnerRect, leftInnerRect.yMin + 3f, 74f, 32f);

        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Medium;
        Widgets.Label(reusedRect, "OARO_Inbox".Translate());
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        //左侧上部（标题 | 列表）分割线
        reusedRect = OARO_WindowUtility.CenterRectOnX(leftInnerRect, leftInnerRect.yMin + 42f, 358f, 3f);
        GUI.DrawTexture(reusedRect, leftCuttingLine);

        //左侧列表
        Rect leftListRect = new(leftInnerRect.xMin + 4f, reusedRect.yMax + 4f, 379f, 527f);
        DrawLetterList(leftListRect);

        //左侧下部按钮
        reusedRect = new(leftMainRect.xMin, leftMainRect.yMax + 2f, 414f, 92f);
        Text.Font = GameFont.Medium;
        if (OARO_WindowUtility.TextButtonImageDisableable(
            butRect: reusedRect,
            label: "OARO_Epistolize".Translate(),
            acceptance: false,
            baseTex: leftButtonBackground,
            downTex: leftButtonBackground_Down))
        {

        }
        Text.Font = GameFont.Small;

        //左下鸽子
        reusedRect = new(inRect.xMin, inRect.yMax - 446f, 126f, 449f);
        GUI.DrawTexture(reusedRect, leftPigeonReliefSculpture);

        //右侧右上关闭按钮
        reusedRect = new(mainRect.xMax - (15f + 38f), mainRect.yMin + 14f, 38f, 38f);
        if (Widgets.ButtonImage(reusedRect, rightUpClose))
        {
            Close();
        }
        //右侧右上设置按钮
        reusedRect = new(reusedRect.xMin - (10f + 38f), reusedRect.yMin, 38f, 38f);
        if (Widgets.ButtonImage(reusedRect, rightUpSetting))
        {
            Find.WindowStack.Add(new Window_LetterBoxSetting());
        }

        //右侧主背景
        reusedRect = new(leftMainRect.xMax + 16f, mainInnerRect.yMin + 63f, 705f, 733f);
        GUI.DrawTexture(reusedRect, rightMainBackBackground);

        Rect rightInnerRect = reusedRect.ContractedBy(3f);

        //右侧右上信封
        reusedRect = new(rightInnerRect.xMin + 35f, rightInnerRect.yMin + 31f, 78f, 69f);
        GUI.DrawTexture(reusedRect, rightUpLetter);

        //右侧上部信件标题
        reusedRect = new(reusedRect.xMax + 16f, rightInnerRect.yMin + 48f, 383f, 32f);
        Text.Font = GameFont.Medium;
        Widgets.LabelEllipses(reusedRect, CurLetter?.Label ?? "OARO_Letter_NoSelected".Translate());
        Text.Font = GameFont.Small;

        //右侧上部分割线
        reusedRect = new(rightInnerRect.xMax - (58f + 518f), rightInnerRect.yMin + 93f, 518f, 17f);
        GUI.DrawTexture(reusedRect, rightUpCuttingLine);

        //右侧信件主要显示区
        Rect letterTextRect = OARO_WindowUtility.CenterRectOnX(rightInnerRect, rightInnerRect.yMin + 130f, 564f, 408f);
        if (CurLetter is not null)
        {
            Text.Font = GameFont.Medium;
            Widgets.LabelScrollable(letterTextRect, CurLetter.Text, ref scrollPosition_letterText);
            Text.Font = GameFont.Small;
        }

        reusedRect = OARO_WindowUtility.CenterRect(letterTextRect, 342f, 367f);
        GUI.DrawTexture(reusedRect, rightCoatOfArms);

        //右侧下部分割线
        reusedRect = OARO_WindowUtility.CenterRectOnX(rightInnerRect, letterTextRect.yMax + 24f, 642f, 9f);
        GUI.DrawTexture(reusedRect, rightDownCuttingLine);

        //右侧下部信件绶带
        reusedRect = OARO_WindowUtility.CenterRectOnX(rightInnerRect, reusedRect.yMax + 10f, 652f, 130f);
        GUI.DrawTexture(reusedRect, rightRibbon);

        //右侧下部信件信息显示
        reusedRect = OARO_WindowUtility.CenterRectOnX(rightInnerRect, reusedRect.yMin + 10f, 340f, 113f);
        Text.Font = GameFont.Medium;
        Widgets.TextArea(reusedRect, CurLetterDesc, readOnly: true);
        Text.Font = GameFont.Small;

        /*
        //右上勋章
        reusedRect = new(inRect.xMax - 50f, inRect.yMin + 62f, 50f, 97f);
        GUI.DrawTexture(reusedRect, rightMedal);
        */
    }

    private void DrawLetterList(Rect inRect)
    {
        Rect viewRect = new(inRect.xMin, inRect.yMin, 374f, 68f);
        int listCount = Mathf.Min(200, ArchivedLetters.Count);
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
                if (SelectedLetterIndex == i)
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
        Rect reuseRect = OARO_WindowUtility.CenterRectOnY(inRect, inRect.xMin + 2f, 49f, 49f);
        if (index == SelectedLetterIndex)
        {
            GUI.DrawTexture(inRect, leftLetterEntry_Sel);
            Widgets.DrawBox(inRect);
            GUI.DrawTexture(reuseRect, leftLetterIcon_Open);
        }
        else
        {
            if (Mouse.IsOver(inRect))
            {
                GUI.DrawTexture(inRect, leftLetterEntry_HighLight);
            }
            else
            {
                GUI.DrawTexture(inRect, (index & 1) == 0 ? leftLetterEntry_Even : leftLetterEntry_Odd);
            }
            GUI.DrawTexture(reuseRect, leftLetterIcon_Close, ScaleMode.ScaleToFit);
        }

        reuseRect = Rect.MinMaxRect(reuseRect.xMax + 6f, inRect.yMin + 2f, inRect.xMax - 2f, inRect.yMax - 2f);
        Widgets.LabelEllipses(reuseRect, ArchivedLetters[index].Label);

        return Widgets.ButtonInvisible(inRect);
    }

    private void SelectLetter(int selIndex)
    {
        if (selIndex < 0 || selIndex > ArchivedLetters.Count)
        {
            UnselectLetter();
            return;
        }
        SelectedLetterIndex = selIndex;
        CurLetter = ArchivedLetters[SelectedLetterIndex];
        CurLetterDesc = CurLetter.GetLetterDesc();
    }

    private void UnselectLetter()
    {
        SelectedLetterIndex = -1;
        CurLetter = null;
        CurLetterDesc = GetEmptyLetterDesc();

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

    private static readonly Texture2D mainBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_MainBackground");

    private static readonly Texture2D leftRibbon = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftRibbon");
    private static readonly Texture2D leftBigLetter = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftBigLetter");
    private static readonly Texture2D leftMainBackBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftMainBackBackground");
    private static readonly Texture2D leftCuttingLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftCuttingLine");
    private static readonly Texture2D leftLetterIcon_Close = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterIcon_Close");
    private static readonly Texture2D leftLetterIcon_Open = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterIcon_Open");
    private static readonly Texture2D leftLetterEntry_Odd = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_Odd");
    private static readonly Texture2D leftLetterEntry_Even = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_Even");
    private static readonly Texture2D leftLetterEntry_HighLight = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_HighLight");
    private static readonly Texture2D leftLetterEntry_Sel = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftLetterEntry_Sel");
    private static readonly Texture2D leftButtonBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftButtonBackground");
    private static readonly Texture2D leftButtonBackground_Down = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftButtonBackground_Down");
    private static readonly Texture2D leftPigeonReliefSculpture = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_LeftPigeonReliefSculpture");

    private static readonly Texture2D rightUpClose = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpClose");
    private static readonly Texture2D rightUpSetting = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpSetting");
    private static readonly Texture2D rightMainBackBackground = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightMainBackBackground");
    private static readonly Texture2D rightUpLetter = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpLetter");
    private static readonly Texture2D rightUpCuttingLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightUpCuttingLine");
    private static readonly Texture2D rightCoatOfArms = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightCoatOfArms");
    private static readonly Texture2D rightDownCuttingLine = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightDownCuttingLine");
    private static readonly Texture2D rightRibbon = ContentFinder<Texture2D>.Get("UI/LetterBox/OARO_RightRibbon");
}