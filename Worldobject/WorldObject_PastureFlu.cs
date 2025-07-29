using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class WorldObject_PastureFlu : WorldObject_InteractWithFixedCarvanBase
{
    public override string FixedCaravanName => "OARO_FixedCaravan_PastureFlu".Translate();

    public override bool StartWork(Caravan caravan)
    {
        if (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine) < 0)
        {
            return false;
        }

        return base.StartWork(caravan);
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is not null)
        {
            int maxMedicineSkill = OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Medicine);

            if (maxMedicineSkill < 8)
            {

            }
            else if (maxMedicineSkill < 15)
            {

            }
            else
            {
                if (OAFrame_PawnUtility.GetMaxSkillLevelOfPawns(associatedFixedCaravan.PawnsListForReading, SkillDefOf.Intellectual) >= 10)
                {

                }
            }
        }

        Destroy();
    }

    protected override void InterruptWork()
    {



    }

}
