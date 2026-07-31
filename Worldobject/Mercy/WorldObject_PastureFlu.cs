using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame.DataLibrary;
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
        if (!caravan.PawnsListForReading.Any(p => p.skills is not null && !p.skills.GetSkill(SkillDefOf.Medicine).TotallyDisabled))
        {
            Messages.Message("OAFrame_MissSkillAvailablePawn".Translate(SkillDefOf.Medicine.Named(KeyLibrary_FormatArgName.SKILL)), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        base.Notify_CaravanArrived(caravan);
    }

    public override bool StartWork(Caravan caravan)
    {
        if (base.StartWork(caravan))
        {
            Messages.Message("OARO_PastureFlu_Arrival".Translate(this.Named(KeyLibrary_FormatArgName.WORLDOBJECT)), MessageTypeDefOf.PositiveEvent);
            return true;
        }
        return false;
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            (Pawn maxMedicinePawn, int maxMedicineSkill) = OberoniaAurea_Frame.Utility.OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);

            if (maxMedicineSkill < 8)
            {
                Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PastureFlu_Fail".Translate()));
            }
            else if (maxMedicineSkill < 15)
            {
                this.SendWorkResolvedSignal();
                Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PastureFlu_Success".Translate(maxMedicinePawn)));
            }
            else
            {
                this.SendWorkResolvedSignal();
                EsteemUtility.AdjustAllOrdersEsteem(2, byPlayer: true, reason: "OARO_ResolvedFlu".Translate());
                StringBuilder sb = new("OARO_PastureFlu_BigSuccess".Translate(maxMedicinePawn.Named(KeyLibrary_FormatArgName.PAWN), 2.Named(KeyLibrary_FormatArgName.Count)));

                (Pawn maxIntellectualPawn, int maxIntellectualSkill) = OberoniaAurea_Frame.Utility.OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Intellectual);
                if (maxIntellectualSkill >= 10)
                {
                    sb.AppendInNewLine("OARO_PastureFlu_Conspiracy".Translate(maxIntellectualPawn.Named(KeyLibrary_FormatArgName.PAWN)));
                }

                (Pawn maxAnimalsPawn, int maxAnimalsSkill) = OberoniaAurea_Frame.Utility.OAFrame_PawnUtility.GetMaxSkillLevelPawn(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Animals);
                if (maxAnimalsSkill >= 10)
                {
                    int herbalCount = Rand.RangeInclusive(90, 150);
                    List<Thing> rewards = OberoniaAurea_Frame.Utility.OAFrame_ThingUtility.GenerateThingListSplitByStack(ThingDefOf.MedicineHerbal, herbalCount);
                    OberoniaAurea_Frame.Utility.OAFrame_FixedCaravanUtility.GiveThings(associatedFixedCaravan, rewards);
                    sb.AppendInNewLine("OARO_PastureFlu_Herbal".Translate(maxAnimalsPawn.Named(KeyLibrary_FormatArgName.PAWN), herbalCount.Named(KeyLibrary_FormatArgName.Count)));
                }
                Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree(sb.ToString()));
            }
        }

        this.SafeDestroy();
    }

    protected override void InterruptWork()
    {
        Find.WindowStack.Add(OberoniaAurea_Frame.Utility.OAFrame_DiaUtility.DefaultConfirmDiaNodeTree("OARO_PastureFlu_Interrupt".Translate()));
        this.SafeDestroy();
    }
}
