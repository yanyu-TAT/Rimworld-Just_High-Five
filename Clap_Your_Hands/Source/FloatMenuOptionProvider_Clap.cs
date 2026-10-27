using RimWorld;
using Verse;

namespace ClapYourHands
{
    /// <summary>
    /// 浮动菜单里的「击掌」：选中单个殖民者后右键另一个殖民者。
    /// 冷却中仍可点击，但结算时不产生数值效果；仇视关系显示为禁用项。
    /// </summary>
    public class FloatMenuOptionProvider_Clap : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool RequiresManipulation => true;

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn actor = context.FirstSelectedPawn;
            if (actor is null || clickedPawn is null || actor == clickedPawn
                || clickedPawn.Dead || clickedPawn.Downed
                || !actor.Spawned || !clickedPawn.Spawned
                || actor.Map != clickedPawn.Map)
            {
                return null;
            }

            string nameA = actor?.Name?.ToStringShort ?? "Unknown";
            string nameC = clickedPawn?.Name?.ToStringShort ?? "Unknown";

            bool flag1 = !ClapUtility.TryGetHand(actor, out _);
            bool flag2 = !ClapUtility.TryGetHand(clickedPawn, out _);
            if (flag1 || flag2)
                return new FloatMenuOption("ClapYourHands.ManualClap.MenuNoHands".Translate(nameC, flag1 ? nameA : nameC), null);

            if (ClapUtility.IsHostileRelation(actor, clickedPawn))
                return new FloatMenuOption("ClapYourHands.ManualClap.MenuHostile".Translate(nameC), null);

            bool onCooldown = !ClapUtility.CanClapNow(actor, clickedPawn);
            string label = onCooldown
                ? "ClapYourHands.ManualClap.MenuCooldown".Translate(nameC)
                : "ClapYourHands.ManualClap.Menu".Translate(nameC);

            return new FloatMenuOption(label, () =>
            {
                actor.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ClapDefOf.Clap_ManualClap, clickedPawn));
            });
        }
    }
}
