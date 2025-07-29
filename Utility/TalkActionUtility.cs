using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using Verse.AI.Group;

namespace OberoniaAurea.RatkinOrder;

public static class TalkActionUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterTalkAction(this ITalkAction talkAction)
    {
        if (talkAction?.TalkWith is not null)
        {
            GameComponent_RatkinOrder.Instance.TalkActionHandler[talkAction.TalkWith] = talkAction;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DeregisterTalkAction(this ITalkAction talkAction)
    {
        if (talkAction?.TalkWith is not null)
        {
            DisableLordJobTalk(talkAction.TalkWith);
            GameComponent_RatkinOrder.Instance.TalkActionHandler.Remove(talkAction.TalkWith);
        }
    }

    public static IntVec3 GetTalkPawnWanderCenterCell(this ITalkAction talkAction, bool nearOrderHall)
    {
        if (talkAction?.TalkWith is null || !talkAction.TalkWith.Spawned)
        {
            return IntVec3.Invalid;
        }

        RCellFinder.TryFindRandomSpotJustOutsideColony(talkAction.TalkWith, out IntVec3 result);
        return result;
    }


    public static bool IsValidTalkActionRecord(KeyValuePair<Pawn, ITalkAction> pair)
    {
        if (pair.Key.DestroyedOrNull() || pair.Value is null)
        {
            return false;
        }
        Pawn p = pair.Key;
        if (p.DeadOrDowned || !p.Spawned)
        {
            return false;
        }
        if (!p.TryGetLord(out Lord lord) || lord.LordJob is not ILordJobWithTalk talkLordJob || talkLordJob.TalkablePawn != p)
        {
            return false;
        }
        return true;
    }

    public static void DisableLordJobTalk(Pawn pawn)
    {
        if (pawn.DestroyedOrNull())
        {
            return;
        }

        if (pawn.GetLord()?.LordJob is ILordJobWithTalk talkLordJob)
        {
            talkLordJob.DisableTalk();
        }
    }
}
