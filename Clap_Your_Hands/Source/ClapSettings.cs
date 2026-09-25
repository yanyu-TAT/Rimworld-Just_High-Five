using UnityEngine;
using Verse;

namespace ClapYourHands
{
    /// <summary>模组设置：完美击掌音效的音量缩放。</summary>
    public class ClapSettings : ModSettings
    {
        //音量缩放系数的可选范围
        public const float VolumeScaleMin = 0f;
        public const float VolumeScaleMax = 1f;

        //音量滑块的步进
        private const float VolumeScaleStep = 0.05f;

        //滑块高度
        private const float SliderHeight = 24f;

        //音量缩放系数，1 为音效 Def 的原音量
        public float VolumeScale = 0.5f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref VolumeScale, "volumeScale", 0.5f);
        }

        /// <summary>绘制模组设置页内容；由原版模组菜单的「Mod 选项」打开。</summary>
        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("ClapYourHands.Settings.Volume".Translate(VolumeScale.ToStringPercent()));

            listing.Gap(12f);

            Rect sliderRect = listing.GetRect(SliderHeight);
            VolumeScale = Widgets.HorizontalSlider(
                sliderRect, VolumeScale, VolumeScaleMin, VolumeScaleMax,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: VolumeScaleMin.ToStringPercent(),
                rightAlignedLabel: VolumeScaleMax.ToStringPercent(),
                roundTo: VolumeScaleStep);

            listing.Gap(12f);

            if (Find.CurrentMap != null)
            {
                if (listing.ButtonText("ClapYourHands.Settings.Test".Translate()))
                    ClapEffects.PlayTestSound();
            }

            listing.End();
        }
    }
}
