using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OberoniaAurea.RatkinOrder;

public class Building_OrderLetterBox : Building
{
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        Command_Action command_OpenBox = new()
        {
            defaultLabel = "OARO_Command_OpenLetterBox".Translate(),
            defaultDesc = "OARO_Command_OpenLetterBox".Translate(),
            icon = IconLibrary.RecommendationIcon,
            iconProportions = new Vector2(1.48f, 1f),
            action = OrderLetterUtility.OpenLetterBox
        };
        yield return command_OpenBox;
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
        {
            yield return floatMenuOption;
        }
        if (!selPawn.Faction.IsPlayerSafe() || !selPawn.RaceProps.Humanlike)
        {
            yield break;
        }
        if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            yield break;
        }
        yield return new FloatMenuOption("OARO_Command_OpenLetterBox".Translate(), action: OrderLetterUtility.OpenLetterBox);
    }

}
