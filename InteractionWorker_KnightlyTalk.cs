using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class InteractionWorker_KnightlyTalk : InteractionWorker
{
    private const float BaseSelectionWeight = 0.075f;

    private static readonly SimpleCurve CompatibilityFactorCurve =
    [
        new CurvePoint(-1.5f, 0f),
        new CurvePoint(-0.5f, 0.1f),
        new CurvePoint(0f, 1f),
        new CurvePoint(0.5f, 1f),
        new CurvePoint(1f, 1.8f),
        new CurvePoint(2f, 3f)
    ];

    public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
    {
        if (!initiator.CanBeKnight() || initiator.Inhumanized())
        {
            return 0f;
        }
        if (!KnightPawnsManager.Instance.IsKnight(initiator))
        {
            return 0f;
        }
        return BaseSelectionWeight
            * CompatibilityFactorCurve.Evaluate(initiator.relations.CompatibilityWith(recipient))
            * (initiator.Faction.IsPlayerSafe() ? 1f : 5f);
    }

    public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
    {
        base.Interacted(initiator, recipient, extraSentencePacks, out letterText, out letterLabel, out letterDef, out lookTargets);
        if (!KnightPawnsManager.Instance.TryGetKnightRecord(initiator, out KnightRecord initiatorKnight))
            return;

        GiveKnightlyTalkHediff(initiatorKnight, recipient);

    }

    private void GiveKnightlyTalkHediff(KnightRecord initiatorKnight, Pawn recipient)
    {
        if (!Rand.Chance(0.1f))
            return;
        HediffDef knightlyTalkHediff = initiatorKnight.Chivalry?.knightlyTalkHediff;
        if (knightlyTalkHediff is null)
            return;

        Hediff hediff = recipient.health.GetOrAddHediff(knightlyTalkHediff);
        HediffComp_Disappears disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
        if (disappearsComp is not null)
        {
            disappearsComp.disappearsAfterTicks = 5 * 60000;
            disappearsComp.ticksToDisappear = 5 * 60000;
        }
    }

    private void KnightVirtueUpgrade(KnightRecord initiatorKnight, Pawn recipient)
    {
        if (!Rand.Chance(0.01f))
            return;
        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(recipient, out ResidentKnight recipientKnight))
            return;

        KnightChivalryDef initiatorChivalry = initiatorKnight.Chivalry;
        KnightVirtueDef targetVirtue = KnightVirtueUtility.GetRandomUpgradableVirtue(recipientKnight, v => initiatorChivalry.IsSameDefNonNullable(v.chivalry));
        if (targetVirtue is null)
            return;

        string reason = "OARO_VirtueUpgradeReason_KnightlyTalk".Translate(initiatorKnight.Pawn.Named(KeyLibrary_FormatArgName.PAWN));
        recipientKnight.KnightVirtueHandler.UpgradeVirtue(targetVirtue, reason: reason);
    }
}
