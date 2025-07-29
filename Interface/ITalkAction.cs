using Verse;

namespace OberoniaAurea.RatkinOrder;

public interface ITalkAction
{
    Pawn TalkWith { get; }
    void TalkAction(Pawn talker, Pawn talkWith);
}