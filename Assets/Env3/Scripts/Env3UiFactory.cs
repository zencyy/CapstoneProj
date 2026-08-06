using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Env3.Anxiety
{
    /// <summary>
    /// Builds the couple of sprites the anxiety dialogue UI needs.
    /// The scene wires up the generated .png assets under Assets/Env3/UI, but the
    /// system falls back to these at runtime so it never hard-fails on a missing sprite.
    /// </summary>
    public static class Env3UiFactory
    {
        public const int RoundedSize = 64;
        public const int RoundedRadius = 16;

        static Sprite _roundedRect;
        static Sprite _vignette;
        static Sprite _softRadial;
        static Sprite _linedPaper;

        /// <summary>Font materials already styled, so a flood of thoughts does not redo the work per thought.</summary>
        static readonly HashSet<int> _styledMaterials = new HashSet<int>();

        /// <summary>One shared styled copy per font asset, keyed by the font's instance ID.</summary>
        static readonly Dictionary<int, Material> _sharedFontMaterials = new Dictionary<int, Material>();

        /// <summary>
        /// Makes a TMP label readable over the rooftop.
        ///
        /// Two things were fighting legibility. The font material this scene uses carries a
        /// black _FaceColor, and the distance field shader multiplies face by vertex colour,
        /// so a label rendered black however white the component asked to be. And even once
        /// white, thin text over a bright sunset sky washes out. So: force the face white and
        /// let the per-label vertex colour be the only thing that tints a line, then add a dark
        /// outline and a soft drop shadow underneath it.
        /// </summary>
        public static void MakeLegible(TMP_Text text, float outlineWidth = 0.14f, float shadowSoftness = 0.45f)
        {
            if (text == null) return;

            // Only ever touch a material this scene owns. If the label is still on the font
            // asset's own default material, take a per-label instance instead, so the fix
            // cannot leak out into every other scene in the project.
            var mat = text.fontSharedMaterial;
            if (mat == null || (text.font != null && mat == text.font.material)) mat = text.fontMaterial;

            StyleMaterial(mat, outlineWidth, shadowSoftness);

            // The outline and shadow draw outside the glyph, so the mesh needs room for them.
            text.UpdateMeshPadding();
        }

        /// <summary>
        /// Moves a label onto <paramref name="font"/> and onto a single shared styled copy of that
        /// font's material.
        ///
        /// Shared rather than per-label, so a screen full of choices still batches instead of
        /// costing a draw call each. A copy rather than the font's own material, because the font
        /// asset is used by other scenes in the project and styling its material would change
        /// their text too.
        /// </summary>
        public static void ApplySharedFont(TMP_Text text, TMP_FontAsset font, float outlineWidth = 0f, float shadowSoftness = 0.5f)
        {
            if (text == null || font == null) return;

            Material shared;
            int key = font.GetInstanceID();
            if (!_sharedFontMaterials.TryGetValue(key, out shared) || shared == null)
            {
                shared = new Material(font.material) { name = font.name + " (Env3)" };
                StyleMaterial(shared, outlineWidth, shadowSoftness);
                _sharedFontMaterials[key] = shared;
            }

            text.font = font;
            text.fontSharedMaterial = shared;
            text.UpdateMeshPadding();
        }

        /// <summary>
        /// White face, optional dark outline, soft drop shadow. An <paramref name="outlineWidth"/>
        /// of zero leaves the glyph edge flat, which is what the rest of the project's subtitles do.
        /// </summary>
        static void StyleMaterial(Material mat, float outlineWidth, float shadowSoftness)
        {
            if (mat == null || !_styledMaterials.Add(mat.GetInstanceID())) return;

            if (mat.HasProperty(ShaderUtilities.ID_FaceColor))
                mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);

            if (mat.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                if (outlineWidth > 0f)
                {
                    mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.9f));
                    mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
                    mat.EnableKeyword("OUTLINE_ON");
                }
                else
                {
                    mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                    mat.DisableKeyword("OUTLINE_ON");
                }
            }

            if (mat.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.75f));
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, shadowSoftness);
                mat.EnableKeyword("UNDERLAY_ON");
            }
        }

        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect == null)
                {
                    var tex = BuildRoundedRectTexture(RoundedSize, RoundedRadius);
                    tex.name = "Env3_RoundedRect";
                    _roundedRect = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                        100f, 0, SpriteMeshType.FullRect,
                        new Vector4(RoundedRadius, RoundedRadius, RoundedRadius, RoundedRadius));
                    _roundedRect.name = "Env3_RoundedRect";
                }
                return _roundedRect;
            }
        }

        /// <summary>Soft dark cloud behind the text. Fades out at the edges so the panel has no visible rectangle.</summary>
        public static Sprite SoftRadial
        {
            get
            {
                if (_softRadial == null)
                {
                    var tex = BuildSoftRadialTexture(256);
                    tex.name = "Env3_SoftRadial";
                    _softRadial = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    _softRadial.name = "Env3_SoftRadial";
                }
                return _softRadial;
            }
        }

        /// <summary>
        /// A strip of ruled notepaper, matching the photographed paper buttons on the main menu.
        /// Drawn rather than photographed, so the label on top stays live text: the menu's own
        /// buttons have their words baked into the image and there is no blank plate to reuse.
        /// </summary>
        public static Sprite LinedPaper
        {
            get
            {
                if (_linedPaper == null)
                {
                    var tex = BuildLinedPaperTexture(512, 160);
                    tex.name = "Env3_LinedPaper";
                    _linedPaper = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    _linedPaper.name = "Env3_LinedPaper";
                }
                return _linedPaper;
            }
        }

        /// <summary>
        /// Off-white paper, blue horizontal rules, a red margin down the left, and a faint
        /// mottle so it does not read as a flat swatch. Not 9-sliced: stretching it would pull
        /// the rules apart, so it is drawn at the strip's own aspect and used Simple.
        /// </summary>
        public static Texture2D BuildLinedPaperTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var paper = new Color(0.957f, 0.949f, 0.918f);
            var rule = new Color(0.639f, 0.729f, 0.843f);
            var margin = new Color(0.855f, 0.451f, 0.478f);

            // Four rules across the strip, with the first sitting a little below the top edge.
            float spacing = height / 4.6f;
            float firstRule = spacing * 0.75f;
            float marginX = width * 0.075f;
            float seed = 11.3f;

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Large soft blotches plus fine grain, both very low contrast.
                    float blotch = Mathf.PerlinNoise(seed + x * 0.012f, seed + y * 0.012f) - 0.5f;
                    float grain = Mathf.PerlinNoise(seed + x * 0.9f, seed + y * 0.9f) - 0.5f;
                    var c = paper * (1f + blotch * 0.05f + grain * 0.022f);
                    c.a = 1f;

                    // Distance to the nearest rule, so the line gets a soft edge instead of aliasing.
                    float fromFirst = (height - 1 - y) - firstRule;
                    float nearest = Mathf.Abs(fromFirst - Mathf.Round(fromFirst / spacing) * spacing);
                    if (fromFirst > -spacing)
                        c = Color.Lerp(c, rule, Mathf.Clamp01(1f - nearest / 1.1f) * 0.85f);

                    float toMargin = Mathf.Abs(x - marginX);
                    c = Color.Lerp(c, margin, Mathf.Clamp01(1f - toMargin / 1.3f) * 0.8f);

                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        public static Sprite Vignette
        {
            get
            {
                if (_vignette == null)
                {
                    var tex = BuildVignetteTexture(256);
                    tex.name = "Env3_Vignette";
                    _vignette = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    _vignette.name = "Env3_Vignette";
                }
                return _vignette;
            }
        }

        /// <summary>White rounded rectangle, alpha-feathered at the corners, ready to 9-slice.</summary>
        public static Texture2D BuildRoundedRectTexture(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance outside the inner (corner-radius inset) rectangle.
                    float dx = Mathf.Max(radius - (x + 0.5f), 0f, (x + 0.5f) - (size - radius));
                    float dy = Mathf.Max(radius - (y + 0.5f), 0f, (y + 0.5f) - (size - radius));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>Radial ramp: opaque in the middle, transparent at the edges. The inverse of the vignette.</summary>
        public static Texture2D BuildSoftRadialTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 1f, r));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        /// <summary>Radial ramp: transparent in the middle, opaque at the edges.</summary>
        public static Texture2D BuildVignetteTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.41421356f;
                    // The ramp finishes at 0.35, far inside the quad's edge midpoint (0.707),
                    // for two reasons. The border is then solidly opaque, so the quad's own
                    // rectangular outline never shows against the world. And full darkness
                    // lands well within the field of view instead of only at the corners,
                    // which is what actually sells the tunnel.
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.13f, 0.35f, r));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}
