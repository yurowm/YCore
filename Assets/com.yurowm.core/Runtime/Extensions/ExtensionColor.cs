using System;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm.Extensions {
    public static class ColorExtensions {
        public static Color Multiply(this Color color, Color color2) {
            return new Color(color.r * color2.r, color.g * color2.g, color.b * color2.b, color.a * color2.a);
        }
        
        public static Color32 Multiply(this Color32 color, Color32 color2) {
            return new Color32(
                (byte) (color.r * color2.r), 
                (byte) (color.g * color2.g), 
                (byte) (color.b * color2.b), 
                (byte) (color.a * color2.a));
        }

        public static Color Transparent(this Color color, float alpha) {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color32 Transparent(this Color32 color, byte alpha) {
            return new Color32(color.r, color.g, color.b, alpha);
        }

        public static Color TransparentMultiply(this Color color, float alpha) {
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }

        public static float GetBrightness(this Color color) {
            return YMath.Max(color.r, color.g, color.b);
        }

        public static float GetBrightnessMid(this Color color) {
            return (color.r + color.g + color.b) / 3;
        }

        public static Color Brightness(this Color color, float brightness) {
            brightness = (brightness).Clamp01();
            float value = (color.r + color.g + color.b) / 3;
            if (value == 0)
                return new Color(brightness, brightness, brightness, color.a);
            else {
                brightness = brightness / value;
                return new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a);
            }
        }

        public static float GetSaturate(this Color color) {
            return YMath.Max(color.r, color.g, color.b) - 
                   YMath.Min(color.r, color.g, color.b);
        }

        public static Color MultiplySaturation(this Color color, float saturation) {
            
            float min = YMath.Min(color.r, color.g, color.b);
            float max = YMath.Max(color.r, color.g, color.b);
            
            color.r = max - (max - color.r) * saturation;
            color.g = max - (max - color.g) * saturation;
            color.b = max - (max - color.b) * saturation;
            
            return color; 
        }
        
        public static Color Overlay(this Color a, Color b) {
            return new Color(
                Overlay(a.r, b.r),
                Overlay(a.g, b.g),
                Overlay(a.b, b.b),
                Overlay(a.a, b.a));
        }

        public static Color OverlayNatural(this Color a, Color b) {
            float saturateFade = 1f - (GetBrightness(b) - .5f).Abs() * 2;
            
            return a
                .Overlay(b)
                .MultiplySaturation(saturateFade);
        }

        static float Overlay(float a, float b) {
            return b < .5f ?
                a * b * 2 : 
                1f - 2f * (1f - a) * (1f - b);
        }

        public static HSBColor ToHSB(this Color color) {
            return HSBColor.FromColor(color);
        }

        public static string ToHex(this Color color) {
            return Mathf.RoundToInt(color.r * 255).ToString("X2") +
                Mathf.RoundToInt(color.g * 255).ToString("X2") +
                Mathf.RoundToInt(color.b * 255).ToString("X2") +
                Mathf.RoundToInt(color.a * 255).ToString("X2");
        }

        public static Color FromHex(string color) {
            try {
                Color result = new Color();
                var length = color.Length;
                if (length == 2) {
                    result.r = 1f * System.Convert.ToInt32(color.Substring(0, 2), 16) / 255;
                    result.g = result.r;
                    result.b = result.r;
                }
                if (length == 6 || length == 8) {
                    result.r = 1f * System.Convert.ToInt32(color.Substring(0, 2), 16) / 255;
                    result.g = 1f * System.Convert.ToInt32(color.Substring(2, 2), 16) / 255;
                    result.b = 1f * System.Convert.ToInt32(color.Substring(4, 2), 16) / 255;
                }
                if (length == 8) 
                    result.a = 1f * System.Convert.ToInt32(color.Substring(6, 2), 16) / 255;
                else
                    result.a = 1;
                
                return result;
            } catch {
                return Color.white;
            }
        }
    }

    [Serializable]
    public struct HSBColor {
        float _h;
        /// <summary>
        /// hue [0..1]
        /// </summary>
        public float Hue {
            get => _h;
            set => _h = Mathf.Repeat(value, 1);
        }
        
        float _s;
        /// <summary>
        /// saturation [0..1]
        /// </summary>
        public float Saturation {
            get => _s;
            set => _s = Mathf.Clamp01(value);
        }
        
        float _b;
        /// <summary>
        /// brightness [0..1]
        /// </summary>
        public float Brightness {
            get => _b;
            set => _b = Mathf.Clamp01(value);
        }
        
        float _a;
        /// <summary>
        /// alpha [0..1]
        /// </summary>
        public float Alpha {
            get => _a;
            set => _a = Mathf.Clamp01(value);
        }

        public HSBColor(float h, float s, float b, float a) {
            this._h = h;
            this._s = s;
            this._b = b;
            this._a = a;
        }

        public HSBColor(float h, float s, float b) : this(h, s, b, 1) { }

        public HSBColor(Color col) {
            HSBColor temp = FromColor(col);
            _h = temp._h;
            _s = temp._s;
            _b = temp._b;
            _a = temp._a;
        }

        public static HSBColor FromColor(Color color) {
            HSBColor ret = new HSBColor(0f, 0f, 0f, color.a);

            float r = color.r;
            float g = color.g;
            float b = color.b;

            float max = Mathf.Max(r, Mathf.Max(g, b));

            if (max <= 0) {
                return ret;
            }

            float min = Mathf.Min(r, Mathf.Min(g, b));
            float dif = max - min;

            if (max > min) {
                if (g == max) {
                    ret._h = (b - r) / dif * 60f + 120f;
                } else if (b == max) {
                    ret._h = (r - g) / dif * 60f + 240f;
                } else if (b > g) {
                    ret._h = (g - b) / dif * 60f + 360f;
                } else {
                    ret._h = (g - b) / dif * 60f;
                }
                if (ret._h < 0) {
                    ret._h = ret._h + 360f;
                }
            } else {
                ret._h = 0;
            }

            ret._h *= 1f / 360f;
            ret._s = (dif / max) * 1f;
            ret._b = max;

            return ret;
        }

        public static Color ToColor(HSBColor hsbColor, bool smooth = false) {
            float r = hsbColor._b;
            float g = hsbColor._b;
            float b = hsbColor._b;
            if (hsbColor._s != 0) {
                float max = hsbColor._b;
                float dif = hsbColor._b * hsbColor._s;
                float min = hsbColor._b - dif;

                float h = hsbColor._h * 360f;

                if (h < 60f) {
                    r = max;
                    g = h * dif / 60f + min;
                    if (smooth) g = Smooth(g);
                    b = min;
                } else if (h < 120f) {
                    r = -(h - 120f) * dif / 60f + min;
                    if (smooth) r = Smooth(r);
                    g = max;
                    b = min;
                } else if (h < 180f) {
                    r = min;
                    g = max;
                    b = (h - 120f) * dif / 60f + min;
                    if (smooth) b = Smooth(b);
                } else if (h < 240f) {
                    r = min;
                    g = -(h - 240f) * dif / 60f + min;
                    if (smooth) g = Smooth(g);
                    b = max;
                } else if (h < 300f) {
                    r = (h - 240f) * dif / 60f + min;
                    if (smooth) r = Smooth(r);
                    g = min;
                    b = max;
                } else if (h <= 360f) {
                    r = max;
                    g = min;
                    b = -(h - 360f) * dif / 60 + min;
                    if (smooth) b = Smooth(b);
                } else {
                    r = 0;
                    g = 0;
                    b = 0;
                }
            }

            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), hsbColor._a);
        }
        
        static float Smooth(float value) {
            return EasingFunctions.InOutQuad(value);
        }

        public Color ToColor(bool smooth = false) {
            return ToColor(this, smooth);
        }

        public override string ToString() {
            return "H:" + _h + " S:" + _s + " B:" + _b;
        }

        public static HSBColor Lerp(HSBColor a, HSBColor b, float t) {
            float h, s;

            if (a._b == 0) {
                h = b._h;
                s = b._s;
            } else if (b._b == 0) {
                h = a._h;
                s = a._s;
            } else {
                if (a._s == 0) {
                    h = b._h;
                } else if (b._s == 0) {
                    h = a._h;
                } else {
                    // works around bug with LerpAngle
                    float angle = Mathf.LerpAngle(a._h * 360f, b._h * 360f, t);
                    while (angle < 0f)
                        angle += 360f;
                    while (angle > 360f)
                        angle -= 360f;
                    h = angle / 360f;
                }
                s = Mathf.Lerp(a._s, b._s, t);
            }
            return new HSBColor(h, s, Mathf.Lerp(a._b, b._b, t), Mathf.Lerp(a._a, b._a, t));
        }
    }


    [Serializable]
    public struct ColorRange {
        public HSBColor startHSB {
            get => start.ToHSB();
            set => start = value.ToColor();
        }
        public HSBColor endHSB {
            get => end.ToHSB();
            set => end = value.ToColor();
        }

        public Color start;
        public Color end;

        public ColorRange(Color color) : this(color, color) { }

        public ColorRange(Color start, Color end) {
            this.start = start;
            this.end = end;
        }

        public ColorRange(HSBColor start, HSBColor end) {
            this.start = start.ToColor();
            this.end = end.ToColor();
        }

        public Gradient ToGradient() {
            Gradient gradient = new Gradient();
            gradient.SetKeys(new GradientColorKey[] {
                new GradientColorKey(startHSB.ToColor(), 0),
                new GradientColorKey(endHSB.ToColor(), 1)
            }, new GradientAlphaKey[] {
                new GradientAlphaKey(startHSB.Alpha, 0),
                new GradientAlphaKey(endHSB.Alpha, 1)
            });
            return gradient;
        }
    }
}
