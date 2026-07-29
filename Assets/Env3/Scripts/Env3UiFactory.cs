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
