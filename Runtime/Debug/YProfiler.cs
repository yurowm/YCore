using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Profiling {
    public static class YProfiler {
        static Dictionary<string, AreaProfiler> areas = new Dictionary<string, AreaProfiler>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        [OnLaunch()]
        static void Initialize() {
            if (OnceAccess.GetAccess("YProfiler"))
                Update().Run();
        }
        #endif
        
        static DateTime? lastCheck = null;
        static int frames = 0;
        static DelayedAccess reportUpdate = new DelayedAccess(1f);
                        
        static IEnumerator Update () {
            while (true) {
                frames++;
                if (reportUpdate.GetAccess()) {
		            if (lastCheck.HasValue) {
                        double totalFrameTime = (DateTime.Now - lastCheck.Value).TotalMilliseconds;
                        DebugPanel.Log("Frame Time", "Profiler", (totalFrameTime / frames).ToString("F2") + "ms.");
                        DebugPanel.Log("FPS", "Profiler", (Mathf.RoundToInt((float) (frames * 1000d / totalFrameTime))));
                        foreach (AreaProfiler area in areas.Values) {
                            area.Frame();
                            DebugPanel.Log(area.name, "Profiler", area.GetReport(totalFrameTime));
                            area.Clear();
                        }
                    }
                    lastCheck = DateTime.Now;
                    frames = 0;
                } else
                    foreach (AreaProfiler area in areas.Values)
                        area.Frame();
                yield return null;
            }
            
        }

        public static AreaProfiler Area(string name) {
        
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (name.IsNullOrEmpty()) 
                return null;

            if (!areas.TryGetValue(name, out var area)) {
                area = new AreaProfiler(name);
                areas.Add(name, area);
            }

            area.Start();
            
            return area;
            
            #else
            
            return null;
            
            #endif
        }

        public class AreaProfiler : IDisposable {
            public string name;

            public AreaProfiler(string name) {
                this.name = name;
            }

            double memory = 0;
            double sum = 0;
            double min = 0;
            double max = 0;
            double avg = 0;
            int frames = 0;

            DateTime? startTime = null;

            void CompleteArea() {
                if (startTime.HasValue) {
                    memory += (DateTime.Now - startTime.Value).TotalMilliseconds;
                    startTime = null;
                }
            }

            public void Start() {
                CompleteArea();
                startTime = DateTime.Now;
                #if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.BeginSample(name);
                #endif
            }

            public void Dispose() {
                CompleteArea();
                #if UNITY_EDITOR
                UnityEngine.Profiling.Profiler.EndSample();
                #endif
            }

            public string GetReport(double totalFrameTime) {
                if (frames <= 0)
                    return "NaN";
                else
                    return string.Format("{0} ({1} - {2}) [{3:F2}%]", 
                        TimeUnits(avg), TimeUnits(min), TimeUnits(max), sum * 100 / totalFrameTime);
            }

            static string TimeUnits(double time) {
                if (time < 0) return "-" + TimeUnits(-time);
            
                if (time >= 1000) return (time / 1000).ToString("F2") + "s.";
                else if (time >= 1) return (time).ToString("F2") + "ms.";
                else if (time >= 0.001f) return (time * 1000).ToString("F2") + "mcs.";
                else if (time >= 0.000001f) return (time * 1000000).ToString("F2") + "ns.";
                else if (time == 0) return "0";
                else return "~0";
            }

            public void Frame() {
                sum += memory;
                frames++;
                min = Math.Min(memory, min);
                max = Math.Max(memory, max);
                avg = sum / frames;
                memory = 0;
            }

            public void Clear() {
                startTime = null;
                sum = 0;
                frames = 0;
                min = Double.MaxValue;
                max = 0;
                avg = 0;
                memory = 0;
            }
        }
    }
}