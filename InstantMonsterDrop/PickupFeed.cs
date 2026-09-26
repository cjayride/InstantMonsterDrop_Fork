using System.Collections.Generic;
using UnityEngine;

namespace InstantMonsterDrop
{
    public class PickupFeed : MonoBehaviour
    {
        private const float FadeOut = 0.8f;

        private class Line
        {
            public string Name;
            public int Amount;
            public Texture Icon;
            public Rect Uv;
            public float Age;
            public float Punch;
            public float Slide;
        }

        private readonly List<Line> lines = new List<Line>();
        private Texture2D pixel;
        private GUIStyle nameStyle;
        private GUIStyle amountStyle;
        private float panelAlpha;
        private int cachedFontSize = -1;

        public static PickupFeed Instance { get; private set; }

        private static float LineHeight => BepInExPlugin.NotificationFontSize + 9f;

        private void Awake()
        {
            Instance = this;
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            pixel.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (pixel != null)
                Destroy(pixel);
        }

        public void Add(ItemDrop.ItemData item, int amount)
        {
            if (!BepInExPlugin.NotificationEnabled)
                return;
            if (item == null || item.m_shared == null || amount <= 0)
                return;

            string name = item.m_shared.m_name;
            if (Localization.instance != null)
                name = Localization.instance.Localize(name);
            if (string.IsNullOrEmpty(name))
                name = item.m_dropPrefab != null ? item.m_dropPrefab.name : "Item";

            Texture icon = null;
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            Sprite[] icons = item.m_shared.m_icons;
            if (icons != null && icons.Length > 0 && icons[0] != null)
            {
                Sprite sprite = icons[Mathf.Clamp(item.m_variant, 0, icons.Length - 1)];
                if (sprite != null && sprite.texture != null)
                {
                    icon = sprite.texture;
                    Rect tr = sprite.textureRect;
                    float tw = sprite.texture.width;
                    float th = sprite.texture.height;
                    if (tw > 0f && th > 0f && tr.width > 0f && tr.height > 0f)
                        uv = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
                }
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Name == name)
                {
                    lines[i].Amount += amount;
                    lines[i].Age = 0f;
                    lines[i].Punch = 1f;
                    Line hit = lines[i];
                    lines.RemoveAt(i);
                    lines.Add(hit);
                    return;
                }
            }

            lines.Add(new Line
            {
                Name = name,
                Amount = amount,
                Icon = icon,
                Uv = uv,
                Age = 0f,
                Punch = 1f,
                Slide = 0f
            });

            float height = BepInExPlugin.NotificationHeight;
            int maxLines = Mathf.Max(1, Mathf.FloorToInt((height - 16f) / LineHeight));
            while (lines.Count > maxLines)
                lines.RemoveAt(0);
        }

        private void Update()
        {
            if (!BepInExPlugin.NotificationEnabled)
            {
                lines.Clear();
                panelAlpha = 0f;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            float lifetime = BepInExPlugin.NotificationLinger;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                Line line = lines[i];
                line.Age += dt;
                line.Punch = Mathf.MoveTowards(line.Punch, 0f, dt * 4f);
                line.Slide = Mathf.MoveTowards(line.Slide, 1f, dt * 8f);
                if (line.Age >= lifetime)
                    lines.RemoveAt(i);
            }

            float target = lines.Count > 0 ? 1f : 0f;
            panelAlpha = Mathf.MoveTowards(panelAlpha, target, dt * 5f);
        }

        private void OnGUI()
        {
            if (!BepInExPlugin.ModEnabled || !BepInExPlugin.NotificationEnabled)
                return;
            if (panelAlpha <= 0.01f)
                return;
            if (Player.m_localPlayer == null)
                return;
            if (Hud.instance != null && Hud.IsUserHidden())
                return;

            EnsureStyles();

            float width = BepInExPlugin.NotificationWidth;
            float height = BepInExPlugin.NotificationHeight;
            GetWindowRect(width, height, out float x, out float y);
            Rect window = new Rect(x, y, width, height);
            float opacity = BepInExPlugin.NotificationOpacity * panelAlpha;
            bool fromRight = BepInExPlugin.Anchor == NotificationAnchor.BottomRight || BepInExPlugin.Anchor == NotificationAnchor.TopRight;
            float amountWidth = AmountColumnWidth(width);

            Color old = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, opacity);
            GUI.DrawTexture(window, pixel);
            if (BepInExPlugin.ShowBorders)
            {
                GUI.color = new Color(0.82f, 0.68f, 0.32f, Mathf.Min(1f, opacity + 0.12f));
                float barX = fromRight ? window.xMax - 3f : window.x;
                GUI.DrawTexture(new Rect(barX, window.y, 3f, window.height), pixel);
                GUI.color = new Color(1f, 1f, 1f, 0.1f * panelAlpha);
                GUI.DrawTexture(new Rect(window.x, window.y, window.width, 1f), pixel);
            }

            float lineHeight = LineHeight;
            float lifetime = BepInExPlugin.NotificationLinger;
            GUI.BeginGroup(window);
            float rowY = window.height - 8f;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                Line line = lines[i];
                rowY -= lineHeight;
                if (rowY < 6f)
                    break;

                float fade = 1f;
                if (line.Age > lifetime - FadeOut)
                    fade = Mathf.Clamp01((lifetime - line.Age) / FadeOut);
                fade *= panelAlpha;

                float slideX = (1f - line.Slide) * 36f;
                if (!fromRight)
                    slideX = -slideX;
                float punch = 1f + line.Punch * 0.08f;
                Rect row = new Rect(10f + slideX, rowY, window.width - 18f, lineHeight);

                GUI.color = new Color(1f, 1f, 1f, fade);
                if (line.Icon != null)
                {
                    float iconSize = (lineHeight - 6f) * punch;
                    Rect iconRect = new Rect(row.x, row.y + (lineHeight - iconSize) * 0.5f, iconSize, iconSize);
                    GUI.DrawTextureWithTexCoords(iconRect, line.Icon, line.Uv);
                }

                float iconPad = line.Icon != null ? lineHeight : 4f;
                Rect nameRect = new Rect(row.x + iconPad, row.y, Mathf.Max(20f, row.width - iconPad - amountWidth - 4f), lineHeight);
                Rect amountRect = new Rect(row.x + row.width - amountWidth, row.y, amountWidth, lineHeight);
                Color nameColor = BepInExPlugin.TextColor;
                Color countColor = BepInExPlugin.AmountColor;
                nameColor.a *= fade;
                countColor.a *= fade;
                DrawLabel(nameRect, line.Name, nameStyle, nameColor, fade);
                DrawLabel(amountRect, "x" + line.Amount, amountStyle, countColor, fade);
            }
            GUI.EndGroup();
            GUI.color = old;
        }

        private static float AmountColumnWidth(float windowWidth)
        {
            float fromFont = BepInExPlugin.NotificationFontSize * 3.4f + 16f;
            float fromWindow = windowWidth * 0.28f;
            return Mathf.Clamp(Mathf.Max(fromFont, fromWindow), 56f, windowWidth * 0.45f);
        }

        private void DrawLabel(Rect rect, string text, GUIStyle style, Color color, float fade)
        {
            if (BepInExPlugin.TextOutline)
            {
                Color outline = BepInExPlugin.OutlineColor;
                outline.a *= fade;
                style.normal.textColor = outline;
                float o = Mathf.Max(1f, BepInExPlugin.NotificationFontSize / 14f);
                GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), text, style);
                GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), text, style);
                GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), text, style);
                GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), text, style);
            }

            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private static void GetWindowRect(float width, float height, out float x, out float y)
        {
            float ox = BepInExPlugin.NotificationX;
            float oy = BepInExPlugin.NotificationY;
            switch (BepInExPlugin.Anchor)
            {
                case NotificationAnchor.BottomLeft:
                    x = ox;
                    y = Screen.height - height - oy;
                    break;
                case NotificationAnchor.TopRight:
                    x = Screen.width - width - ox;
                    y = oy;
                    break;
                case NotificationAnchor.TopLeft:
                    x = ox;
                    y = oy;
                    break;
                default:
                    x = Screen.width - width - ox;
                    y = Screen.height - height - oy;
                    break;
            }

            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - width));
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - height));
        }

        private void EnsureStyles()
        {
            int fontSize = BepInExPlugin.NotificationFontSize;
            if (nameStyle != null && cachedFontSize == fontSize)
                return;

            cachedFontSize = fontSize;
            nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            amountStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Overflow,
                wordWrap = false
            };
        }
    }
}
