using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Yurowm.Extensions;
using Yurowm.Integrations;
using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public class SoundHapticWaveForm : SoundEffect.Module {
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 0.05f, 1);
        
        public string pattern;

        long[] durations = null;
        int[] amplitudes = null;
        
        public override void OnPlay() {
            if (SoundController.IsMute())
                return;
            
            if (durations == null) 
                Build(0.03f, 16);
            
            if (Integration.Get<YHaptic>()?.GetActiveProvider() is YHapticAndroid provider)
                provider.Play(durations, amplitudes);
        }

        public void Build(float timerResolution, int amplitudeResolution) {
            var durations = new List<long>();
            var amplitudes = new List<int>();
    
            if (!pattern.IsNullOrEmpty()) {
                var segments = pattern.Split("\n");
                foreach (var segment in segments) {
                    var parts = segment.Split(':');
                    if (parts.Length < 2) continue;

                    var type = parts[0].ToLower();
                    var args = parts[1].Split(',').Select(arg => int.Parse(arg.Trim())).ToArray();

                    switch (type) {
                        case "constant":
                            if (args.Length == 2) {
                                var duration = (long)args[0];
                                var amplitude = Mathf.Clamp((int)args[1], 0, 255);
                                durations.Add(duration);
                                amplitudes.Add(amplitude);
                            }
                            break;

                        case "pause":
                            if (args.Length == 1) {
                                var duration = (long)args[0];
                                durations.Add(duration);
                                amplitudes.Add(0); // Пауза = амплитуда 0
                            }
                            break;

                        case "linear":
                            if (args.Length == 3) {
                                var totalDuration = args[0];
                                var startAmplitude = Mathf.Clamp((int)args[1], 0, 255);
                                var endAmplitude = Mathf.Clamp((int)args[2], 0, 255);

                                var steps = Mathf.CeilToInt(totalDuration / timerResolution);
                                for (int i = 0; i < steps; i++) {
                                    var t = i / (float)steps; // Нормализованное время
                                    var amplitude = Mathf.Lerp(startAmplitude, endAmplitude, t);
                                    durations.Add((long)(timerResolution * 1000)); // В миллисекундах
                                    amplitudes.Add(Mathf.RoundToInt(amplitude));
                                }
                            }
                            break;
                    }
                }
            }

            this.durations = durations.ToArray();
            this.amplitudes = amplitudes.ToArray();
        }

        
        public void BuildLegacy(float timerResolution, int amplitudeResolution) {
            var durations = new List<long>();
            var amplitudes = new List<int>();
            
            var stepTimeMS = 1000 * timerResolution;

            var keys = curve.keys;
            var amplitude = -1;
            var lastShowTimeMS = 0L;
            
            void Shot(float time) {
                var timeMS = (long) (1000 * time).RoundToInt();
                if (amplitude < 0) return;
                durations.Add(timeMS - lastShowTimeMS);
                amplitudes.Add(amplitude);
                lastShowTimeMS = timeMS;
            }
            
            // Проходим по всем сегментам между ключевыми точками
            for (int i = 0; i < keys.Length - 1; i++) {
                var startTime = keys[i].time;
                var endTime = keys[i + 1].time;
            
                var segmentDuration = endTime - startTime;
                var steps = (segmentDuration / timerResolution).CeilToInt(); 
            
                for (var j = 0; j <= steps; j++) {
                    var t = Mathf.Lerp(startTime, endTime, j / (float) steps); 
                    var a = (curve.Evaluate(t).Clamp01() * 255).RoundToInt();
            
                    if ((a - amplitude).Abs() >= amplitudeResolution) {
                        Shot(t);
                        amplitude = a;
                    }
                }
            }
            
            Shot(keys.Last().time);

            this.durations = durations.ToArray();
            this.amplitudes = amplitudes.ToArray();
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            // writer.Write("curve", curve);
            writer.Write("pattern", pattern);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            // reader.Read("curve", ref curve);
            reader.Read("pattern", ref pattern);
        }
    }
}