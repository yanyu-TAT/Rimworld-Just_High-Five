using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ClapYourHands
{
    /// <summary>完美击掌的表现：双方中点处的音效、闪电纹样与溅落的火花。</summary>
    public static class ClapEffects
    {
        //溅落的火花数量
        private const int SparkCount = 12;

        //闪电纹样的尺寸随机范围
        private const float BoltScaleMin = 0.9f;
        private const float BoltScaleMax = 1.1f;

        //外发光层相对核心层的放大倍率
        private const float GlowScaleFactor = 1.25f;

        //火花水平初速的随机范围（格/秒）
        private const float SparkSpeedMin = 1.4f;
        private const float SparkSpeedMax = 2.6f;

        //音量缩放系数；设置未加载时按原音量
        private static float VolumeScale => ClapYourHandsMod.Settings?.VolumeScale ?? 0.5f;

        /// <summary>在双方中点播放完美击掌的音效与特效；任一方不可用时直接跳过。</summary>
        public static void PlayPerfect(Pawn initiator, Pawn recipient)
        {
            Map map = initiator?.Map;
            if (map is null || recipient is null || recipient.Map != map
                || !initiator.Spawned || !recipient.Spawned)
            {
                return;
            }

            Vector3 mid = (initiator.DrawPos + recipient.DrawPos) * 0.5f;
            PlaySound(mid, map);

            if (!mid.ShouldSpawnMotesAt(map))
                return;

            if (ClapDefOf.Clap_BlackFlashBoltCore is null || ClapDefOf.Clap_BlackFlashBoltGlow is null
                || ClapDefOf.Clap_HandSpark is null)
            {
                Log.Error($"{ClapYourHandsMod.LogPrefix}黑闪 FleckDef 未加载，已跳过特效。");
                return;
            }

            //两层共用同一朝向，外发光略大，形成「暗红核心 + 亮红外圈」
            float rotation = Rand.Range(0f, 360f);
            float scale = Rand.Range(BoltScaleMin, BoltScaleMax);
            ThrowBolt(mid, map, ClapDefOf.Clap_BlackFlashBoltGlow, scale * GlowScaleFactor, rotation);
            ThrowBolt(mid, map, ClapDefOf.Clap_BlackFlashBoltCore, scale, rotation);

            for (int i = 0; i < SparkCount; i++)
                ThrowSpark(mid, map);
        }

        /// <summary>在镜头中心试听一次音效；由模组设置页的试听按钮调用。</summary>
        public static void PlayTestSound()
        {
            if (ClapDefOf.Clap_HandClap is null)
            {
                Log.Error($"{ClapYourHandsMod.LogPrefix}击掌音效 Def 未加载，已跳过试听。");
                return;
            }

            Map map = Find.CurrentMap;
            if (map is null)
            {
                Messages.Message("ClapYourHands.Settings.TestNeedsMap".Translate(),
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            //音效 Def 的 subSound 不是 onCamera，音源位置取自 SoundInfo 的位置，故放在镜头中心
            SoundInfo info = SoundInfo.InMap(new TargetInfo(Find.CameraDriver.MapPosition, map));
            info.volumeFactor = VolumeScale;
            info.testPlay = true;
            ClapDefOf.Clap_HandClap.PlayOneShot(info);
        }

        //播放音效；变体由原版 AudioGrain_Folder 随机取一，基准音量与音高由 Def 的 Range 决定
        private static void PlaySound(Vector3 mid, Map map)
        {
            if (ClapDefOf.Clap_HandClap is null)
            {
                Log.Error($"{ClapYourHandsMod.LogPrefix}击掌音效 Def 未加载，已跳过音效。");
                return;
            }

            SoundInfo info = SoundInfo.InMap(new TargetInfo(mid.ToIntVec3(), map));
            info.volumeFactor = VolumeScale;
            ClapDefOf.Clap_HandClap.PlayOneShot(info);
        }

        private static void ThrowBolt(Vector3 mid, Map map, FleckDef def, float scale, float rotation)
        {
            FleckCreationData bolt = FleckMaker.GetDataStatic(mid, map, def, scale);
            bolt.rotation = rotation;
            map.flecks.CreateFleck(bolt);
        }

        //水平方向随机飞散；抛物线拱与空气阻力由 Clap_HandSpark 的 Def 字段决定
        private static void ThrowSpark(Vector3 mid, Map map)
        {
            FleckCreationData spark = FleckMaker.GetDataStatic(
                mid, map, ClapDefOf.Clap_HandSpark, Rand.Range(0.8f, 1.2f));
            spark.velocityAngle = Rand.Range(0f, 360f);
            spark.velocitySpeed = Rand.Range(SparkSpeedMin, SparkSpeedMax);
            spark.rotation = Rand.Range(-20f, 20f);
            spark.rotationRate = Rand.Range(-90f, 90f);
            map.flecks.CreateFleck(spark);
        }
    }
}
