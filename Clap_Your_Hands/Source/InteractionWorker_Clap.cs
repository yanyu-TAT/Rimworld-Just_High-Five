using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ClapYourHands
{
    /// <summary>
    /// 击掌互动的工作器，由原版 <c>Pawn_InteractionsTracker</c> 按
    /// <see cref="RandomSelectionWeight"/> 加权抽取。
    /// </summary>
    public class InteractionWorker_Clap : InteractionWorker
    {
        /// <summary>
        /// 抽取权重。几何门禁（距离、视线）由原版 <c>Pawn_InteractionsTracker.CanInteractNowWith</c> 负责，
        /// 不在此重复校验；这里只表达击掌自身的规则。
        /// 抽取时按「每个候选 × 每个 InteractionDef」各调用一次，判定顺序为成本从低到高。
        /// </summary>
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            if (initiator.Inhumanized())
                return 0f;

            //双手未被占用
            if (initiator.IsCarryingPawn() || recipient.IsCarryingPawn())
                return 0f;

            //检查冷却
            if (!ClapUtility.CanClapNow(initiator, recipient))
                return 0f;

            //非仇视
            if (ClapUtility.IsHostileRelation(initiator, recipient))
                return 0f;

            //双方都至少需要一只可用的手
            if (!ClapUtility.TryGetHand(initiator, out _) || !ClapUtility.TryGetHand(recipient, out _))
                return 0f;

            return ClapDebug.MoreClapHands
                ? ClapUtility.BaseSelectionWeight * ClapDebug.WeightMultiplier
                : ClapUtility.BaseSelectionWeight;
        }

        /// <summary>互动实际发生时的结算入口。</summary>
        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks,
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            if (initiator is null || recipient is null || initiator.Dead || recipient.Dead)
            {
                Log.Error($"{ClapYourHandsMod.LogPrefix}击掌结算收到无效的小人，已跳过。");
                return;
            }

            var outcome = ClapUtility.RollOutcome(initiator, recipient);
            ClapUtility.ApplyOutcome(initiator, recipient, outcome);

            if (outcome == ClapOutcome.Perfect)
            {
                ClapUtility.ApplyPerfectReward(initiator);
                ClapUtility.ApplyPerfectReward(recipient);
                ClapEffects.PlayPerfect(initiator, recipient);
            }

            // 双方各自进入 24 小时冷却
            ClapUtility.ApplyCooldown(initiator);
            ClapUtility.ApplyCooldown(recipient);
        }
    }
}
