using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface ITalkAction
{
    Pawn TalkWith { get; }
    void TalkAction(Pawn talkWith, Pawn talker = null, bool canPostpone = true);
}