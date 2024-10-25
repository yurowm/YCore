using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Yurowm.Coroutines;
using Yurowm.Dashboard;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm {
    [DashboardGroup("Development")]
    [DashboardTab("Screenshot Maker", null)]
    public class ScreenshotMaker : DashboardEditor {
        
        static DirectoryInfo folder = null;
        public override bool Initialize() {
            return true;
        }

        static void CreateFolder() {
            if (folder == null) {
                var projectFolder= new DirectoryInfo(Application.dataPath).Parent?.FullName ?? "";
                folder = new DirectoryInfo(Path.Combine(projectFolder, "Screenshots"));
                if (!folder.Exists) folder.Create();
            }
        }

        public override void OnGUI() {
                
            using (GUIHelper.Lock.Start(!Application.isPlaying)) {
                if (GUIHelper.Button("Time Scale Pause", Time.timeScale == 0 ? "[PAUSED]" : "[PLAYING]"))
                    Time.timeScale = Time.timeScale == 0 ? 1 : 0;
                //
                //
                // if (GUIHelper.Button("Set Resolution", "...")) {
                //     GenericMenu menu = new GenericMenu();
                //     
                //     menu.AddItem(new GUIContent("iPhone 5.5"), false, () => Screen.SetResolution(1242, 2208, false));
                //     menu.AddItem(new GUIContent("iPhone 6.5"), false, () => Screen.SetResolution(1242, 2688, false));
                //     menu.AddItem(new GUIContent("iPad"), false, () => Screen.SetResolution(2048, 2732, false));
                //     menu.AddItem(new GUIContent("FullHD"), false, () => Screen.SetResolution(1080, 1920, false));
                //     
                //     menu.ShowAsContext();
                // }
                
                using (GUIHelper.Horizontal.Start()) {
                    if (GUILayout.Button("Shot and Save", EditorStyles.miniButtonLeft, GUILayout.Width(150))) Shot(1);
                    if (GUILayout.Button("X2", EditorStyles.miniButtonMid, GUILayout.Width(40))) Shot(2);
                    if (GUILayout.Button("X5", EditorStyles.miniButtonMid, GUILayout.Width(40))) Shot(5);
                    if (GUILayout.Button("X10", EditorStyles.miniButtonRight, GUILayout.Width(40))) Shot(10);
                }
            }
            
        }

        static IEnumerator Shot(int size) {
            return Shot(new Order {
                superSize = size,
                name = $"Screen_{Application.productName}"
            });
        }
        
        static IEnumerator Shot(Order order) {
            CreateFolder();

            string fileName = $"{order.name ?? "Screenshot"}_{DateTime.Now}";
            fileName = fileName.Replace('/', '-').Replace(' ', '_').Replace(':', '-');
            fileName += ".png";

            var file = new FileInfo(Path.Combine(folder.FullName, fileName));
            if (order.resolution.HasValue) {
                Screen.SetResolution(order.resolution.Value.X, order.resolution.Value.Y, FullScreenMode.Windowed);
                yield return new WaitForEndOfFrame();
            }
             
            ScreenCapture.CaptureScreenshot(file.FullName, order.superSize);
        }
        
        static IEnumerator Shot(IEnumerable<Order> orders) {
            foreach (var order in orders)
                yield return Shot(order);
        }

        struct Order {
            public int2? resolution;
            public int superSize;
            public string name;
        }

        
        [MenuItem("Yurowm/Screenshot/Create x1 %1")]
        static void Shot1() {
            Shot(1).Run();
        }
            
        [MenuItem("Yurowm/Screenshot/Create x2 %2")]
        static void Shot2() {
            Shot(2).Run();
        }

        [MenuItem("Yurowm/Screenshot/Create x5 %5")]
        static void Shot5() {
            Shot(5).Run();
        }

        [MenuItem("Yurowm/Screenshot/Create x10 %0")]
        static void Shot10() {
            Shot(10).Run();
        }

        [MenuItem("Yurowm/Screenshot/Apple App Store")]
        static void ShotForAppStore() {
            var orders = new List<Order> {
                new Order { name = "iPhone_6.7", resolution = new int2(1290, 2796) },
                new Order { name = "iPhone_6.5", resolution = new int2(1242, 2688) },
                new Order { name = "iPhone_5.5", resolution = new int2(1242, 2208) },
                new Order { name = "iPad_12.9", resolution = new int2(2048, 2732) }
            };
            
            Shot(orders).Run();
        }
    }
}