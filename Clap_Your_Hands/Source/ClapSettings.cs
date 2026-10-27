using UnityEngine;
using Verse;

namespace ClapYourHands
{
    /// <summary>模组设置：完美击掌音效音量与手动击掌判定时长。</summary>
    public class ClapSettings : ModSettings
    {
        //音量缩放系数的可选范围
        public const float VolumeScaleMin = 0f;
        public const float VolumeScaleMax = 1f;
        public const float VolumeDefault = 0.5f;

        //音量滑块的步进
        private const float VolumeScaleStep = 0.05f;

        //手动击掌判定时长的可选范围（秒）
        public const float JudgeDurationMin = 0.1f;
        public const float JudgeDurationMax = 5f;
        public const float JudgeDurationStep = 0.1f;
        public const float DefaultJudgeDuration = 2.5f;

        //行内按钮宽度
        private const float InlineButtonWidth = 90f;

        //音量缩放系数，1 为音效 Def 的原音量
        public float VolumeScale = VolumeDefault;

        //手动击掌的判定时长（秒）
        public float JudgeDuration = DefaultJudgeDuration;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref VolumeScale, "volumeScale", 0.5f);
            Scribe_Values.Look(ref JudgeDuration, "judgeDuration", DefaultJudgeDuration);
        }

        /// <summary>把判定时长限制在可选范围内。</summary>
        public static float ClampJudgeDuration(float value) =>
            Mathf.Clamp(value, JudgeDurationMin, JudgeDurationMax);

        /// <summary>绘制模组设置页内容；由原版模组菜单的「Mod 选项」打开。</summary>
        public void DoWindowContents(Rect inRect)
        {
            const float headerGap = 6f;
            const float rowGap = 4f;
            const float groupGap = 10f;

            Rect body = inRect;

            float x = body.x + ClapSettingsUI.Indent;
            float width = body.width - ClapSettingsUI.Indent * 2f;
            float y = body.y;

            DrawVolumeGroup(body, x, width, ref y, headerGap, rowGap);
            y += groupGap;
            DrawManualClapGroup(body, x, width, ref y, headerGap, rowGap);
        }

        private void DrawVolumeGroup(Rect body, float x, float width, ref float y, float headerGap, float rowGap)
        {
            ClapSettingsUI.DrawFoldHeader(
                new Rect(body.x, y, body.width, ClapSettingsUI.TitleHeight),
                "ClapYourHands.Settings.VolumeGroup".Translate());
            y += ClapSettingsUI.TitleHeight + headerGap;

            Rect label = new Rect(x, y, width, ClapSettingsUI.RowHeight);
            ClapSettingsUI.DrawLabel(label, "ClapYourHands.Settings.Volume".Translate(VolumeScale.ToStringPercent()));
            ClapSettingsUI.DrawRowSeparator(label);
            y += ClapSettingsUI.RowHeight + rowGap;

            Rect slider = new Rect(x, y, width, ClapSettingsUI.SliderHeight);
            bool hasTestButton = Find.CurrentMap != null;
            float sliderWidth = hasTestButton ? slider.width - InlineButtonWidth - 8f : slider.width;
            Rect sliderPart = new Rect(slider.x, slider.y, sliderWidth, slider.height);

            VolumeScale = Widgets.HorizontalSlider(
                sliderPart, VolumeScale, VolumeScaleMin, VolumeScaleMax,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: null,
                rightAlignedLabel: null,
                roundTo: VolumeScaleStep);

            if (hasTestButton)
            {
                Rect testButton = new Rect(slider.xMax - InlineButtonWidth, slider.y, InlineButtonWidth, slider.height);
                if (Widgets.ButtonText(testButton, "ClapYourHands.Settings.Test".Translate()))
                    ClapEffects.PlayTestSound();
            }

            ClapSettingsUI.DrawRowSeparator(slider);
            y += ClapSettingsUI.SliderHeight;
        }

        private void DrawManualClapGroup(Rect body, float x, float width, ref float y, float headerGap, float rowGap)
        {
            ClapSettingsUI.DrawFoldHeader(
                new Rect(body.x, y, body.width, ClapSettingsUI.TitleHeight),
                "ClapYourHands.Settings.ManualClapGroup".Translate());
            y += ClapSettingsUI.TitleHeight + headerGap;

            Rect label = new Rect(x, y, width, ClapSettingsUI.RowHeight);
            ClapSettingsUI.DrawLabel(label, "ClapYourHands.Settings.JudgeDuration".Translate(JudgeDuration.ToString("0.0")));
            ClapSettingsUI.DrawRowSeparator(label);
            y += ClapSettingsUI.RowHeight + rowGap;

            Rect slider = new Rect(x, y, width, ClapSettingsUI.SliderHeight);
            JudgeDuration = Widgets.HorizontalSlider(
                slider, JudgeDuration, JudgeDurationMin, JudgeDurationMax,
                middleAlignment: false,
                label: null,
                leftAlignedLabel: null,
                rightAlignedLabel: null,
                roundTo: JudgeDurationStep);
            ClapSettingsUI.DrawRowSeparator(slider);
            y += ClapSettingsUI.SliderHeight;
        }
    }
}
