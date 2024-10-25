using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm.Extensions {
    public static class RichTextString {
        static HSBColor hsbColor = new HSBColor(0, 0.5f, 1f);
        
        public static string Alpha(this string text, float alpha) {
            return $"<alpha=#{Mathf.RoundToInt(alpha.Clamp01() * 255):X2}>" + text + "</color>";
        }
        
        public static string Colorize(this string text, Color color) {
            return $"<color=#{color.ToHex()}>" + text + "</color>";
        }
        
        public static string ColorizeByHash(this string text) {
            if (text.IsNullOrEmpty())
                return text;
            
            hsbColor.Hue = YRandom.staticMain.Value(text.GetHashCode());
            return text.Colorize(hsbColor.ToColor());
        }
        
        public static string Align(this string text, HorizontalAlignmentOptions alignment) {
            return $"<align={alignment.ToString().ToLowerInvariant()}>" + text + "</align>";
        }
        
        public static string Monospace(this string text, float spacing = 0.7f) {
            return $"<mspace={spacing.ToString(CultureInfo.InvariantCulture)}em>{text}</mspace>";
        }
        
        public static string AsInlineSprite(this string spriteName) {
            return $"<sprite name=\"{spriteName}\">";
        }
        
        public static string AsInlineSpriteTint(this string spriteName) {
            return $"<sprite name=\"{spriteName}\" tint>";
        }
        
        public static string AsInlineSpriteColor(this string spriteName, string hexColor) {
            return $"<sprite name=\"{spriteName}\" color=#{hexColor}>";
        }
        
        public static string AsInlineSpriteColor(this string spriteName, Color color) {
            return spriteName.AsInlineSpriteColor(color.ToHex());
        }
        
        public static string Italic(this string text) {
            return $"<i>{text}</i>";
        }
        
        public static string Bold(this string text) {
            return $"<b>{text}</b>";
        }
        
        public static string Style(this string text, string style) {
            if (text.IsNullOrEmpty() || style.IsNullOrEmpty())
                return text;
            return $"<style=\"{style}\">{text}</style>";
        }
        
        public static string Strikethrough(this string text) {
            return $"<s>{text}</s>";
        }
        
        public static string Underline(this string text) {
            return $"<u>{text}</u>";
        }
        
        public static string Scale(this string text, float scale) {
            return $"<size={(scale * 100).RoundToInt()}%>{text}</size>";
        }
        
        public static string HyperLinkURL(this string text, string url) {
            return $"<a href=\"{url}\">{text}</a>";
        }
        
        public static string HyperLink(this string text, IDictionary<string, string> data) {
            return $"<a {data.Select(p => $"{p.Key}=\"{p.Value}\"").Join(" ")}>{text}</a>";
        }

        //TODO
        #region Into The Core
        
        public static string Add(this string text, string addition, string separator = ", ") {
            if (text.IsNullOrEmpty())
                return addition;
            return text + separator + addition;
        }

        public static string Brackets(this string text, Bracket bracket = Bracket.Round) {
            switch (bracket) {
                case Bracket.Round: return "(" + text + ")";
                case Bracket.Box: return "[" + text + "]";
                case Bracket.Curly: return "{" + text + "}";
                case Bracket.Angle: return "<" + text + ">";
            }
            
            return text;
        }
        
        public static string PlusIfPossitive(this int value) {
            if (value > 0)
                return "+" + value;
            return value.ToString();
        }
        
        public static string PlusIfPossitive(this float value, string format = null) {
            if (format == null)
                format = string.Empty;
            if (value > 0)
                return "+" + value.ToString(format, CultureInfo.InvariantCulture);
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
        
        public static string Ellipsis(this string text, int maxLength) {
            if (text == null)
                return null;
            
            if (maxLength > 0 && text.Length > maxLength)
                return text.Substring(0, maxLength) + "...";
            
            return text;
        }

        #endregion
    }
    
    public enum Bracket {
        Round,
        Box,
        Curly,
        Angle
    }
}