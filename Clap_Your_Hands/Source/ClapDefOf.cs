using RimWorld;
using Verse;
using Verse.Sound;

namespace ClapYourHands
{
    /// <summary>Def 引用缓存。字段名必须与 Defs 中的 defName 一致。</summary>
    [DefOf]
    public static class ClapDefOf
    {
        public static InteractionDef Clap;
        public static ThoughtDef Clap_Bad;
        public static ThoughtDef Clap_Normal;
        public static ThoughtDef Clap_Good;
        public static ThoughtDef Clap_Perfect;
        public static HediffDef Clap_Buff;
        public static HediffDef Clap_Cooldown;
        public static FleckDef Clap_BlackFlashBoltCore;
        public static FleckDef Clap_BlackFlashBoltGlow;
        public static FleckDef Clap_HandSpark;
        public static SoundDef Clap_HandClap;

        static ClapDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ClapDefOf));
        }
    }
}
