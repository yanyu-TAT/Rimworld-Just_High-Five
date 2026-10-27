using System;
using UnityEngine;
using Verse;

namespace ClapYourHands
{
    /// <summary>设置页的绘制风格：标题条、行游标、行底细条与底部条。</summary>
    public static class ClapSettingsUI
    {
        public const float TitleHeight = 30f;
        public const float RowHeight = 28f;
        public const float RowGap = 6f;
        public const float SliderHeight = 24f;
        public const float Indent = 12f;
        public const float BorderThickness = 2f;

        public static readonly Color TitleBarBg = new Color(14f / 255f, 16f / 255f, 39f / 255f);
        public static readonly Color Accent = new Color(196f / 255f, 149f / 255f, 82f / 255f);
        public static readonly Color TextNormal = new Color(0.8f, 0.8f, 0.9f);
        public static readonly Color RowLine = new Color(1f, 1f, 1f, 0.07f);

        /// <summary>纵向游标：按行取矩形并推进。</summary>
        public struct Cursor
        {
            public float X;
            public float Y;
            public float Width;

            public Cursor(float x, float y, float width)
            {
                X = x;
                Y = y;
                Width = width;
            }

            public Cursor(Rect rect)
                : this(rect.x, rect.y, rect.width)
            {
            }

            public Rect Row(float height) => Row(X, Width, height);

            public Rect Row(float x, float width, float height)
            {
                Rect rect = new Rect(x, Y, width, height);
                Y += height;
                return rect;
            }

            public void Gap(float px) => Y += px;
        }

        public readonly struct TextScope : IDisposable
        {
            private readonly GameFont _font;
            private readonly TextAnchor _anchor;

            public TextScope(GameFont font, TextAnchor anchor)
            {
                _font = Text.Font;
                _anchor = Text.Anchor;
                Text.Font = font;
                Text.Anchor = anchor;
            }

            public void Dispose()
            {
                Text.Font = _font;
                Text.Anchor = _anchor;
            }
        }

        public readonly struct ColorScope : IDisposable
        {
            private readonly Color _color;

            public ColorScope(Color color)
            {
                _color = GUI.color;
                GUI.color = color;
            }

            public void Dispose() => GUI.color = _color;
        }

        /// <summary>分组标题条：整行深色底、左侧金色竖条、行首展开符号与标题。</summary>
        public static void DrawFoldHeader(Rect rect, string label)
        {
            Widgets.DrawBoxSolid(rect, TitleBarBg);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 3f, rect.height), Accent);

            const float iconSize = 18f;
            Rect iconRect = new Rect(rect.x + 9f, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);
            Texture2D icon = ContentFinder<Texture2D>.Get("UI/Buttons/Minus", false);
            if (icon is not null)
            {
                GUI.DrawTexture(iconRect, icon);
            }
            else
            {
                using (new TextScope(GameFont.Small, TextAnchor.MiddleCenter))
                using (new ColorScope(Accent))
                    Widgets.Label(iconRect, "-");
            }

            Rect labelRect = new Rect(
                iconRect.xMax + 7f,
                rect.y,
                Mathf.Max(rect.xMax - iconRect.xMax - 7f, 1f),
                rect.height);
            using (new TextScope(GameFont.Small, TextAnchor.MiddleLeft))
            using (new ColorScope(TextNormal))
                Widgets.Label(labelRect, label);
        }

        /// <summary>行底细条。</summary>
        public static void DrawRowSeparator(Rect row) =>
            Widgets.DrawBoxSolid(new Rect(row.x, row.yMax - 1f, row.width, 1f), RowLine);

        /// <summary>页面底部条：深色底 + 顶部金色细线。</summary>
        public static void DrawFooter(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, TitleBarBg);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), Accent);
        }

        /// <summary>行标题：小号字体、左对齐、常规文本色。</summary>
        public static void DrawLabel(Rect rect, string text)
        {
            using (new TextScope(GameFont.Small, TextAnchor.MiddleLeft))
            using (new ColorScope(TextNormal))
                Widgets.Label(rect, text);
        }
    }
}
