using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ClapYourHands
{
    /// <summary>击掌的纯逻辑：权重、结果抽取、结算应用。可调数值集中在文件顶部。</summary>
    public static class ClapUtility
    {
        // ==================== 可调数值（调参入口） ====================

        //击掌在原版随机社交池中的抽取权重
        public const float BaseSelectionWeight = 2.0f;

        //仇视门槛
        public const int HostileOpinionThreshold = -20;

        //增益持续时间
        public const int BuffDurationTicks = GenDate.TicksPerHour * 12;

        //击掌冷却
        public const int CooldownTicks = GenDate.TicksPerDay;

        //结果基础权重
        private static readonly float[] BaseOutcomeWeights = { 10f, 30f, 50f, 10f };

        //全部结果档位
        private static readonly ClapOutcome[] AllOutcomes =
        {
            ClapOutcome.Bad, ClapOutcome.Normal, ClapOutcome.Good, ClapOutcome.Perfect,
        };

        //完美击掌奖励分支权重
        private static readonly float[] PerfectRewardWeights = { 50f, 30f, 15f, 5f };

        //完美击掌的奖励分支
        private static readonly PerfectRewardBranch[] AllRewardBranches =
        {
            PerfectRewardBranch.S, PerfectRewardBranch.SS,
            PerfectRewardBranch.SSS, PerfectRewardBranch.Inspiration,
        };
        private enum PerfectRewardBranch
        {
            S = 0,
            SS = 1,
            SSS = 2,
            Inspiration = 3,
        }

        //好感度对正面效果权重影响
        private static readonly SimpleCurve PositiveWeightFactor = new()
        {
            new CurvePoint(-100f, 0.5f),
            new CurvePoint(0f, 1f),
            new CurvePoint(100f, 1.5f),
        };

        //好感度对负面效果权重影响
        private static readonly SimpleCurve NegativeWeightFactor = new()
        {
            new CurvePoint(0f, 1f),
            new CurvePoint(100f, 0.5f),
        };

        // ==================== 关系与冷却判定 ====================

        /// <summary>双向好感度中较低的一方。</summary>
        public static int MutualOpinion(Pawn a, Pawn b)
        {
            return Mathf.Min(a.relations.OpinionOf(b), b.relations.OpinionOf(a));
        }

        /// <summary>是否属于「仇视」关系。</summary>
        public static bool IsHostileRelation(Pawn a, Pawn b)
        {
            return MutualOpinion(a, b) <= HostileOpinionThreshold;
        }

        private static readonly List<BodyPartRecord> TmpHands = new List<BodyPartRecord>();

        /// <summary>
        /// 随机取一只可用于击掌的手（左右手共用 <see cref="BodyPartDefOf.Hand"/>）。
        /// <paramref name="hand"/> 返回可挂载部位：自然手为手本身，机械手为执行替换的祖先部位。
        /// </summary>
        public static bool TryGetHand(Pawn pawn, out BodyPartRecord hand)
        {
            hand = null;

            if (pawn?.health?.hediffSet is null)
                return false;

            List<BodyPartRecord> parts = pawn.RaceProps?.body?.AllParts;
            if (parts is null)
                return false;

            HediffSet hediffSet = pawn.health.hediffSet;
            TmpHands.Clear();

            for (int i = 0; i < parts.Count; i++)
            {
                BodyPartRecord part = parts[i];
                if (part.def != BodyPartDefOf.Hand)
                    continue;

                BodyPartRecord anchor = ResolveHandAnchor(hediffSet, part);
                if (anchor is not null)
                    TmpHands.Add(anchor);
            }

            return TmpHands.TryRandomElement(out hand);
        }

        /// <summary>
        /// 解析该手对应的可挂载部位：手存在则返回手，手因祖先被植入体替换而缺失则返回替换部位，
        /// 截肢返回 null。
        /// </summary>
        private static BodyPartRecord ResolveHandAnchor(HediffSet hediffSet, BodyPartRecord hand)
        {
            if (!hediffSet.PartIsMissing(hand))
                return hand;

            for (BodyPartRecord part = hand.parent; part is not null; part = part.parent)
            {
                if (hediffSet.HasDirectlyAddedPartFor(part))
                    return part;
            }

            return null;
        }

        public static bool IsOnCooldown(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediff<Hediff_ClapCooldown>() is not null;
        }

        /// <summary>当前是否允许这两人击掌（冷却判定，见 <see cref="CooldownRequiresBoth"/>）。</summary>
        public static bool CanClapNow(Pawn a, Pawn b)
        {
            bool aReady = !IsOnCooldown(a);
            bool bReady = !IsOnCooldown(b);
            return aReady || bReady;
        }

        // ==================== 结果抽取与结算 ====================

        /// <summary>按双方好感度修正后抽取击掌结果。权重全部为 0 时回退为「普通」。</summary>
        public static ClapOutcome RollOutcome(Pawn initiator, Pawn recipient)
        {
            if (ClapDebug.AlwaysPerfect)
                return ClapOutcome.Perfect;

            int opinion = MutualOpinion(initiator, recipient);
            float positiveFactor = PositiveWeightFactor.Evaluate(opinion);
            float negativeFactor = NegativeWeightFactor.Evaluate(opinion);

            return AllOutcomes.RandomElementByWeightWithFallback(
                outcome => OutcomeWeight(outcome, positiveFactor, negativeFactor),
                ClapOutcome.Normal);
        }

        /// <summary>各结果档位的修正后权重。</summary>
        private static float OutcomeWeight(ClapOutcome outcome, float positiveFactor, float negativeFactor) => 
            outcome switch
            {
                ClapOutcome.Bad => BaseOutcomeWeights[0] * negativeFactor,
                ClapOutcome.Normal => BaseOutcomeWeights[1],
                ClapOutcome.Good => BaseOutcomeWeights[2] * positiveFactor,
                ClapOutcome.Perfect => BaseOutcomeWeights[3] * positiveFactor,
                _ => 0f,
            };

        /// <summary>把结果对应的情绪 / 好感度想法写入双方。</summary>
        public static void ApplyOutcome(Pawn initiator, Pawn recipient, ClapOutcome outcome)
        {
            var thought = ThoughtFor(outcome);
            if (thought is null) return;

            // 原版工具：内部按 SocialImpact 缩放 opinionOffset
            Pawn_InteractionsTracker.AddInteractionThought(initiator, recipient, thought);
            Pawn_InteractionsTracker.AddInteractionThought(recipient, initiator, thought);
        }

        private static ThoughtDef ThoughtFor(ClapOutcome outcome) => 
            outcome switch
            {
                ClapOutcome.Bad => ClapDefOf.Clap_Bad,
                ClapOutcome.Normal => ClapDefOf.Clap_Normal,
                ClapOutcome.Good => ClapDefOf.Clap_Good,
                ClapOutcome.Perfect => ClapDefOf.Clap_Perfect,
                _ => null,
            };

        // ==================== 完美击掌奖励 ====================

        /// <summary>完美击掌的奖励：3 级增益之一，或随机灵感（不可用时回退为第三级增益）。</summary>
        public static void ApplyPerfectReward(Pawn pawn)
        {
            var branch = AllRewardBranches.RandomElementByWeightWithFallback(
                b => PerfectRewardWeights[(int)b],
                PerfectRewardBranch.S);

            if (branch != PerfectRewardBranch.Inspiration)
            {
                ApplyBuff(pawn, (int)branch + 1);
                return;
            }

            if (TryGrantInspiration(pawn))
                return;

            // 已有灵感 / 被 hediff 阻断 → 回退为第三级增益
            ApplyBuff(pawn, 3);
        }

        private static bool TryGrantInspiration(Pawn pawn)
        {
            InspirationHandler handler = pawn.mindState?.inspirationHandler;
            if (handler is null || handler.Inspired) return false;

            InspirationDef def = handler.GetRandomAvailableInspirationDef();
            if (def is null) return false;

            // reason 作为灵感开始信件的正文前缀
            var reason = "ClapYourHands.InspirationReason".Translate(pawn.Named("PAWN"));
            return handler.TryStartInspiration(def, reason);
        }

        /// <summary>施加 / 合并增益。</summary>
        public static void ApplyBuff(Pawn pawn, int level)
        {
            TryGetHand(pawn, out BodyPartRecord hand);
            var buff = pawn?.health?.GetOrAddHediff(ClapDefOf.Clap_Buff, hand) as Hediff_ClapBuff;
            buff.ApplyLevel(level, BuffDurationTicks);
        }

        /// <summary>写入 24 小时冷却。</summary>
        public static void ApplyCooldown(Pawn pawn)
        {
            var cooldown = pawn?.health?.GetOrAddHediff(ClapDefOf.Clap_Cooldown) as Hediff_ClapCooldown;
            cooldown?.ResetTimer(CooldownTicks);
        }

    }
}
