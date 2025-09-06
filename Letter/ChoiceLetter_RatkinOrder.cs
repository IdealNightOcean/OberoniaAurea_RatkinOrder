using Verse;


namespace OberoniaAurea.RatkinOrder;

public abstract class ChoiceLetter_RatkinOrder : ChoiceLetter
{
    public RatkinOrder relatedOrder;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref relatedOrder, "relatedOrder");
    }

    public override void OpenLetter()
    {
        DiaNode diaNode = new(Text);
        diaNode.options.AddRange(Choices);
        Dialog_NodeTreeWithRatkinOrderInfo window = new(diaNode, relatedOrder, delayInteractivity: false, radioMode, title);
        Find.WindowStack.Add(window);
    }
}
