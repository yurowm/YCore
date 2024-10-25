using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIStyles;
using Yurowm.Utilities;
using UColor = UnityEngine.Color;

namespace Yurowm.GUIHelpers {
    public static class GUIHelper {
        public class Vertical : IDisposable {
            static Vertical Instance = new Vertical();
            static Rect rect;
            
            public static Vertical Start(params GUILayoutOption[] options) {
                GUILayout.BeginVertical(options);
                return Instance;
            }
            
            public static Vertical Start(UColor color) {
                var rect = EditorGUILayout.BeginVertical();
                if (Event.current.type == EventType.Repaint)
                    Handles.DrawSolidRectangleWithOutline(rect, color, UColor.clear);

                return Instance;
            }

            public static Vertical Start(GUIStyle style, params GUILayoutOption[] options) {
                GUILayout.BeginVertical(style, options);
                return Instance;
            }

            public static Vertical Start(Action<Rect> drawBackground, params GUILayoutOption[] options) {
                if (Event.current.type == EventType.Repaint)
                    drawBackground?.Invoke(rect);
                return Start(options);
            }

            Vertical() { }

            public void Dispose() {
                GUILayout.EndVertical();
                rect = GUILayoutUtility.GetLastRect();
            }
        }

        public class Horizontal : IDisposable {
            static Horizontal Instance = new Horizontal();
            static Rect rect;

            public static Horizontal Start(params GUILayoutOption[] options) {
                GUILayout.BeginHorizontal(options);
                return Instance;
            }
            
            public static Horizontal Start(UColor color) {
                var rect = EditorGUILayout.BeginHorizontal();
                if (Event.current.type == EventType.Repaint)
                    Handles.DrawSolidRectangleWithOutline(rect, color, UColor.clear);

                return Instance;
            }

            public static Horizontal Start(GUIStyle style, params GUILayoutOption[] options) {
                GUILayout.BeginHorizontal(style, options);
                return Instance;
            }
            
            public static Horizontal Start(Action<Rect> drawBackground, params GUILayoutOption[] options) {
                if (Event.current.type == EventType.Repaint)
                    drawBackground?.Invoke(rect);
                return Start(options);
            }

            Horizontal() { }

            public void Dispose() {
                GUILayout.EndHorizontal();
            }
        }

        public class VerticalSplit : IDisposable {
            float size;
            
            public VerticalSplit(float size = 200) {
                this.size = size;
            }
            
            public VerticalSplit Start() {
                DrawCursor();
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Height(size));
                return this;
            }

            public void Dispose() {
                GUILayout.EndVertical();
            }
            
            float lastPosition = -1;
            int current = 0;
            bool drag = false;
            
            void DrawCursor() {
                var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(5));
                    
                DrawLine(
                    new Vector2(rect.xMin, rect.center.y.Round()),
                    new Vector2(rect.xMax, rect.center.y.Round()),
                    SeparatorColor, 2);
                
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

                if (rect.Contains(Event.current.mousePosition))
                    if (Event.current.type == EventType.MouseDown) {
                        lastPosition = Event.current.mousePosition.y; 
                        drag = true;
                    }
                
                if (Event.current.type == EventType.MouseUp) {
                    lastPosition = default; 
                    drag = false;
                }

                if (drag && Event.current.type == EventType.MouseDrag) {
                    var delta = Event.current.mousePosition.y - lastPosition;
                    
                    size -= delta;

                    lastPosition = Event.current.mousePosition.y; 
                    
                    if (EditorWindow.mouseOverWindow)
                        EditorWindow.mouseOverWindow.Repaint();
                }
            }
        }
        
        public class Toolbar : IDisposable {
            static Toolbar Instance = new Toolbar();

            public static Toolbar Start() {
                GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));
                return Instance;
            }

            Toolbar() { }

            public void Dispose() {
                GUILayout.EndHorizontal();
            }
        }

        public class FocusSpoiler : IDisposable {
            static object focus;
            object key;
            
            public FocusSpoiler() { }

            public static FocusSpoiler Start(string title, object key) {
                if (key == null)
                    return null;
                
                var instance = new FocusSpoiler();
                
                instance.key = key;
                
                var isVisible = instance.IsVisible();
                if (isVisible != EditorGUILayout.Foldout(isVisible, title)) {
                    isVisible = !isVisible;
                    if (isVisible)
                        focus = key;
                    else
                        focus = null;
                }

                EditorGUI.indentLevel ++;
                return instance;
            }

            public bool IsVisible() {
                return key == focus;
            }

            public void Dispose() {
                EditorGUI.indentLevel --;
            }
        }

        public class Spoiler : IDisposable {
            AnimBool animBool;
            EditorGUILayout.FadeGroupScope group;
            
            public Spoiler(bool shown = false) {
                animBool = new AnimBool(shown);
                animBool.valueChanged.AddListener(() => EditorWindow.focusedWindow.Repaint());
            }

            public Spoiler Start(string title) {
                animBool.target = EditorGUILayout.Foldout(animBool.target, title);
                group = new EditorGUILayout.FadeGroupScope(animBool.faded);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.BeginVertical();
                
                return this;
            }

            public bool IsVisible() {
                return group.visible;
            }

            public void Dispose() {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                
                group.Dispose();
            }
        }
        
        public class Scroll : IDisposable {
            Vector2 position = new Vector2();
            GUILayoutOption[] options = null;
            GUIStyle style = null;

            public Scroll(GUIStyle style, params GUILayoutOption[] options) : this(options) {
                this.style = style;
            }

            public Scroll(params GUILayoutOption[] options) {
                this.options = options;
            }

            public IDisposable Start() {
                if (style == null)
                    position = GUILayout.BeginScrollView(position, options);
                else
                    position = GUILayout.BeginScrollView(position, style, options);
                
                return this;
            }
            
            public void Dispose() {
                GUILayout.EndScrollView();
            }

            public void ScrollTo(float y = 0) {
                position.y = y;
            }
        }

        public class Lock : IDisposable {
            static readonly Lock Instance = new Lock();
            
            readonly List<bool> memory = new List<bool>();
            
            Lock() {}

            public static Lock Start(bool value) {
                Instance.memory.Add(GUI.enabled);
                if (GUI.enabled)
                    GUI.enabled = !value;
                return Instance;
            }

            public void Dispose() {
                GUI.enabled = memory.All(v => v);
                memory.RemoveAt(memory.Count - 1);
            }
        }
        
        public class Clip : IDisposable {
            static readonly Clip Instance = new Clip();

            Clip() {}
            
            public static Clip Start(Rect rect) {
                GUI.BeginClip(rect);
                return Instance;
            }
                
            public static Clip Start(Rect rect, out Rect clipedRect) {
                clipedRect = rect;
                clipedRect.position = Vector2.zero;
                return Start(rect);
            }

            public void Dispose() {
                GUI.EndClip();
            }
        }
        
        public class Password {

            bool shown = false;

            public string Edit(string label, string value) {
                if (label.IsNullOrEmpty())
                    label = "Password";
                
                using (Horizontal.Start()) {
                    if (shown)
                        value = EditorGUILayout.TextField(label, value);
                    else 
                        value = EditorGUILayout.PasswordField(label, value);
             
                    shown = GUILayout.Toggle(shown, "Show", EditorStyles.miniButton, GUILayout.Width(50));
                }
                
                return value;
            }
        }
        
        
        public abstract class GUIColor : IDisposable {
            
            static readonly UColor liteColor = new UColor(.33f, .33f, .33f, 1f);
            static readonly UColor proColor = new UColor(1f, 1f, 1f, 1f);
            
            public static UColor GetProLiteColor() {
                return EditorGUIUtility.isProSkin ? proColor : liteColor;
            }
            
            public abstract void Dispose();
        }
        
        public class Color : GUIColor {
            static readonly Color Instance = new Color();

            readonly List<UColor> memory = new List<UColor>();
            
            Color() {}

            public static Color Start(UColor color) {
                Instance.memory.Add(GUI.color);
                GUI.color = color;
                return Instance;
            }
                
            public static Color ProLiteStart() {
                return Start(GetProLiteColor());
            }

            public override void Dispose() {
                GUI.color = memory[memory.Count - 1];
                memory.RemoveAt(memory.Count - 1);
            }
        }

        public class ContentColor : GUIColor {
            static readonly ContentColor Instance = new ContentColor();

            readonly List<UColor> memory = new List<UColor>();
            
            ContentColor() {}

            public static ContentColor Start(UColor color) {
                Instance.memory.Add(GUI.color);
                GUI.contentColor = color;
                return Instance;
            }
            
            public static ContentColor ProLiteStart() {
                return Start(GetProLiteColor());
            }

            public override void Dispose() {
                GUI.contentColor = memory[memory.Count - 1];
                memory.RemoveAt(memory.Count - 1);
            }
        }

        public class BackgroundColor : GUIColor {
            static readonly BackgroundColor Instance = new BackgroundColor();

            readonly List<UColor> memory = new List<UColor>();
            
            BackgroundColor() {}

            public static BackgroundColor Start(UColor color) {
                Instance.memory.Add(GUI.color);
                GUI.backgroundColor = color;
                return Instance;
            }
            
            public static BackgroundColor ProLiteStart() {
                return Start(GetProLiteColor());
            }

            public override void Dispose() {
                GUI.backgroundColor = memory[memory.Count - 1];
                memory.RemoveAt(memory.Count - 1);
            }
        }

        public class HandlesColor : GUIColor {
            static readonly HandlesColor Instance = new HandlesColor();

            readonly List<UColor> memory = new List<UColor>();
            
            HandlesColor() {}

            public static HandlesColor Start(UColor color) {
                Instance.memory.Add(Handles.color);
                Handles.color = color;
                return Instance;
            }
            
            public static HandlesColor ProLiteStart() {
                return Start(GetProLiteColor());
            }

            public override void Dispose() {
                Handles.color = memory[memory.Count - 1];
                memory.RemoveAt(memory.Count - 1);
            }
        }

        public class Change : IDisposable {
            static Change instance = new Change();
            
            List<Action> actionsQueue = new List<Action>();
            
            Change() {}
            
            public static Change Start(Action onChange) {
                EditorGUI.BeginChangeCheck();
                instance.actionsQueue.Add(onChange);
                return instance;
            }

            public void Dispose() {
                if (EditorGUI.EndChangeCheck())
                    actionsQueue[actionsQueue.Count - 1].Invoke();
                actionsQueue.RemoveAt(actionsQueue.Count - 1);
            }
        }     
        
        public class IndentLevel : IDisposable {
            static IndentLevel instance = new IndentLevel();
            static List<int> lastValue = new ();
            IndentLevel() {}
            
            public static IndentLevel Start() {
                lastValue.Add(EditorGUI.indentLevel);
                EditorGUI.indentLevel++;
                return instance;
            }
            
            public static IndentLevel Zero() {
                lastValue.Add(EditorGUI.indentLevel);
                EditorGUI.indentLevel = 0;
                return instance;
            }

            public void Dispose() {
                EditorGUI.indentLevel = lastValue.GrabLast();
            }

        }
        
        public class SingleLine : IDisposable {
            static SingleLine instance = new();
            static List<IndentLevel> lastValue = new ();
            SingleLine() {}
            
            public static SingleLine Start(string label = null) {
                EditorGUILayout.BeginHorizontal();
                if (!label.IsNullOrEmpty())
                    EditorGUILayout.PrefixLabel(label);
                lastValue.Add(IndentLevel.Zero());
                return instance;
            }

            public void Dispose() {
                lastValue.GrabLast()?.Dispose();
                EditorGUILayout.EndHorizontal();
            }
        }
        
        public class Area : IDisposable {
            static Area instance = new Area();
            
            Area() {}
            
            public static Area Start(string title) {
                EditorGUILayout.LabelField(title, Styles.microTitle);
                EditorGUI.indentLevel++;
                return instance;
            }
            
            public static Area Start(string title, Action menuAction) {
                using (Horizontal.Start()) {
                    EditorGUILayout.PrefixLabel(title);
                    if (GUILayout.Button("...")) menuAction.Invoke();
                }
                EditorGUI.indentLevel++;
                return instance;
            }

            public void Dispose() {
                EditorGUI.indentLevel--;
            }
        }
        
        public class EditorLabelWidth : IDisposable {
            static readonly EditorLabelWidth Instance = new();

            readonly List<float> memory = new();
            
            EditorLabelWidth() {}

            public static EditorLabelWidth Start(float value) {
                Instance.memory.Add(EditorGUIUtility.labelWidth);
                EditorGUIUtility.labelWidth = value;
                return Instance;
            }

            public static EditorLabelWidth Default() => Start(0);
            public static EditorLabelWidth Zero() => Start(.001f);

            public void Dispose() {
                EditorGUIUtility.labelWidth = memory.GrabLast();
            }
        }
        
        #region Separator

        static readonly UColor SeparatorColor = new UColor(.1f, .1f, .1f);
        
        public static void DrawSeparatorRect(Rect rect) {
            DrawRectLine(rect.GrowSize(2), SeparatorColor, 4);
        }
        
        #endregion
        
        public class LayoutSplitter {
            int resize = -1;
            int current = 0;
            float[] sizes;

            Rect lastRect;

            public float thickness = 2;

            OrientationLine orientation;
            OrientationLine internalOrientation;

            bool areaStarted = false;
            bool firstArea = false;

            public LayoutSplitter(OrientationLine orientation, OrientationLine internalOrientation, params float[] sizes) {
                this.sizes = sizes;
                this.orientation = orientation;
                this.internalOrientation = internalOrientation;
            }

            public bool Area(GUIStyle style = null) {
                if (IsLast() && !firstArea)
                    return false;

                if (mask != null && !mask[current]) {
                    current++;
                    return false;
                }

                if (areaStarted) {
                    areaStarted = false;
                    EndLayout(internalOrientation);
                }
                if (!firstArea) {
                    lastRect = GUILayoutUtility.GetLastRect();
                    var rect = EditorGUILayout.GetControlRect(Expand(Anti(orientation)), Size(orientation, thickness));
                    
                    Handles.DrawSolidRectangleWithOutline(rect, 
                        SeparatorColor, 
                        UColor.clear);

                    EditorGUIUtility.AddCursorRect(rect, orientation == OrientationLine.Horizontal ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);

                    if (resize < 0 && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                        resize = current;

                    if (resize == current) {
                        float delta = 0;
                        switch (orientation) {
                            case OrientationLine.Horizontal: delta = Mathf.Max(lastRect.width - (rect.x - Event.current.mousePosition.x + thickness / 2), 50); break;
                            case OrientationLine.Vertical: delta = Mathf.Max(lastRect.height - (rect.y - Event.current.mousePosition.y + thickness / 2), 50); break;
                        }

                        delta -= sizes[current];

                        sizes[current] += delta;

                        if (EditorWindow.mouseOverWindow)
                            EditorWindow.mouseOverWindow.Repaint();

                        if (Event.current.type == EventType.MouseUp)
                            resize = -1;
                    }
                }

                if (firstArea)
                    firstArea = false;
                else
                    current++;

                if (IsLast())
                    BeginLayout(internalOrientation, style);
                else
                    BeginLayout(internalOrientation, style, Size(orientation, sizes[current]));

                areaStarted = true;

                if (mask != null)
                    return mask[current];

                return true;
            }

            OrientationLine Anti(OrientationLine o) {
                switch (o) {
                    case OrientationLine.Horizontal: return OrientationLine.Vertical;
                    case OrientationLine.Vertical: return OrientationLine.Horizontal;
                }
                return OrientationLine.Horizontal;
            }

            Rect BeginLayout(OrientationLine o, GUIStyle style = null, params GUILayoutOption[] options) {
                switch (o) {
                    case OrientationLine.Horizontal: return style == null ? EditorGUILayout.BeginHorizontal(options) : EditorGUILayout.BeginHorizontal(style, options);
                    case OrientationLine.Vertical: return style == null ? EditorGUILayout.BeginVertical(options) : EditorGUILayout.BeginVertical(style, options);
                }
                return new Rect();
            }

            void EndLayout(OrientationLine o, params GUILayoutOption[] options) {
                switch (o) {
                    case OrientationLine.Horizontal: GUILayout.EndHorizontal(); break;
                    case OrientationLine.Vertical: GUILayout.EndVertical(); break;
                }
            }

            GUILayoutOption Size(OrientationLine o, float size) {
                switch (o) {
                    case OrientationLine.Horizontal: return GUILayout.Width(size);
                    case OrientationLine.Vertical: return GUILayout.Height(size);
                }
                return null;
            }

            GUILayoutOption Expand(OrientationLine o) {
                switch (o) {
                    case OrientationLine.Horizontal: return GUILayout.ExpandWidth(true);
                    case OrientationLine.Vertical: return GUILayout.ExpandHeight(true);
                }
                return null;
            }

            bool[] mask = null;
            public Splitter Start(params bool[] mask) {
                last = sizes.Length;
                UpdateMask(mask);

                current = 0;
                areaStarted = false;
                firstArea = true;

                BeginLayout(orientation, null, Expand(orientation));

                if (current >= sizes.Length)
                    return new Splitter(() => EndLayout(orientation));

                return new Splitter(() => {
                    if (areaStarted)
                        EndLayout(internalOrientation);
                    EndLayout(orientation);
                });
            }

            public void UpdateMask(params bool[] mask) {
                if (mask != null && mask.Length == sizes.Length + 1) {
                    this.mask = mask;
                    while (last >= 0 && !mask[last])
                        last--;
                } else
                    this.mask = null;
            }

            int last = -1;
            bool IsLast() {
                return current >= last;
            }

            public class Splitter : IDisposable {
                Action onDispose;

                public Splitter(Action onDispose) {
                    this.onDispose = onDispose;
                }

                public void Dispose() {
                    onDispose();
                }
            }
        }

        public static void EditRange(string label, IntRange range, int minLimit, int maxLimit) {
            if (range == null) return;

            float min = range.Min;
            float max = range.Max;

            using (Horizontal.Start()) {
                EditorGUILayout.MinMaxSlider(label, ref min, ref max, minLimit, maxLimit);
                range.min = EditorGUILayout.IntField(Mathf.RoundToInt(min), GUILayout.Width(40));
                range.max = EditorGUILayout.IntField(Mathf.RoundToInt(max), GUILayout.Width(40));
            }
        }
        
        public static T Popup<T>(string label, T selected, IEnumerable<T> collection, GUIStyle style, 
            Func<T, string> getName, Action<T> onChange, Func<T, bool> filter = null,
            params GUILayoutOption[] options) where T : class {
            
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, style, options);
            if (!label.IsNullOrEmpty())
                rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            
            if (collection == null) {
                GUI.Label(rect, "<NO OPTIONS>", style);
                return selected;
            }

            if (filter != null && !filter.Invoke(selected)) {
                var fod = collection.FirstOrDefault(filter);
                if (fod != selected && filter.Invoke(fod)) {
                    selected = fod;
                    onChange?.Invoke(selected);
                }
            }
            
            if (GUI.Button(rect, GetName(selected, getName), style)) {
                var menu = new GenericMenu();

                if (filter == null || filter.Invoke(null)) {
                    menu.AddItem(new GUIContent(GetName(null, getName)), selected == null, () => {
                        selected = null;
                        onChange?.Invoke(selected);
                        GUI.changed = true;
                    });
                }
                    
                foreach (T item in collection) {
                    if (filter != null && !filter.Invoke(item)) continue;
                    T _item = item;
                    menu.AddItem(new GUIContent(GetName(item, getName)), item.Equals(selected), () => {
                        selected = _item;
                        onChange?.Invoke(selected);
                        GUI.changed = true;
                    });
                }
                
                menu.DropDown(rect);
            }
            
            return selected;
        }
        
        static string GetName<T>(T item, Func<T, string> getName) where T : class {
            if (item == null) return "<NULL>";
            return getName?.Invoke(item) ?? "<NO NAME>";
        }
        
        public static void DrawSprite(Sprite sprite) {
            DrawSprite(sprite, sprite.texture.width, sprite.texture.height);
        }

        public static void DrawSprite(Sprite sprite, float width, float height) {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(width), GUILayout.Height(height));
            if (sprite != null) {
                Rect uv = sprite.rect;
                uv.x /= sprite.texture.width;
                uv.width /= sprite.texture.width;
                uv.y /= sprite.texture.height;
                uv.height /= sprite.texture.height;
                GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
            }
        }


        static Texture2D _wireTexture = null;
        static Texture2D wireTexture {
            get {
                if (_wireTexture == null) {
                    
                    var zeroColor = new UColor(1, 1, 1, 0);
                    var oneColor = new UColor(1, 1, 1, 1);
                    
                    _wireTexture = new Texture2D(1, 5, TextureFormat.RGBA4444, false);

                    for (int x = 0; x < _wireTexture.width; x++)
                        for (int y = 0; y < _wireTexture.height; y++)
                            _wireTexture.SetPixel(x, y, y == 0 || y == _wireTexture.height - 1 ?
                                zeroColor : oneColor);

                    _wireTexture.Apply();
                }
                return _wireTexture;
            }

        }

        public static void DrawBezier(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, UnityEngine.Color startColor, UnityEngine.Color endColor, float width) {
            if (Event.current.type != EventType.Repaint) return;

            Vector3 last = startPosition;
            Vector3 current;
            Vector3 q0, q1, q2, r0, r1;
            float delta = 1f / 40;
            UColor color = Handles.color;
            for (float t = 0; last != endPosition; t += delta) {
                q0 = Vector3.Lerp(startPosition, startTangent, t);
                q1 = Vector3.Lerp(startTangent, endTangent, t);
                q2 = Vector3.Lerp(endTangent, endPosition, t);
                r0 = Vector3.Lerp(q0, q1, t);
                r1 = Vector3.Lerp(q1, q2, t);

                current = Vector3.Lerp(r0, r1, t);
                Handles.color = UnityEngine.Color.Lerp(startColor, endColor, t) * color;
                Handles.DrawAAPolyLine(wireTexture, width, last, current);
                last = current;
            }
            Handles.color = color;
        }
        
        public static void DrawLine(Vector2 startPosition, Vector2 endPosition, UnityEngine.Color color, float width) {
            if (Event.current.type != EventType.Repaint) return;
            
            using (HandlesColor.Start(color))
                Handles.DrawAAPolyLine(wireTexture, width, startPosition, endPosition);
        }
        
        public static void DrawLine(Vector3 startPosition, Vector3 endPosition, float width) {
            if (Event.current.type != EventType.Repaint) return;
            
            Handles.DrawAAPolyLine(wireTexture, width, startPosition, endPosition);
        }
        
        
        static readonly Vector3[] verts = new Vector3[4];
        
        public static void DrawRect(Rect rect, UColor color) {
            if (Event.current.type != EventType.Repaint) return;

            if (color.a <= 0) return;
            
            using (HandlesColor.Start(color)) {
                verts[0] = new Vector3(rect.xMin, rect.yMin);
                verts[1] = new Vector3(rect.xMax, rect.yMin);
                verts[2] = new Vector3(rect.xMax, rect.yMax);
                verts[3] = new Vector3(rect.xMin, rect.yMax);
                Handles.DrawAAConvexPolygon(verts);
            }
        }
        
        public static void DrawRectLine(Rect rect, UnityEngine.Color color, float width) {
            if (Event.current.type != EventType.Repaint) return;
            
            var offset = width * .3f;
            
            using (HandlesColor.Start(color)) {
                Handles.DrawAAPolyLine(wireTexture, width, new Vector3(rect.xMin - offset, rect.yMin), new Vector3(rect.xMax + offset, rect.yMin));
                Handles.DrawAAPolyLine(wireTexture, width, new Vector3(rect.xMax, rect.yMin - offset), new Vector3(rect.xMax, rect.yMax + offset));
                Handles.DrawAAPolyLine(wireTexture, width, new Vector3(rect.xMax + offset, rect.yMax), new Vector3(rect.xMin - offset, rect.yMax));
                Handles.DrawAAPolyLine(wireTexture, width, new Vector3(rect.xMin, rect.yMax + offset), new Vector3(rect.xMin, rect.yMin - offset));
            }
        }

        public class SearchPanel {
            static GUIStyle _searchStyle, _searchXStyle, _keyItemStyle;
            static GUIStyle searchStyle {
                get {
                    if (_searchStyle == null)
                        _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
                    return _searchStyle;
                }
            }
            static GUIStyle searchXStyle {
                get {
                    if (_searchXStyle == null)
                        _searchXStyle = GUI.skin.FindStyle("ToolbarSearchCancelButton");
                    return _searchXStyle;
                }
            }
            public static GUIStyle keyItemStyle {
                get {
                    if (_keyItemStyle == null) {
                        _keyItemStyle = new GUIStyle(EditorStyles.label);
                        _keyItemStyle.richText = true;      
                        _keyItemStyle.wordWrap = false;
                    }
                    return _keyItemStyle;
                }
            }

            string search;
            public string value {
                get => search;
                set => search = value;
            }

            public SearchPanel(string search) {
                this.search = search;
            }

            public void OnGUI(Action<string> onChanged, params GUILayoutOption[] options) {
                using (Change.Start(() => onChanged(search))) {
                    using (Horizontal.Start()) {
                        search = GUILayout.TextField(search, searchStyle, options);
                        if (!search.IsNullOrEmpty() && GUILayout.Button("", searchXStyle)) {
                            search = "";
                            EditorGUI.FocusTextInControl("");
                        }
                    }
                }
            }

            static UColor searchColor => EditorGUIUtility.isProSkin ? searchColorPro : searchColorLite;
            static readonly UColor searchColorPro = new UColor(1f, 0.53f, 0.53f);
            static readonly UColor searchColorLite = new UColor(1f, 0.27f, 0.27f);
            
            public static string Format(string text, string search) {
                var index = text.IndexOf(search, StringComparison.InvariantCultureIgnoreCase);
                
                if (index < 0)
                    return text;
                
                return text.Substring(0, index)
                    + text.Substring(index, search.Length).Colorize(searchColor).Bold()
                    + text.Substring(index + search.Length);
            }
        }

        public static bool Button(string label, string buttonLabel, GUIStyle style = null, params GUILayoutOption[] options) {
            using (Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(label);
                return style == null ? GUILayout.Button(buttonLabel, options) : GUILayout.Button(buttonLabel, style, options);
            }
        }

        public static bool Button(string label, GUIStyle style = null) {
            var content = new GUIContent(label);
            
            style ??= GUI.skin.button;
            
            return GUILayout.Button(content, style, GUILayout.Width(style.CalcSize(content).x));
        }

        public static void ClearControl() {
            GUI.FocusControl("");
        }

        public static int? NullableIntField(string label, int? value) {
            using (SingleLine.Start(label)) {
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                
                var toggleRect = rect;
                toggleRect.width = toggleRect.height;
                value = EditorGUI.Toggle(toggleRect, value.HasValue) ? value.GetValueOrDefault() : null;
                
                rect.xMin = toggleRect.xMax;
                
                if (value.HasValue) 
                    value = EditorGUI.IntField(rect, value.Value);
            }
            
            return value;
        }
    }
}