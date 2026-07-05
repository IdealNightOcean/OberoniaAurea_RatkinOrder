using RimWorld;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatWorker_MeditationPointDailyGain : StatWorker_MeditationPointBase
{
    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);
        if (CanApplyOn(pawn))
        {
            float pointGain = pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationBase);
            if (pointGain <= 0)
            {
                return 0f;
            }
            pointGain *= pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
            return pointGain;
        }
        return 0f;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        Pawn pawn = req.Pawn ?? (req.Thing as Pawn);
        if (CanApplyOn(pawn))
        {
            float pointBase = pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationBase);
            if (pointBase <= 0f)
            {
                return "StatsReport_MeditationPointDailyGain_BaseZero".Translate();
            }
            else
            {
                StringBuilder sb = new();
                sb.AppendLine("StatsReport_MeditationPointDailyGain_Base".Translate(pointBase.ToString("0.##")));
                float pointFactor = pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
                sb.AppendLine("StatsReport_MeditationPointDailyGain_Factor".Translate(pointFactor.ToString("0.##")));
                float pointGain = pointBase * pointFactor;
                sb.AppendLine("StatsReport_MeditationPointDailyGain".Translate(pointGain.ToString("0.##")));

                return sb.ToString();
            }
        }

        return "StatsReport_MeditationPointDailyGain_Invalid".Translate();
    }

    public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
    {
        Pawn pawn = optionalReq.Pawn ?? (optionalReq.Thing as Pawn);
        if (CanApplyOn(pawn))
        {
            float pointBase = pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationBase);
            if (pointBase <= 0f)
            {
                return "0 ( 0 x -- )";
            }
            else
            {
                float pointFactor = pawn.GetStatValue(OARO_ModDefOf.OARO_Stat_MeditationFactor);
                float pointGain = pointBase * pointFactor;
                return string.Format("{0} ( {1} x {2} )", pointGain.ToStringByStyle(stat.toStringStyle, numberSense), pointBase.ToString("0.##"), pointFactor.ToString("0.##"));
            }
        }

        return "0 ( -- x -- )";
    }
}