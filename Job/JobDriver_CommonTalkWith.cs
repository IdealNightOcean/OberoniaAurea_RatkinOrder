using OberoniaAurea_Frame;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class JobDriver_CommonTalkWith : JobDriver_TalkWithAtOnce
{
    protected override void TalkAction(Pawn talker, Pawn talkWith)
    {
        if (talker.DestroyedOrNull() || talkWith.DestroyedOrNull())
        {
            return;
        }
        if (GameComponent_RatkinOrder.Instance.TalkActionHandler.TryGetValue(talkWith, out ITalkAction talkAction))
        {
            talkAction.TalkAction(talkWith: talkWith, talker: talker, canPostpone: true);
        }
    }
}