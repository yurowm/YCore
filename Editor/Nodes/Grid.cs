using System;
using UnityEditor;
using UnityEngine;

namespace Yurowm.Nodes.Editor {
    public class Grid {
        public Color backgroundColor = new Color(0.2f, 0.3f, 0.4f);
        public Color gridColor = new Color(1, 1, 1, 0.2f);
        public Color gridSubColor = new Color(1, 1, 1, 0.1f);
        public int gridStep = 100;
        public int gridSub = 5;

        public Action<Rect, int> label = null;

        public void Draw(Rect rect) {
            if (Event.current.type == EventType.Repaint) {
                Handles.DrawSolidRectangleWithOutline(rect, backgroundColor, Color.clear);
                Color color = Handles.color;
                float s = gridSub > 0 ? 1f * gridStep / gridSub : 0;
                for (float x = Mathf.Floor(rect.x / gridStep) * gridStep + .5f; x < rect.xMax; x += gridStep) {
                    if (ClipX(ref rect, x)) {
                        Handles.color = gridColor;
                        Handles.DrawLine(new Vector3(x, rect.yMin), new Vector3(x, rect.yMax));
                        label?.Invoke(new Rect(x, rect.yMin, 100, 20), Mathf.RoundToInt(x / gridStep));
                    }
                    if (gridSub > 0) {
                        Handles.color = gridSubColor;
                        for (int i = 0; i < gridSub; i++)
                            if (ClipX(ref rect, x + s * i))
                                Handles.DrawLine(new Vector3(x + s * i, rect.yMin), new Vector3(x + s * i, rect.yMax));
                    }
                }
                for (float y = Mathf.Floor(rect.y / gridStep) * gridStep + .5f; y < rect.yMax; y += gridStep) {
                    if (ClipY(ref rect, y)) {
                        Handles.color = gridColor;
                        Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
                    }
                    if (gridSub > 0) {
                        Handles.color = gridSubColor;
                        for (int i = 0; i < gridSub; i++)
                            if (ClipY(ref rect, y + s * i))
                                Handles.DrawLine(new Vector3(rect.xMin, y + s * i), new Vector3(rect.xMax, y + s * i));
                    }
                    label?.Invoke(new Rect(rect.xMin, y, 100, 20), Mathf.RoundToInt(-y / gridStep));
                }
                Handles.color = color;
            }
        }

        bool ClipX(ref Rect rect, float x) {
            return rect.xMin < x && rect.xMax > x;
        }

        bool ClipY(ref Rect rect, float y) {
            return rect.yMin < y && rect.yMax > y;
        }

        public Rect Snap(Rect rect) {
            rect.position = Snap(rect.position);
            return rect;
        }

        public Vector2 Snap(Vector2 position) {
            float snap = 1f * gridStep;
            if (gridSub > 0) snap /= gridSub;
            position.x = Mathf.Round(position.x / snap) * snap;
            position.y = Mathf.Round(position.y / snap) * snap;
            return position;
        }
    }
}