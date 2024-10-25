using System;
using System.Globalization;
using UnityEditor;

namespace Yurowm.Editors {
    public static class TimeEditor {
        public static DateTime Edit(string label, DateTime dateTime) {
            if (DateTime.TryParse(EditorGUILayout.TextField(label, dateTime.ToString(CultureInfo.InvariantCulture)), out var value))
                return value;

            return dateTime;
        }
        
        public static TimeSpan Edit(string label, TimeSpan span) {
            if (TimeSpan.TryParse(EditorGUILayout.TextField(label, span.ToString()), out var value))
                return value;

            return span;
        }
    }
}