using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 牧场流感种子村庄（特化类）
/// </summary>
public sealed class WorldObject_PastureFlu : WorldObject_InteractWithFixedCaravan_Nameable
{
    public override int TicksNeeded => 30000;
    public override string FixedCaravanName => "OARO_FixedCaravan_PastureFlu".Translate();
    public override string FixedCaravanWorkDesc() => "OARO_PastureFlu_TimeLeft".Translate(ticksRemaining.ToStringTicksToPeriod());

    public override void Notify_CaravanArrived(Caravan caravan)
    {
        if (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(caravan.PawnsListForReading, SkillDefOf.Medicine) < 0)
        {
            Messages.Message("OARO_NoOneCanDo".Translate(SkillDefOf.Medicine.label), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            (Pawn maxMedicinePawn, int maxMedicineSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);

            if (maxMedicineSkill < 8)
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PastureFlu_Fail".Translate()));
            }
            else if (maxMedicineSkill < 15)
            {
                this.SendWorkResolvedSignal();
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PastureFlu_Success".Translate(maxMedicinePawn)));
            }
            else
            {
                this.SendWorkResolvedSignal();
                EsteemUtility.AdjustAllOrdersEsteem(2, byPlayer: true, reason: "OARO_ResolvedFlu".Translate());
                StringBuilder sb = new("OARO_PastureFlu_BigSuccess".Translate(maxMedicinePawn, 2));

                (Pawn maxIntellectualPawn, int maxIntellectualSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Intellectual);
                if (maxIntellectualSkill >= 10)
                {
                    sb.AppendInNewLine("OARO_PastureFlu_Conspiracy".Translate(maxIntellectualPawn));
                }

                (Pawn maxAnimalsPawn, int maxAnimalsSkill) = OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
                if (maxAnimalsSkill >= 10)
                {
                    int herbalCount = Rand.RangeInclusive(90, 150);
                    List<Thing> rewards = OAFrame_MiscUtility.TryGenerateThing(ThingDefOf.MedicineHerbal, herbalCount);
                    OAFrame_FixedCaravanUtility.GiveThings(associatedFixedCaravan, rewards);
                    sb.AppendInNewLine("OARO_PastureFlu_Herbal".Translate(maxAnimalsPawn, herbalCount));
                }
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(sb.ToString()));
            }
        }

        this.SafeDestroy();
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PastureFlu_Interrupt".Translate()));
        this.SafeDestroy();
    }
}
