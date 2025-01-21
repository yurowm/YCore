// using System;
// using UnityEditor;
// using Yurowm.ObjectEditors;
// using Yurowm.Services;
//
// namespace Yurowm.Editors {
//     public class TrueTimeEditor : ObjectEditor<TrueTime> {
//         public override void OnGUI(TrueTime trueTime, object context = null) {
//             if (TimeSpan.TryParse(EditorGUILayout.TextField("Debug Time Offset", trueTime.debugTimeOffset.ToString()), out var value))
//                 trueTime.debugTimeOffset = value;
//                 
//             EditList("Servers", trueTime.servers);
//         }
//     }
//     
//     public class NTPSeverEditor : ObjectEditor<TrueTime.NTPServer> {
//         public override void OnGUI(TrueTime.NTPServer server, object context = null) {
//             server.URL = EditorGUILayout.TextField("URL", server.URL);
//         }
//     }
// }