using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public class StatPart_MeditationBase : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || pawn.Map != OrderHallHandler.Instance.MainOrderCodePedestal?.Map)
        {
            return;
        }
        if (!ResidentKnightsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
        {
            return;
        }

        if (OrderHallHandler.Instance.OrderHallRoom is not null)
        {
            val += 5f;
        }

        val += Mathf.Min(OrderHallHandler.Instance.AcademicFurnituresCount * 2f, 30f);
        val += ResidentKnightsManager.Instance.InstructorKnightsCount.Value * 5f;
    }

    public override string ExplanationPart(StatRequest req)
    {
        Pawn pawn = req.Thing as Pawn;
        if (!pawn.CanBeKnight() || pawn.Map != OrderHallHandler.Instance.MainOrderCodePedestal?.Map)
        {
            return null;
        }
        if (!ResidentKnightsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnightRecord record))
        {
            return null;
        }

        int stepChange;
        StringBuilder sb = new();
        if (OrderHallHandler.Instance.OrderHallRoom is not null)
        {
            sb.AppendLine("OARO_MeditationBase_OrderHallRoom".Translate(5.ToStringWithSign()));
        }

        int academicFurnituresCount = OrderHallHandler.Instance.AcademicFurnituresCount;
        if (academicFurnituresCount > 0)
        {
            stepChange = Mathf.Min(OrderHallHandler.Instance.AcademicFurnituresCount * 2, 30);
            sb.AppendLine("OARO_ChangeOffset_AcademicFurnituresCount".Translate(academicFurnituresCount.ToString(), stepChange.ToStringWithSign()));
        }

        int instructorKnightsCount = ResidentKnightsManager.Instance.InstructorKnightsCount.Value;
        if (instructorKnightsCount > 0)
        {
            stepChange = instructorKnightsCount * 5;
            sb.AppendLine("OARO_ChangeOffset_ResidentInstructorKnightsCount".Translate(instructorKnightsCount.ToString(), stepChange.ToStringWithSign()));
        }

        return sb.ToString();
    }
}