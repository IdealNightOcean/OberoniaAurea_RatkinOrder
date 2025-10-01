using OberoniaAurea_Frame;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public sealed class WorldObject_SupplyDepotConstruction : WorldObject_InteractWithFixedCaravan_Nameable
{
    private float constricProgress = 0f;
    private bool autoCotrInofrmed;
    private int ticksToNextAutoCtor;

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (constricProgress > 400f)
        {
            if ((ticksToNextAutoCtor -= delta) <= 0)
            {
                ticksToNextAutoCtor = 2500;
                constricProgress = Mathf.Clamp(constricProgress + 10f, 0f, 800f);
                if (constricProgress >= 800f && !isWorking)
                {
                    SendWorkResolvedSignal();
                    Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_SupplyDepot_FinallyAutoFinished".Translate(), Faction));
                    PlanetTile tile = Tile;
                    this.SafeDestroy();
                }
            }
        }
    }

    protected override void FinishWork()
    {
        if (associatedFixedCaravan is null)
        {
            return;
        }

        int totalSkillLevel = associatedFixedCaravan.PawnsListForReading.Sum(p => p.skills?.GetSkill(SkillDefOf.Construction).GetLevel() ?? 0);
        float gainProgress = totalSkillLevel * 2f + associatedFixedCaravan.PawnsCount * 10f;
        if (constricProgress >= 400f)
        {
            gainProgress *= 1.5f;
        }
        constricProgress = Mathf.Clamp(constricProgress + gainProgress, 0f, 800f);

        if (constricProgress >= 800f)
        {
            SendWorkResolvedSignal();
            Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_SupplyDepot_FinallyFinished".Translate(), Faction));

            PlanetTile tile = Tile;
            this.SafeDestroy();
        }
        else
        {
            if (!autoCotrInofrmed && constricProgress >= 400f)
            {
                autoCotrInofrmed = true;
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_SupplyDepot_AutoCtorStar".Translate(gainProgress.ToString("F2")), Faction));
            }
            else
            {
                Find.WindowStack.Add(OAFrame_DiaUtility.DefaultConfirmDiaNodeTreeWithFactionInfo("OARO_SupplyDepot_Finished".Translate(gainProgress.ToString("F2")), Faction));
            }
        }
    }
    protected override void InterruptWork() { }
}
