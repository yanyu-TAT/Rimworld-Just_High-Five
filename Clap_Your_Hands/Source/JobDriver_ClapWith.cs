using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ClapYourHands
{
    /// <summary>
    /// 手动击掌：走到对方身边后打开判定条，按判定结果结算；判定期间对方被 stun。
    /// </summary>
    public class JobDriver_ClapWith : JobDriver
    {
        //判定期间覆盖的 stun 时长（镜头延迟 + 淡入 + 5 秒判定 + 余量）
        private const int StunTicks = 480;

        private Pawn Target => job.targetA.Thing as Pawn;

        private ClapOutcome? _resolvedOutcome;
        private bool _loadedFromSave;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil face = new Toil();
            face.initAction = () =>
            {
                if (Target is not null)
                    pawn.rotationTracker.FaceTarget(Target);
            };
            face.handlingFacing = true;
            face.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return face;

            Toil wait = new Toil();
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            wait.initAction = OpenRhythm;
            wait.tickAction = TickRhythm;
            wait.AddFailCondition(() => Target is null || Target.Dead || Target.Map != pawn.Map || pawn.Dead);
            wait.AddEndCondition(() => _resolvedOutcome.HasValue ? JobCondition.Succeeded : JobCondition.Ongoing);
            wait.AddFinishAction(CleanupRhythm);
            yield return wait;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                _loadedFromSave = true;
        }

        private void OpenRhythm()
        {
            if (Target is null || Target.Dead || Target.Map != pawn.Map || _loadedFromSave)
            {
                CancelClap();
                return;
            }

            bool flag1 = !ClapUtility.TryGetHand(pawn, out _);
            bool flag2 = !ClapUtility.TryGetHand(Target, out _);
            if (flag1 || flag2)
            {
                var causer = flag1 ? pawn : Target;
                Messages.Message("ClapYourHands.ManualClap.NoHands".Translate(causer?.Name?.ToStringShort ?? "Unknown"), MessageTypeDefOf.RejectInput, historical: false);
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            Target.rotationTracker.FaceTarget(pawn);
            Target.stances?.stunner?.StunFor(StunTicks, pawn, addBattleLog: false, showMote: false, disableRotation: true);

            Vector3 mid = (pawn.DrawPos + Target.DrawPos) * 0.5f;
            CameraJumper.TryJump(new GlobalTargetInfo(mid.ToIntVec3(), pawn.Map), CameraJumper.MovementMode.Pan);

            Window_ClapRhythm window = new Window_ClapRhythm(pawn, Target, Resolve);
            Find.WindowStack.Add(window);
        }

        private void TickRhythm()
        {
            if (_loadedFromSave || Target is null || Target.Dead || Target.Map != pawn.Map || pawn.Dead)
            {
                CancelClap();
                return;
            }

            //判定期间保持双方面向；对方 stun 时自动转向被跳过，这里直接设置朝向
            pawn.rotationTracker.FaceTarget(Target);
            Target.rotationTracker.FaceTarget(pawn);
        }

        private void Resolve(ClapOutcome outcome)
        {
            _resolvedOutcome = outcome;

            if (Target == null || Target.Dead || Target.Map != pawn.Map)
                return;

            bool effectsEnabled = ClapUtility.CanClapNow(pawn, Target);
            if (!effectsEnabled)
                Messages.Message("ClapYourHands.ManualClap.OnCooldown".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);

            //立刻结束目标的击晕状态
            Target.stances?.stunner?.StopStun();
            ClapUtility.ApplyClapResult(pawn, Target, outcome, effectsEnabled);
        }

        //判定被中断：解除 stun 并结束工作；判定已完成时窗口自行收尾
        private void CleanupRhythm()
        {
            if (_resolvedOutcome.HasValue)
                return;

            Target?.stances?.stunner?.StopStun();
            Find.WindowStack.TryRemove(typeof(Window_ClapRhythm), doCloseSound: false);
        }

        private void CancelClap()
        {
            Target?.stances?.stunner?.StopStun();

            if (pawn.jobs?.curDriver == this)
                pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        }
    }
}
