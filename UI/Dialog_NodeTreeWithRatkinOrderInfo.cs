using OberoniaAurea.RatkinOrder.Utility;
using RimWorld;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class Dialog_NodeTreeWithRatkinOrderInfo : Dialog_NodeTree
{
    private RatkinOrder RatkinOrder { get; }

    private const float RelatedRatkinOrderInfoSize = 79f;

    public Dialog_NodeTreeWithRatkinOrderInfo(DiaNode nodeRoot, RatkinOrder ratkinOrder, bool delayInteractivity = false, bool radioMode = false, string title = null)
        : base(nodeRoot, delayInteractivity, radioMode, title)
    {
        RatkinOrder = ratkinOrder;
        if (ratkinOrder is not null)
        {
            minOptionsAreaHeight = 60f;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        base.DoWindowContents(inRect);
        if (RatkinOrder is not null)
        {
            float curY = inRect.height - RelatedRatkinOrderInfoSize;
            DrawRelatedOrderInfo(inRect, RatkinOrder, ref curY);
        }
    }

    private static void DrawRelatedOrderInfo(Rect rect, RatkinOrder ratkinOrder, ref float curY)
    {
        Text.Anchor = TextAnchor.LowerRight;
        curY += 10f;
        EsteemHandler.RelationshipKind orderRelationship = ratkinOrder.Relationship;
        string text = ratkinOrder.NameColored.CapitalizeFirst() + "\n" + "OARO_Esteem".Translate().CapitalizeFirst() + ": " + ratkinOrder.Esteem;
        GUI.color = Color.gray;
        Rect textRect = new(rect.x, curY, rect.width, Text.CalcHeight(text, rect.width));
        Widgets.Label(textRect, text);
        curY += textRect.height;
        GUI.color = orderRelationship.GetColor();
        Rect rect3 = new(textRect.x, curY - 7f, textRect.width, 25f);
        Widgets.Label(rect3, orderRelationship.GetLabel());
        curY += rect3.height;
        GUI.color = Color.white;
        GenUI.ResetLabelAlign();
    }
}
