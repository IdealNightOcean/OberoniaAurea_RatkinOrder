using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ChoiceLetter_RatkinOrder : ChoiceLetter
{
    public RatkinOrder RelatedOrder;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            yield return Option_Close;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref RelatedOrder, nameof(RelatedOrder));
    }

    public override void OpenLetter()
    {
        DiaNode diaNode = new(Text);
        diaNode.options.AddRange(Choices);
        Dialog_NodeTreeWithRatkinOrderInfo window = new(diaNode, RelatedOrder, delayInteractivity: false, radioMode, title);
        Find.WindowStack.Add(window);
    }
}
