using System;
using UnityEngine;
using Verse;

namespace ClapYourHands
{
    /// <summary>
    /// 手动击掌的音游判定条：全屏透明覆盖层，不暂停游戏。
    /// 判定完成时回调结算，并保留 2 秒展示（放大跳动 + 命中段高亮）后淡出。
    /// </summary>
    public class Window_ClapRhythm : Window
    {
        //镜头跳转后的停顿
        private const float IntroDelay = 0.35f;
        //淡入 / 淡出时长
        private const float FadeInDuration = 0.3f;
        private const float FadeOutDuration = 0.3f;
        //结果保留时长
        private const float ResultHoldDuration = 1f;
        //整条放大跳动
        private const float PopScale = 1.08f;
        private const float PopDuration = 0.2f;
        //命中段放大倍数
        private const float HitScale = 1.3f;

        //轨道布局
        private const float TrackWidthRatio = 0.5f;
        private const float TrackYRatio = 0.65f;
        private const float TrackHeight = 40f;
        private const float CursorWidth = 4f;
        private const float PerfectCenterMin = 0.25f;
        private const float PerfectCenterMax = 0.75f;

        private static readonly Color TrackBg = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color BorderColor = new Color(0.7f, 0.6f, 0.4f);
        private const float BorderThickness = 6f;
        private static readonly Color BadColor = new Color(0.35f, 0.05f, 0.05f);
        private static readonly Color NormalColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color GoodColor = new Color(0.15f, 0.4f, 0.85f);
        private static readonly Color PerfectColor = new Color(0.1f, 0.7f, 0.25f);

        private static readonly ClapOutcome[] OutcomeOrder =
        {
            ClapOutcome.Bad, ClapOutcome.Normal, ClapOutcome.Good, ClapOutcome.Perfect,
            ClapOutcome.Good, ClapOutcome.Normal, ClapOutcome.Bad,
        };

        private readonly Pawn initiator;
        private readonly Pawn recipient;
        private readonly Action<ClapOutcome> onResolved;
        private readonly float judgeDuration;

        private readonly Rect[] segmentRects = new Rect[7];
        private Rect trackRect;

        private float startRealtime;
        private float judgeStartRealtime;
        private ClapOutcome? resolved;
        private float resolvedRealtime;
        private int hitIndex = -1;
        private float cursorXAtResolve;

        //判定条整体尺寸覆盖全屏，完成阶段收缩到轨道区域
        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);
        protected override float Margin => 0f;

        public Window_ClapRhythm(Pawn initiator, Pawn recipient, Action<ClapOutcome> onResolved)
        {
            this.initiator = initiator;
            this.recipient = recipient;
            this.onResolved = onResolved;
            judgeDuration = ClapSettings.ClampJudgeDuration(
                ClapYourHandsMod.Settings?.JudgeDuration ?? ClapSettings.DefaultJudgeDuration);

            layer = WindowLayer.Super;
            forcePause = false;
            doWindowBackground = false;
            drawShadow = false;
            closeOnClickedOutside = false;
            closeOnCancel = false;
            doCloseX = false;
            doCloseButton = false;
            preventCameraMotion = false;
            absorbInputAroundWindow = true;
            onlyOneOfTypeAllowed = true;
            soundAppear = null;
            soundClose = null;

            windowRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            Layout();
            startRealtime = Now;
            judgeStartRealtime = startRealtime + IntroDelay + FadeInDuration;
        }

        private static float Now => (float)Time.realtimeSinceStartupAsDouble;

        //按概率比例把轨道分成 7 段：左三段与右三段各自按 糟糕/一般/不错 比例分配剩余宽度
        private void Layout()
        {
            float trackWidth = UI.screenWidth * TrackWidthRatio;
            float trackX = (UI.screenWidth - trackWidth) / 2f;
            float trackY = UI.screenHeight * TrackYRatio;
            trackRect = new Rect(trackX, trackY, trackWidth, TrackHeight);

            float[] probs = ClapUtility.GetOutcomeProbabilities(initiator, recipient);
            float perfectWidth = probs[3] * trackWidth;
            float perfectCenter = trackX + trackWidth * Rand.Range(PerfectCenterMin, PerfectCenterMax);
            perfectCenter = Mathf.Clamp(
                perfectCenter,
                trackX + perfectWidth / 2f,
                trackX + trackWidth - perfectWidth / 2f);

            float leftSpace = perfectCenter - perfectWidth / 2f - trackX;
            float rightSpace = trackX + trackWidth - perfectCenter - perfectWidth / 2f;
            float sideWeight = probs[0] + probs[1] + probs[2];

            if (sideWeight <= 0f)
            {
                leftSpace = rightSpace = (trackWidth - perfectWidth) / 2f;
                for (int i = 0; i < 3; i++)
                    probs[i] = 1f / 3f;
                sideWeight = 1f;
            }

            float x = trackX;
            for (int i = 0; i < 3; i++)
            {
                float w = leftSpace * probs[i] / sideWeight;
                segmentRects[i] = new Rect(x, trackY, w, TrackHeight);
                x += w;
            }

            float perfectStart = perfectCenter - perfectWidth / 2f;
            segmentRects[3] = new Rect(perfectStart, trackY, perfectWidth, TrackHeight);

            x = perfectCenter + perfectWidth / 2f;
            for (int i = 0; i < 3; i++)
            {
                int segIndex = 4 + i;
                int probIndex = 2 - i;
                float w = rightSpace * probs[probIndex] / sideWeight;
                segmentRects[segIndex] = new Rect(x, trackY, w, TrackHeight);
                x += w;
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            float now = Now;

            if (resolved is null)
            {
                //原版战斗式临时限速；判定结束后由 TimeSlower 自然过期
                Find.TickManager.slower.SignalForceNormalSpeed();

                if (now >= judgeStartRealtime + judgeDuration)
                {
                    Resolve(ClapOutcome.Bad, 6);
                }
                else if (now >= judgeStartRealtime && Input.GetMouseButtonDown(0))
                {
                    //判定位置取游标当前所在段，与画面显示保持一致
                    int index = HitTest(CursorPosition(now));
                    Resolve(index >= 0 ? OutcomeOrder[index] : ClapOutcome.Bad, index);
                }
            }
            else if (now >= resolvedRealtime + ResultHoldDuration + FadeOutDuration)
            {
                Close(false);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float now = Now;
            float alpha = Alpha(now);
            if (alpha <= 0f)
                return;

            float pop = 1f;
            if (resolved is not null)
            {
                float t = Mathf.Clamp01((now - resolvedRealtime) / PopDuration);
                pop = 1f + (PopScale - 1f) * Mathf.Sin(t * Mathf.PI);
            }

            Vector2 trackCenter = trackRect.center;
            Widgets.DrawBoxSolid(Local(ScaleRect(trackRect, trackCenter, pop)), WithAlpha(TrackBg, alpha));

            for (int i = 0; i < segmentRects.Length; i++)
            {
                if (resolved is not null && i == hitIndex)
                    continue;

                Rect rect = ScaleRect(segmentRects[i], trackCenter, pop);
                Widgets.DrawBoxSolid(Local(rect), WithAlpha(SegmentColor(OutcomeOrder[i]), alpha));
            }

            DrawBorder(Local(ScaleRect(trackRect, trackCenter, pop)), WithAlpha(BorderColor, alpha));

            //命中段最后绘制并放大，保证在其他判定区之上
            if (resolved is not null && hitIndex >= 0 && hitIndex < segmentRects.Length)
            {
                Rect hit = ScaleRect(segmentRects[hitIndex], trackCenter, pop);
                hit = ScaleRect(hit, segmentRects[hitIndex].center, HitScale);
                Widgets.DrawBoxSolid(Local(hit), WithAlpha(SegmentColor(OutcomeOrder[hitIndex]), alpha));
            }

            float cursorX = resolved is not null ? cursorXAtResolve : CursorPosition(now);
            Rect cursor = new Rect(cursorX - CursorWidth / 2f, trackRect.y - 10f, CursorWidth, trackRect.height + 20f);
            Widgets.DrawBoxSolid(Local(ScaleRect(cursor, trackCenter, pop)), WithAlpha(Color.white, alpha));

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;

            Rect labelRect = new Rect(trackRect.x, trackRect.y - 56f, trackRect.width, 40f);
            if (resolved is null)
            {
                if (now >= judgeStartRealtime)
                    Widgets.Label(Local(labelRect), "ClapYourHands.Rhythm.Click".Translate());
            }
            else
            {
                Widgets.Label(Local(labelRect), ResultLabel(resolved.Value));
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        public override void OnCancelKeyPressed()
        {
            //判定期间忽略取消键
        }

        private void Resolve(ClapOutcome outcome, int index)
        {
            if (resolved is not null)
                return;

            resolved = outcome;
            hitIndex = index;
            cursorXAtResolve = index >= 0
                ? Mathf.Clamp(CursorPosition(Now), segmentRects[index].xMin, segmentRects[index].xMax)
                : trackRect.xMax;
            resolvedRealtime = Now;

            absorbInputAroundWindow = false;
            windowRect = new Rect(
                trackRect.x - 40f,
                trackRect.y - 80f,
                trackRect.width + 80f,
                trackRect.height + 180f).Rounded();

            onResolved?.Invoke(outcome);
        }

        private float CursorPosition(float now)
        {
            float t = Mathf.Clamp01((now - judgeStartRealtime) / judgeDuration);
            return trackRect.x + trackRect.width * t;
        }

        private int HitTest(float x)
        {
            for (int i = 0; i < segmentRects.Length; i++)
            {
                if (x >= segmentRects[i].xMin && x <= segmentRects[i].xMax)
                    return i;
            }

            return -1;
        }

        private float Alpha(float now)
        {
            if (resolved is null)
            {
                float t = (now - startRealtime - IntroDelay) / FadeInDuration;
                return Mathf.Clamp01(t);
            }

            float fadeStart = resolvedRealtime + ResultHoldDuration;
            return 1f - Mathf.Clamp01((now - fadeStart) / FadeOutDuration);
        }

        private Rect Local(Rect screenRect) =>
            new Rect(
                screenRect.x - windowRect.x,
                screenRect.y - windowRect.y,
                screenRect.width,
                screenRect.height);

        private static Rect ScaleRect(Rect rect, Vector2 pivot, float scale) =>
            new Rect(
                pivot.x - (pivot.x - rect.x) * scale,
                pivot.y - (pivot.y - rect.y) * scale,
                rect.width * scale,
                rect.height * scale);

        private static void DrawBorder(Rect rect, Color color)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, BorderThickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - BorderThickness, rect.width, BorderThickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, BorderThickness, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - BorderThickness, rect.y, BorderThickness, rect.height), color);
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, color.a * alpha);

        private static Color SegmentColor(ClapOutcome outcome) =>
            outcome switch
            {
                ClapOutcome.Bad => BadColor,
                ClapOutcome.Normal => NormalColor,
                ClapOutcome.Good => GoodColor,
                _ => PerfectColor,
            };

        private static TaggedString ResultLabel(ClapOutcome outcome) =>
            (outcome switch
            {
                ClapOutcome.Bad => "ClapYourHands.Rhythm.Bad".Translate(),
                ClapOutcome.Normal => "ClapYourHands.Rhythm.Normal".Translate(),
                ClapOutcome.Good => "ClapYourHands.Rhythm.Good".Translate(),
                _ => "ClapYourHands.Rhythm.Perfect".Translate(),
            }).Colorize(SegmentColor(outcome));
    }
}
