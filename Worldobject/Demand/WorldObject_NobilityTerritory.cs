using System;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 叛乱镇压 - 贵族领地
/// </summary>
public class WorldObject_NobilityTerritory : WorldObject_BranchDemand
{
    public enum NobilityType : byte
    {
        None,
        Deceitful, //狡诈
        Stubborn, //刚愎
        Justice, //正义
        Tyrannical, //暴虐
        Kindness, //仁慈
        Greediness //贪婪
    }

    public string NobilityCliqueKey => "Nobility_" + ID;
    public string NobilityCivilianCliqueKey => "NobilityCivilian_" + ID;

    private NobilityType nobilityType;
    private int troops;
    private bool hasExposeType;
    private bool hasExposeTroops;

    private float osmolity;

    private float Osmolity
    {
        get => osmolity;
        set => osmolity = Mathf.Clamp01(osmolity);
    }

    public override void PostAdd()
    {
        base.PostAdd();
        QuestClique nobilityClique = new()
        {
            Name = "OARO_CliqueName_Nobility".Translate(Name),
            ActiveDesc = "OARO_CliqueActiveDesc_Nobility".Translate(),
            InactiveDesc = "OARO_CliqueInactiveDesc_Nobility".Translate(),
            Potency = 0.2f,
            Willingness = 0f,

            IsActivatable = true,
            IsBribable = false,
            IsCommunicable = false
        };
        CliquesManager.TryAddClique(NobilityCliqueKey, nobilityClique);

        if (nobilityType == NobilityType.Kindness)
        {
            QuestClique civilianClique = new()
            {
                Name = "OARO_CliqueName_NobilityCivilian".Translate(Name),
                ActiveDesc = "OARO_CliqueActiveDesc_NobilityCivilian".Translate(),
                InactiveDesc = "OARO_CliqueInactiveDesc_NobilityCivilian".Translate(),
                Potency = -0.3f,
                Willingness = 0f,

                IsActivatable = false,
                IsBribable = false,
                IsCommunicable = false
            };
            CliquesManager.TryAddClique(NobilityCivilianCliqueKey, civilianClique);
        }

        QuestPart_NobilityTerritory_TypeSelecter questPart_NobilityTerritory_TypeSelecter = quest?.GetFirstPartOfType<QuestPart_NobilityTerritory_TypeSelecter>();
        (nobilityType, hasExposeType) = questPart_NobilityTerritory_TypeSelecter is null ? (NobilityType.None, true) : questPart_NobilityTerritory_TypeSelecter.PopAlternative();

    }

    protected override void FinishWork()
    {
        throw new NotImplementedException();
    }

    protected override void InterruptWork()
    {
        throw new NotImplementedException();
    }

}
