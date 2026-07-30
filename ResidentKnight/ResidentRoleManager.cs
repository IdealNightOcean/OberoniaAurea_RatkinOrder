using OberoniaAurea.RatkinOrder.Utility;
using OberoniaAurea_Frame;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace OberoniaAurea.RatkinOrder;

/// <summary>
/// 常驻骑士职位管理器 - 负责管理常驻骑士的职位分配和相关Buff阶段的计算
/// </summary>
public class ResidentRoleManager : IExposable
{
    public static ResidentRoleManager Instance { get; private set; }

    private readonly int tickHashOffset;

    private Dictionary<ResidentKnightRoleDef, ResidentKnight> rolesToKnights = [];
    public IReadOnlyDictionary<ResidentKnightRoleDef, ResidentKnight> RolesToKnights => rolesToKnights;

    private HediffStageModifierBuilder BuffStageTemplate { get; } = new();
    private int nextBuffStageForceRefreshTick;

    public ResidentRoleManager()
    {
        OAFrame_MiscUtility.ValidateSingleton(Instance, nameof(ResidentRoleManager));
        Instance = this;

        tickHashOffset = Rand.Range(0, int.MaxValue).HashOffset();
    }
    public static void ClearStaticCache() => Instance = null;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKnightOfRole(ResidentKnightRoleDef roleDef, out ResidentKnight record) => rolesToKnights.TryGetValue(roleDef, out record);

    public bool TrySetKnightRole(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (SetResidentKnightRole(pawn, roleDef, replaceCurRole))
        {
            BuffStageTemplate.MarkInvalid();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取新的Buff阶段。会根据当前常驻骑士的职位情况刷新Buff阶段模板。
    /// </summary>
    public HediffStage GetNewBuffStage()
    {
        if (!BuffStageTemplate.IsReady)
        {
            RefreshRoleBuffStageTemplate();
        }

        return BuffStageTemplate.BuildNewHediffStage();
    }

    public void Tick()
    {
        if (TickUtility.IsHashIntervalTick(tickHashOffset, 60000))
        {
            nextBuffStageForceRefreshTick = Find.TickManager.TicksGame + 60000;
            BuffStageTemplate.MarkInvalid();
        }
    }

    public void Notify_ResidentKnightRemoved(ResidentKnight knight)
    {
        if (knight.CurRole is not null)
        {
            RemoveResidentKnightRole(knight.CurRole);
            BuffStageTemplate.MarkInvalid();
        }
    }

    private bool SetResidentKnightRole(Pawn pawn, ResidentKnightRoleDef roleDef, bool replaceCurRole = true)
    {
        if (!ResidentPawnsManager.Instance.TryGetKnightRecord(pawn, out ResidentKnight pawnRecord))
        {
            return false;
        }

        if (rolesToKnights.TryGetValue(roleDef, out ResidentKnight curRolePawnRecord))
        {
            if (curRolePawnRecord.Pawn == pawn)
            {
                return true;
            }
            if (!replaceCurRole)
            {
                return false;
            }
        }

        ResidentKnightRoleDef pOldRole = pawnRecord.CurRole;

        switch (curRolePawnRecord, pOldRole)
        {
            //新增职位
            case (null, null):
                {
                    pawnRecord.ChangeRole(roleDef);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
            //两人交接职位
            case (not null, null):
                {
                    curRolePawnRecord.ChangeRole(null);
                    pawnRecord.ChangeRole(roleDef);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
            //本人职位改变
            case (null, not null):
                {
                    pawnRecord.ChangeRole(roleDef);
                    rolesToKnights.Remove(pOldRole);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
            //替代对方职位
            case (not null, not null):
                {
                    curRolePawnRecord.ChangeRole(null);
                    pawnRecord.ChangeRole(roleDef);

                    rolesToKnights.Remove(pOldRole);
                    rolesToKnights[roleDef] = pawnRecord;
                    break;
                }
        }

        return true;
    }

    private bool RemoveResidentKnightRole(ResidentKnightRoleDef roleDef)
    {
        if (rolesToKnights.TryGetValue(roleDef, out ResidentKnight pawnRecord))
        {
            pawnRecord.ChangeRole(null);
            rolesToKnights.Remove(roleDef);
            return true;
        }
        return false;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref rolesToKnights, nameof(rolesToKnights), LookMode.Def, LookMode.Reference, ref rolesToKnightKeys, ref rolesToKnightValues);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (rolesToKnights.RemoveAll(kv => kv.Value is null || kv.Value.CurState == ResidentPawnState.ForceRemove) > 0)
            {
                Log.Error($"[OARO] {nameof(ResidentRoleManager)} 的部分常驻骑士角色在加载后为null或无效，已被移除。");
            }
        }
    }

    private void RefreshRoleBuffStageTemplate()
    {
        BuffStageTemplate.ResetTemplate();

        int lawOrderKnightsCount = ResidentPawnsManager.Instance.ResidentKnights.Where(r => r?.Branch?.HonorDef == OARO_ModDefOf.OARO_Honor_LawOrder).Count();
        if (lawOrderKnightsCount > 0)
        {
            BuffStageTemplate.AddOffset(StatDefOf.GlobalLearningFactor, Mathf.Min(lawOrderKnightsCount * 0.12f, 0.6f));
        }

        foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnight> kv in rolesToKnights)
        {
            (ResidentKnightRoleDef roldDef, Pawn pawn) = (kv.Key, kv.Value.Pawn);

            BuffStageTemplate.AddOffsets(roldDef.statOffsets);
            BuffStageTemplate.AddOffsets(roldDef.RoleWorker.RoleStatOffsets(pawn));

            BuffStageTemplate.AddFactors(roldDef.statFactors);
            BuffStageTemplate.AddFactors(roldDef.RoleWorker.RoleStatFactors(pawn));
        }

        nextBuffStageForceRefreshTick = Find.TickManager.TicksGame + 60000;
        BuffStageTemplate.FinalizeTemplate();
    }

    public void DrawDevWindow(Listing_Standard listing_Rect)
    {
        listing_Rect.Label($"常驻骑士职位: {rolesToKnights.Count}");
        if (rolesToKnights.NullOrEmpty())
        {
            listing_Rect.SubLabel("None".Translate(), widthPct: 0.8f);
        }
        else
        {
            foreach (KeyValuePair<ResidentKnightRoleDef, ResidentKnight> kv in rolesToKnights)
            {
                listing_Rect.SubLabel(kv.Key.label + ": " + kv.Value.Pawn.Name, widthPct: 0.8f);
            }
        }
    }

    private List<ResidentKnightRoleDef> rolesToKnightKeys;
    private List<ResidentKnight> rolesToKnightValues;
}