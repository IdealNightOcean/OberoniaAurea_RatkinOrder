using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 庆典村庄（特化类）
/// </summary>
public sealed class WorldObject_CelebrationHost : WorldObject_InteractWithFixedCaravan_Nameable
{
    public override int TicksNeeded => 60000;
    public override string FixedCaravanName => "OARO_FixedCaravan_CelebrationHost".Translate();
    public override string FixedCaravanWorkDesc() => "OARO_CelebrationHost_TimeLeft".Translate(ticksRemaining.ToStringTicksToPeriod());

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (!caravan.PawnsListForReading.Any(p => p.skills is not null && !p.skills.GetSkill(SkillDefOf.Social).TotallyDisabled))
        {
            Messages.Message("OAFrame_MissSkillAvailablePawn".Translate(SkillDefOf.Social.Named(KeyLibrary_FormatArgName.SKILL)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("OARO_Thought_CelebrationHost");
            foreach (Pawn pawn in associatedFixedCaravan.PawnsListForReading)
            {
                pawn.needs.mood?.thoughts.memories.TryGainMemory(thoughtDef);
            }

            int count = associatedFixedCaravan.PawnsListForReading.Count * 10;
            List<Thing> rewards = OAFrame_MiscUtility.TryGenerateThing(OARO_ThingDefOf.RK_StrawberryBeer, count);

            OAFrame_FixedCaravanUtility.GiveThings(associatedFixedCaravan, rewards);

            (Pawn maxSocialPawn, int maxSocialSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Social);

            maxSocialPawn ??= associatedFixedCaravan.PawnsListForReading.RandomElement();
            maxSocialPawn.skills?.Learn(SkillDefOf.Social, 6000f);
            string text = "OARO_CelebrationHost_Finish".Translate(maxSocialPawn, count) + "\n" + "OAFrame_PawnGainSkillXp".Translate(maxSocialPawn, SkillDefOf.Social.LabelCap, 6000);
        }

        this.SendWorkResolvedSignal();

        this.SafeDestroy();
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_CelebrationHost_Interrupt".Translate()));
        this.SafeDestroy();
    }
}
