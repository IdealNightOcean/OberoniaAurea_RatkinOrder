using NightOcean.SimpleAIClient;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace OberoniaAurea.RatkinOrder;

public static class DecoratePromptUtility
{
    private const string Space = "    ";

    public static void GetOrderPrompt(StringBuilder promptBuilder, RatkinOrder ratkinOrder)
    {
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrder".Translate(ratkinOrder.Name));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrderFunds".Translate(ratkinOrder.Funds.ToStringPercent("0.##")));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrderRelationship".Translate($"OARO_Relationship_{ratkinOrder.Relationship}".Translate()));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_RatkinOrderEsteem".Translate(ratkinOrder.Esteem));
    }

    public static void GetBranchPrompt(StringBuilder promptBuilder, Branch branch)
    {
        promptBuilder.AppendLine("OARO_Prompt_Branch".Translate(branch.Name));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchWorkStateDesc".Translate(branch.CurWorkStateDesc));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchSupplyState".Translate(branch.SupplyState, branch.Supply.ToStringPercent("0.##")));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchPotency".Translate(branch.Potency.ToString("0.##")));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchPopulation".Translate(branch.PopulationHandler.Population));

        promptBuilder.Append(Space);
        promptBuilder.AppendLine("OARO_Prompt_BranchPublicSecurity".Translate(branch.PopulationHandler.PublicSecurityLabel, branch.PopulationHandler.PublicSecurity.ToStringPercent("0.##")));

        if (branch.IsBranchOfType(Branch.BranchType.Friendly))
        {
            promptBuilder.Append(Space);
            promptBuilder.AppendLine("OARO_Prompt_BranchFriendly".Translate());

        }
        if (branch.HonorDef is not null)
        {
            promptBuilder.Append(Space);
            promptBuilder.AppendLine("OARO_Prompt_BranchHonor".Translate(branch.HonorDef.Named(KeyLibrary_FormatArgName.HONORDEF)));
        }

        if (branch.TaskHandler.HasTask)
        {
            promptBuilder.Append(Space);
            promptBuilder.AppendLine("OARO_Prompt_BranchSupplyTask".Translate(branch.TaskHandler.CurTask.Label));
        }

        if (branch.IsOnJointPatrol())
        {
            promptBuilder.Append(Space);
            promptBuilder.AppendLine("OARO_Prompt_BranchOnJointPatrol".Translate());
        }
    }

    public static IEnumerable<ClientMessage> GetMercyQuestTalkPrompt(MercyQuestDef mercyQuestDef)
    {
        if (mercyQuestDef is null)
        {
            yield break;
        }

        StringBuilder promptBuilder = new(RatkinOrderSettings.MainAIPrompt);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("OARO_Setting_MercyQuestPrompt_System".Translate());

        yield return new ClientMessage(ClientMessageRole.system, promptBuilder.ToString());


        string example = String.IsNullOrEmpty(mercyQuestDef.reasonForHelp) ? "OARO_Setting_MercyQuestPrompt_DefaultExample".Translate()
                                                                           : mercyQuestDef.reasonForHelp;

        yield return new ClientMessage(ClientMessageRole.user, "OARO_Setting_MercyQuestPrompt_User".Translate(mercyQuestDef.Named(KeyLibrary_FormatArgName.MERCYQUEST), example.Named("Example")));
    }

    public static IEnumerable<ClientMessage> GetIncidentConcernPrompt(Branch branch, IncidentDef incidentDef, IncidentParms parms, int delayDays)
    {
        if (branch is null || incidentDef is null || parms is null)
        {
            yield break;
        }

        StringBuilder promptBuilder = new(RatkinOrderSettings.MainAIPrompt);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("OARO_Prompt_IncidentConcern_System".Translate());
        GetOrderPrompt(promptBuilder, branch.RatkinOrder);
        promptBuilder.AppendLine();
        GetBranchPrompt(promptBuilder, branch);
        yield return new ClientMessage(ClientMessageRole.system, promptBuilder.ToString());

        promptBuilder.Clear();
        List<NamedArgument> arguments =
            [
                 incidentDef.LabelCap.Named("incidentLabel"),
                 delayDays.Named("delayDays")
            ];
        arguments.Add(GenerateNamedArgument(incidentDef.category?.LabelCap, "category"));
        arguments.Add(GenerateNamedArgument(parms.faction?.Name, "relatedFaction"));
        arguments.Add(GenerateNamedArgument(parms.points.ToString("F0"), "points"));
        arguments.Add(GenerateNamedArgument(parms.raidStrategy?.LabelCap, "raidStrategy"));
        arguments.Add(GenerateNamedArgument(parms.raidArrivalMode?.LabelCap, "raidArrivalMode"));

        promptBuilder.AppendLine("OARO_Prompt_IncidentConcern_User".Translate(arguments.ToArray()));
        arguments.Clear();
        yield return new ClientMessage(ClientMessageRole.user, promptBuilder.ToString());
    }

    public static IEnumerable<ClientMessage> GetMercyQuestAdmirePrompt(Branch branch, Quest quest, MercyQuestDef mercyQuestDef, int delayDays)
    {
        if (branch is null || quest is null || mercyQuestDef is null)
        {
            yield break;
        }

        StringBuilder promptBuilder = new(RatkinOrderSettings.MainAIPrompt);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("OARO_Prompt_MercyQuestAdmire_System".Translate());
        GetOrderPrompt(promptBuilder, branch.RatkinOrder);
        promptBuilder.AppendLine();
        GetBranchPrompt(promptBuilder, branch);
        yield return new ClientMessage(ClientMessageRole.system, promptBuilder.ToString());

        promptBuilder.Clear();
        promptBuilder.AppendLine("OARO_Prompt_MercyQuestAdmire_User".Translate(
            delayDays.Named("delayDays"),
            quest.name.Named("questName"),
            quest.description.Named("questDescription"),
            mercyQuestDef.Named(KeyLibrary_FormatArgName.MERCYQUEST)
            ));
        yield return new ClientMessage(ClientMessageRole.user, promptBuilder.ToString());
    }

    private static NamedArgument GenerateNamedArgument(string argument, string name)
    {
        if (String.IsNullOrEmpty(argument))
        {
            return "OARO_Prompt_NotAvailable".Translate().Named(name);
        }
        else
        {
            return argument.Named(name);
        }
    }
}