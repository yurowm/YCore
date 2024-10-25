using System;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Extensions;
using Yurowm.Shapes;

namespace Yurowm.Colors {
    public abstract class Repaint : BaseBehaviour { }
    
    public abstract class RepaintColor : Repaint, IRepaintSetColor {
        [Flags]
        public enum Type {
            RGB = Red | Green | Blue,
            RGBA = RGB | Alpha,
            Alpha = 1 << 0,
            Red = 1 << 1,
            Green = 1 << 2,
            Blue = 1 << 3,
            Hue = 1 << 4,
            Saturate = 1 << 5,
            Brightness = 1 << 6,
            Overlay = 1 << 7,
            OverlayInverse = 1 << 8,
            OverlayNatural = 1 << 9
        }
        
        public Type type = Type.RGB;
        protected bool rememberOriginalColor => type.HasFlag(Type.Overlay) 
                                                || type.HasFlag(Type.OverlayInverse)
                                                || type.HasFlag(Type.OverlayNatural);

        const Type HSB = Type.Hue | Type.Saturate | Type.Brightness;
        const Type HSBA = HSB | Type.Alpha;
        const Type Blending = Type.Overlay | Type.OverlayInverse | Type.OverlayNatural;
        
        public Color TransformColor(Color original, Color color) {
            if (type.OverlapFlag(Blending)) {
                if (type.HasFlag(Type.Overlay)) return color.Overlay(original);
                if (type.HasFlag(Type.OverlayInverse)) return original.Overlay(color);
                if (type.HasFlag(Type.OverlayNatural)) return color.OverlayNatural(original);
            }
            
            if (type.HasFlag(Type.RGBA) || type.HasFlag(HSBA))
                return color;
            
            if (type.HasFlag(Type.RGB) || type.HasFlag(HSB)) {
                color.a = original.a;
                return color;
            }
            
            if (type.OverlapFlag(Type.RGB)) {
                if (type.HasFlag(Type.Red)) original.r = color.r;
                if (type.HasFlag(Type.Green)) original.g = color.g;
                if (type.HasFlag(Type.Blue)) original.b = color.b;
                if (type.HasFlag(Type.Alpha)) original.a = color.a;
                return original;
            }
            
            if (type.OverlapFlag(HSB)) {
                HSBColor originalHSB = new HSBColor(original);
                HSBColor colorHSB = new HSBColor(color);
                
                if (type.HasFlag(Type.Hue)) originalHSB.Hue = colorHSB.Hue;
                if (type.HasFlag(Type.Saturate)) originalHSB.Saturation = colorHSB.Saturation;
                if (type.HasFlag(Type.Brightness)) originalHSB.Brightness = colorHSB.Brightness;
                if (type.HasFlag(Type.Alpha)) originalHSB.Alpha = colorHSB.Alpha;
                
                return originalHSB.ToColor();
            }
            
            return original;
        }
        
        public Color TransformColor(Color color) {
            return TransformColor(GetOriginalColor(), color);
        }

        protected bool remembered = false;
        Color rememberedColor;
        
        public Color GetOriginalColor() {
            if (!rememberOriginalColor) return GetColor();

            if (remembered) return rememberedColor;
            
            rememberedColor = GetColor();    
            remembered = true;

            return rememberedColor;
        }

        public abstract void SetColor(Color color);
        public abstract Color GetColor();
    }
 
    public interface IRepaintSetColor {
        void SetColor(Color color);
    }
 
    public interface IRepaintSetShape {
        void SetMesh(MeshData meshData);
    }
    
    public interface IRepaintSetMesh {
        void SetMesh(Mesh mesh);
    }
    
    public interface IRepaintByContext {
        void Repaint(LiveContext fieldContext);
    }
    
    public static class RepaintExtensions {
        public static void Repaint(this GameObject gameObject, Color color) {
            foreach (var component in gameObject.GetComponentsInChildren<Repaint>(true))
                if (component is IRepaintSetColor setColor)
                    setColor.SetColor(color);
        }
        
        public static void Repaint(this GameObject gameObject, Mesh mesh) {
            foreach (var component in gameObject.GetComponentsInChildren<Repaint>(true))
                if (component is IRepaintSetMesh setColor)
                    setColor.SetMesh(mesh);
        }
        
        public static void Repaint(this GameObject gameObject, MeshData meshData) {
            foreach (var component in gameObject.GetComponentsInChildren<Repaint>(true))
                if (component is IRepaintSetShape setColor)
                    setColor.SetMesh(meshData);
        }
    }
    
}