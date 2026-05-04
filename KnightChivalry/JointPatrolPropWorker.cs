using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 骑士精神联巡属性的功能类
/// </summary>
public class JointPatrolPropWorker
{
    public virtual void OnJointPatrolTaskCompleted(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        rewardData.OnChivalryCompleted(def);
    }
    public virtual void OnJointPatrolTaskFailed(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        rewardData.OnChivalryFailed(def);
    }
}

public class JointPatrolPropWorker_Base : JointPatrolPropWorker
{
    public override void OnJointPatrolTaskCompleted(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        base.OnJointPatrolTaskCompleted(def, rewardData);
        rewardData.AdjustBranchMedal(def, count: 1);
    }

    public override void OnJointPatrolTaskFailed(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        base.OnJointPatrolTaskFailed(def, rewardData);
        rewardData.Fund -= 0.03f;
        rewardData.ParticipantPublicSecurity -= 0.025f;
    }
}

public class JointPatrolPropWorker_Courage : JointPatrolPropWorker_Base
{
    public override void OnJointPatrolTaskCompleted(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        base.OnJointPatrolTaskCompleted(def, rewardData);

        rewardData.Fund += 0.05f;
        rewardData.PublicSecurity += Rand.Range(0.05f, 0.15f);
    }
}

public class JointPatrolPropWorker_Tenacity : JointPatrolPropWorker_Base
{
    public override void OnJointPatrolTaskCompleted(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        base.OnJointPatrolTaskCompleted(def, rewardData);

        rewardData.Fund += 0.05f;
        rewardData.Population += Rand.Range(50, 150);
    }
}

public class JointPatrolPropWorker_Rescue : JointPatrolPropWorker_Base
{
    public override void OnJointPatrolTaskCompleted(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        base.OnJointPatrolTaskCompleted(def, rewardData);

        rewardData.Reformation += 10f;
        rewardData.Population += Rand.Range(50, 150);
    }
}

public class JointPatrolPropWorker_Justice : JointPatrolPropWorker_Base
{
    public override void OnJointPatrolTaskCompleted(KnightChivalryDef def, JointPatrolRewardData rewardData)
    {
        base.OnJointPatrolTaskCompleted(def, rewardData);

        rewardData.Reformation += 10f;
        rewardData.PublicSecurity += Rand.Range(0.05f, 0.15f);
    }
}