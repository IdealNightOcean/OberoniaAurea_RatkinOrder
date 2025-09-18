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
        Pawn talkWith = talkAction?.TalkWith;
        if (talkWith is null || !talkWith.Spawned)
        {
            return IntVec3.Invalid;
        }
        IntVec3 result;
        if (nearOrderHall)
        {
            if (GlobalOrderInteractionManager.MainOrderCodePedestal?.Map == talkWith.Map)
            {
                IntVec3 searchRootPos = GlobalOrderInteractionManager.MainOrderCodePedestal?.Position ?? talkWith.Position;
                RCellFinder.TryFindRandomSpotJustOutsideColony(searchRootPos, talkWith.Map, talkWith, out result);
                return result;
            }
        }

        RCellFinder.TryFindRandomSpotJustOutsideColony(talkWith.Position, talkWith.Map, talkWith, out result);
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
