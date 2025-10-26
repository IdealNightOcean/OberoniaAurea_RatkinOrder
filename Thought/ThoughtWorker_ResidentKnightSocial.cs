using RimWorld;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class ThoughtWorker_KnightPersonalitySocial : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
    {
        if (!p.Faction.IsPlayerSafe() || otherPawn.Faction.IsPlayerSafe())
        {
            return ThoughtState.Inactive;
        }

        if (KnightPawnsManager.TryGetKnightRecord(p, out KnightRecord pRecord)
          && KnightPawnsManager.TryGetKnightRecord(otherPawn, out KnightRecord otherRecord))
        {
            if (IsResonateKnightPersonality(pRecord.Personality, otherRecord.Personality))
            {
                return ThoughtState.ActiveAtStage(1);
            }
            else
            {
                return ThoughtState.ActiveAtStage(0);
            }
        }

        return ThoughtState.Inactive;
    }

    /// <summary>
    /// 是否为相互共鸣个性
    /// </summary>
    private static bool IsResonateKnightPersonality(KnightRecord.PersonalityType personality, KnightRecord.PersonalityType other)
    {
        return personality switch
        {
            KnightRecord.PersonalityType.None => false,
            KnightRecord.PersonalityType.Courage => (other & (KnightRecord.PersonalityType.Tenacity | KnightRecord.PersonalityType.Oath)) != 0,
            KnightRecord.PersonalityType.Tenacity => (other & (KnightRecord.PersonalityType.Courage | KnightRecord.PersonalityType.Compassion)) != 0,
            KnightRecord.PersonalityType.Compassion => (other & (KnightRecord.PersonalityType.Tenacity | KnightRecord.PersonalityType.Justice)) != 0,
            KnightRecord.PersonalityType.Oath => (other & (KnightRecord.PersonalityType.Courage | KnightRecord.PersonalityType.Justice)) != 0,
            KnightRecord.PersonalityType.Justice => (other & (KnightRecord.PersonalityType.Compassion | KnightRecord.PersonalityType.Oath)) != 0,
            _ => false,
        };
    }
}