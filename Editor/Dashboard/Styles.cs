using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yurowm.Icons;

namespace Yurowm.GUIStyles {
    public static class Styles {

        static bool initialized = false;

        static PropertyInfo getGUIDepth;
        
        static bool CheckOnGUI() {
            if (getGUIDepth == null)
                getGUIDepth = typeof(GUIUtility)
                    .GetProperty("guiDepth",  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return (int) getGUIDepth.GetValue(null) > 0;
        }
        
        static Styles() {
            if (!CheckOnGUI()) {
                Debug.LogError("Don't call styles out of GUI pipline");
                return;   
            }
            
            Initialize();
        }

        public static void Initialize() {
            if (!CheckOnGUI()) return;
            
            if (EditorStyles.label == null) return;
            
            try {
                Font monospaceFont = Resources.Load<Font>("Fonts/Monospace");
                
                richLabel = new GUIStyle(EditorStyles.label);
                richLabel.richText = true;

                tagLabel = new GUIStyle();
                tagLabel.border = new RectOffset(1, 1, 1, 1);
                tagLabel.margin = new RectOffset(0, 0, 0, 0);
                tagLabel.padding = new RectOffset(0, 0, 0, 0);
                tagLabel.overflow = new RectOffset(0, 0, 0, 0);
                tagLabel.normal.textColor = Color.white;
                tagLabel.font = monospaceFont;
                tagLabel.fontStyle = FontStyle.Bold;
                tagLabel.alignment = TextAnchor.MiddleCenter;
                tagLabel.fontSize = 10;

                tagLabelBlack = new GUIStyle(tagLabel);
                tagLabelBlack.normal.textColor = Color.black;

                whiteLabel = new GUIStyle(EditorStyles.whiteLabel);
                whiteLabel.normal.textColor = Color.white;

                whiteBoldLabel = new GUIStyle(EditorStyles.whiteBoldLabel);
                whiteBoldLabel.normal.textColor = Color.white;

                multilineLabel = new GUIStyle(EditorStyles.label);
                multilineLabel.clipping = TextClipping.Clip;
                multilineLabel.wordWrap = true;

                centeredLabel = new GUIStyle(EditorStyles.label);
                centeredLabel.alignment = TextAnchor.MiddleCenter;

                centeredMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                centeredMiniLabel.normal.textColor = EditorStyles.label.normal.textColor;

                miniLabel = new GUIStyle(centeredMiniLabel);
                miniLabel.alignment = richLabel.alignment;
                
                miniLabelBlack = new GUIStyle(miniLabel);
                miniLabel.normal.textColor = Color.black;

                miniButton = new GUIStyle(EditorStyles.miniButton);
                miniButton.fixedHeight = 0;
                
                centeredMiniLabelWhite = new GUIStyle(centeredMiniLabel);
                centeredMiniLabelWhite.normal.textColor = Color.white;
                centeredMiniLabelWhite.active = centeredMiniLabelWhite.normal;
                centeredMiniLabelWhite.hover = centeredMiniLabelWhite.normal;

                centeredMiniLabelBlack = new GUIStyle(centeredMiniLabel);
                centeredMiniLabelBlack.normal.textColor = Color.black;
                centeredMiniLabelBlack.active = centeredMiniLabelBlack.normal;
                centeredMiniLabelBlack.hover = centeredMiniLabelBlack.normal;

                textAreaLineBreaked = new GUIStyle(EditorStyles.textArea);
                textAreaLineBreaked.clipping = TextClipping.Clip;

                monospaceLabel = new GUIStyle(textAreaLineBreaked);
                monospaceLabel.font = monospaceFont;
                monospaceLabel.fontSize = 11;

                largeTitle = new GUIStyle(EditorStyles.label);
                largeTitle.normal.textColor = EditorStyles.label.normal.textColor;
                largeTitle.fontStyle = FontStyle.Bold;
                largeTitle.fontSize = 21;
                largeTitle.richText = true;

                title = new GUIStyle(largeTitle);
                title.fontSize = 18;

                miniTitle = new GUIStyle(title);
                miniTitle.fontSize = 14;
                
                microTitle = new GUIStyle(title);
                microTitle.fontSize = 12;

                popup = new GUIStyle(GUI.skin.window);
                popup.padding = new RectOffset(4, 4, 4, 4);

                area = new GUIStyle();
                area.normal.background = EditorIcons.GetIcon("Area");
                area.border = new RectOffset(4, 4, 4, 4);
                area.margin = new RectOffset(1, 1, 1, 1);
                area.padding = new RectOffset(4, 4, 4, 4);
                area.focused = area.normal;
                area.hover = area.normal;

                paperArea = new GUIStyle(EditorStyles.textArea);
                paperArea.normal.background = EditorIcons.GetIcon("PaperArea");

                paperArea.border = new RectOffset(6, 6, 5, 6);
                paperArea.margin = new RectOffset(4, 4, 4, 4);
                paperArea.padding = new RectOffset(3, 3, 3, 3);
                paperArea.alignment = TextAnchor.MiddleCenter;
                
                blackArea = new GUIStyle(area);
                blackArea.normal.background = EditorIcons.GetIcon("BlackArea");
                blackArea.border = new RectOffset(4, 4, 5, 3);
                blackArea.margin = new RectOffset(2, 2, 2, 2);
                blackArea.padding = new RectOffset(4, 4, 4, 4);
                blackArea.hover = blackArea.normal;
            } catch (Exception) {
                return;
            }
            initialized = true;
        }

        public static GUIStyle tagLabel;
        public static GUIStyle tagLabelBlack;
        public static GUIStyle richLabel;
        public static GUIStyle whiteLabel;
        public static GUIStyle whiteBoldLabel;
        public static GUIStyle multilineLabel;
        public static GUIStyle miniLabel;
        public static GUIStyle miniLabelBlack;
        public static GUIStyle centeredLabel;
        public static GUIStyle centeredMiniLabel;
        public static GUIStyle centeredMiniLabelWhite;
        public static GUIStyle centeredMiniLabelBlack;
        public static GUIStyle miniButton;

        public static GUIStyle largeTitle;
        public static GUIStyle title;
        public static GUIStyle miniTitle;
        public static GUIStyle microTitle;
        public static GUIStyle area;
        public static GUIStyle popup;
        public static GUIStyle paperArea;
        public static GUIStyle blackArea;
        public static GUIStyle textAreaLineBreaked;
        public static GUIStyle monospaceLabel;
    }
}
