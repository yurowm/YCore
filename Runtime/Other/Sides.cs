using System;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Utilities {
    [Flags]
    public enum Side {
        Null = 0,
        Right = 1 << 0,
        Left = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3,
        TopRight = 1 << 4,
        TopLeft = 1 << 5,
        BottomRight = 1 << 6,
        BottomLeft = 1 << 7
    }
    
    public static class Sides {
        public static readonly Side[] all;
        public static readonly Side[] straight;
        public static readonly Side[] slanted;
        
        static Sides() {
            all = new[] {
                Side.Right, Side.TopRight,
                Side.Top, Side.TopLeft,
                Side.Left, Side.BottomLeft,
                Side.Bottom, Side.BottomRight};
            straight = new[] {
                Side.Right, Side.Top,
                Side.Left, Side.Bottom };
            slanted = new[] {
                Side.TopRight, Side.TopLeft,
                Side.BottomLeft, Side.BottomRight};
        }
        
        public static Side Rotate(this Side side, int steps) {
            return GetSideFromDirectionIndex(GetDirectionIndex(side) + steps);
        }
        
        public static Side BitRotate(this Side sides, int steps) {
            if (steps % 8 == 0)
                return sides;
            
            Side result = 0;
            
            all.Where(s => sides.HasFlag(s))
                .Select(s => s.Rotate(steps))
                .ForEach(s => result = result | s);
            
            return result;
        }

        public static Side Mirror(this Side side) {               
            switch (side) {
                case Side.Right: return Side.Left;
                case Side.TopRight: return Side.BottomLeft;
                case Side.Top: return Side.Bottom;
                case Side.TopLeft: return Side.BottomRight;
                case Side.Left: return Side.Right;
                case Side.BottomLeft: return Side.TopRight;
                case Side.Bottom: return Side.Top;
                case Side.BottomRight: return Side.TopLeft;
            }
            return Side.Null;
        }

        public static bool IsNotNull(this Side side) {
            return side != Side.Null;
        }

        public static bool IsStraight(this Side side) {             
            switch (side) {
                case Side.Right: return true;
                case Side.Top: return true;
                case Side.Left: return true;
                case Side.Bottom: return true;
                case Side.TopRight: return false;
                case Side.TopLeft: return false;
                case Side.BottomLeft: return false;
                case Side.BottomRight: return false;
            }
            return false;
        }

        public static bool IsSlanted(this Side side) {             
            switch (side) {
                case Side.Right: return false;
                case Side.Top: return false;
                case Side.Left: return false;
                case Side.Bottom: return false;
                case Side.TopRight: return true;
                case Side.TopLeft: return true;
                case Side.BottomLeft: return true;
                case Side.BottomRight: return true;
            }
            return false;
        }

        public static bool IsHorizontal(this Side side) {             
            switch (side) {
                case Side.Right: return true;
                case Side.Left: return true;
            }
            return false;
        }

        public static bool IsVertical(this Side side) {             
            switch (side) {
                case Side.Top: return true;
                case Side.Bottom: return true;
            }
            return false;
        }
        
        public static int X(this Side s) {
            switch (s) {
                case Side.Top:
                case Side.Bottom:
                    return 0;
                case Side.TopLeft:
                case Side.BottomLeft:
                case Side.Left:
                    return -1;
                case Side.BottomRight:
                case Side.TopRight:
                case Side.Right:
                    return 1;
            }
            return 0;
        }

        public static int Y(this Side s) {
            switch (s) {
                case Side.Left:
                case Side.Right:
                    return 0;
                case Side.Bottom:
                case Side.BottomRight:
                case Side.BottomLeft:
                    return -1;
                case Side.TopLeft:
                case Side.TopRight:
                case Side.Top:
                    return 1;
            }
            return 0;
        }

        static readonly float halfOfSqrtOf2 = 2f.Sqrt() / 2;

        public static float Sin(this Side s) {
            switch (s) {
                case Side.Left:
                case Side.Right:
                    return 0;
                case Side.Bottom:
                    return -1;
                case Side.BottomRight:
                case Side.BottomLeft:
                    return -halfOfSqrtOf2;
                case Side.TopLeft:
                case Side.TopRight:
                    return halfOfSqrtOf2;
                case Side.Top:
                    return 1;
            }
            return 0;
        }
        
        public static float Cos(this Side s) {
            switch (s) {
                case Side.Top:
                case Side.Bottom:
                    return 0;
                case Side.TopLeft:
                case Side.BottomLeft:
                    return -halfOfSqrtOf2;
                case Side.Left:
                    return -1;
                case Side.BottomRight:
                case Side.TopRight:
                    return halfOfSqrtOf2;
                case Side.Right:
                    return 1;
            }
            return 0;
        }

        public static int2 ToInt2(this Side s) {
            switch (s) {
                case Side.Right: return new int2(1, 0);
                case Side.TopRight: return new int2(1, 1);
                case Side.Top: return new int2(0, 1);
                case Side.TopLeft: return new int2(-1, 1);
                case Side.Left: return new int2(-1, 0);
                case Side.BottomLeft: return new int2(-1, -1);
                case Side.Bottom: return new int2(0, -1);
                case Side.BottomRight: return new int2(1, -1);
            }
            return int2.Null;
        }
        
        public static Vector2 ToVector2(this Side side) {
            switch (side) {
                case Side.Right:return Vector2.right;
                case Side.TopRight: return new Vector2(1, 1);
                case Side.Top: return Vector2.up;
                case Side.TopLeft: return new Vector2(-1, 1);
                case Side.Left: return Vector2.left;
                case Side.BottomLeft: return new Vector2(-1, -1);
                case Side.Bottom: return Vector2.down;
                case Side.BottomRight: return new Vector2(1, -1);
            }
            return Vector2.zero;
        }

        
        
        public static Side Horizontal(this Side side) {
            switch (side) {
                case Side.Left:
                case Side.TopLeft:
                case Side.BottomLeft:
                    return Side.Left;
                    
                case Side.Right:
                case Side.TopRight:
                case Side.BottomRight:
                    return Side.Right;
            }
            return Side.Null;
        }

        public static Side Vertical(this Side s) {
            switch (s) {
                case Side.Top:
                case Side.TopLeft:
                case Side.TopRight:
                    return Side.Top;
                    
                case Side.Bottom:
                case Side.BottomLeft:
                case Side.BottomRight:
                    return Side.Bottom;
            }

            return Side.Null;
        }

        public static int ToAngle(this Side side) {
            switch (side) {
                case Side.Right: return 0;
                case Side.TopRight: return 45;
                case Side.Top: return 90;
                case Side.TopLeft: return 135;
                case Side.Left: return 180;
                case Side.BottomLeft: return 225;
                case Side.Bottom: return 270;
                case Side.BottomRight: return 315;
            }
            return 0;
        }
        
        public static int GetDirectionIndex(this Side side) {
            switch (side) {
                case Side.Right: return 0;
                case Side.TopRight: return 1;
                case Side.Top: return 2;
                case Side.TopLeft: return 3;
                case Side.Left: return 4;
                case Side.BottomLeft: return 5;
                case Side.Bottom: return 6;
                case Side.BottomRight: return 7;
            }
            return 0;
        }
        
        public static Side GetSideFromDirectionIndex(int index) {
            switch (index.Repeat(8)) {
                case 0: return Side.Right;
                case 1: return Side.TopRight;
                case 2: return Side.Top;
                case 3: return Side.TopLeft;
                case 4: return Side.Left;
                case 5: return Side.BottomLeft;
                case 6: return Side.Bottom;
                case 7: return Side.BottomRight;
            }
            return Side.Null;
        }
    } 
}
