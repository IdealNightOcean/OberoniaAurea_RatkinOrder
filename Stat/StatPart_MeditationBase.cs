using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatPart_MeditationBase : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (OrderStationHandler.Instance.OrderHallRoom is not null)
            return;

        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !pawn.Faction.IsPlayerSafe())
            return;

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight record))
            return;

        val += 5f;
        val += Mathf.Min(OrderStationHandler.BuildingHandler.AcademicFurnituresCount * 2f, 30f);
        val += ResidentPawnsManager.Instance.InstructorKnightsCount.Value * 5f;
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (OrderStationHandler.Instance.OrderHallRoom is not null)
            return null;

        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || !pawn.Faction.IsPlayerSafe())
            return null;

        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight record))
            return null;

        int stepChange;
        StringBuilder sb = new(64);
        sb.AppendLine("OARO_MeditationBase_OrderHallRoom".Translate(5.ToStringWithSign()));

        int academicFurnituresCount = OrderStationHandler.BuildingHandler.AcademicFurnituresCount;
        if (academicFurnituresCount > 0)
        {
            stepChange = Mathf.Min(OrderStationHandler.BuildingHandler.AcademicFurnituresCount * 2, 30);
            sb.AppendLine("OARO_ChangeOffset_AcademicFurnituresCount".Translate(academicFurnituresCount.ToString(), stepChange.ToStringWithSign()));
        }

        int instructorKnightsCount = ResidentPawnsManager.Instance.InstructorKnightsCount.Value;
        if (instructorKnightsCount > 0)
        {
            stepChange = instructorKnightsCount * 5;
            sb.AppendLine("OARO_ChangeOffset_ResidentInstructorKnightsCount".Translate(instructorKnightsCount.ToString(), stepChange.ToStringWithSign()));
        }

        return sb.ToString();
    }
}